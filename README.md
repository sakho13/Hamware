# Hamware: Software Development Simulator

かわいいハムスターたちが、実在する技術を使って、毎週発生する仕様変更・問い合わせ・障害などに対応しながらプロダクトを育てる開発現場シミュレーションゲーム。

現在はUIを持たないシミュレーションレイヤーの実装を、GitHub Issue単位で段階的に進めている段階。

## 開発の進め方

- 各GitHub Issueが1つの機能スライスをスコープする
- 実装前に詳細な開発計画を立て、合意した上で実装する(plan-first)
- 各変更はNUnit EditModeテストで検証し、Unity Test Runnerの結果(全PASS)をもって完了とする
- git / GitHub操作(commit, push, PR作成)は開発者が手動で行う

## 設計方針

### 自己管理型ドメインオブジェクト

`Employee`や`DevelopmentTask`のような各ドメインオブジェクトは、自分自身の状態を自分で管理する。

- `Employee`は自分の週あたり残り利用可能工数(`AvailableHours`)を自分で持ち、`TryConsumeHours` / `ResetWeeklyHours`を通じてのみ変化する
- `Employee`は自分が保有する基本スキル(`Skill`の集合)も自分で管理する(`AddSkill`)
- `DevelopmentTask`は自分のスキル補正後の累積進捗工数(`CompletedProgressHours`)を自分で持つ

上位クラス(`GameSimulation`)は個々のオブジェクトの内部状態を直接書き換えず、各オブジェクトが公開するメソッド経由でのみ操作する。

### Try*パターン

不正な操作は例外ではなくbool戻り値で表現し、失敗時は状態を変更しない。

- `Employee.TryConsumeHours(hours)`
- `GameSimulation.TryAllocateHours(employee, task, hours)`

一方、コンストラクタでの不正な初期値(範囲外のスキルレベルなど)は`ArgumentOutOfRangeException`をスローする。

### フォルダ構成

- `Assets/Scripts/Company/` : 社員・組織に関するドメインモデル(`Employee`, `Skill`, `SkillType`)
- `Assets/Scripts/Simulation/` : シミュレーション進行に関するドメインモデル(`DevelopmentTask`, `WeekProcessor`, `GameSimulation`)

`Employee`が`Simulation/`ではなく`Company/`に置かれているのは意図的な構造判断で、「誰が」を表す概念と「何を進めるか」を表す概念を分離している。

## ドメインモデル

### Company

| クラス | 役割 |
|---|---|
| `Employee` | 社員。名前・週あたり利用可能工数・保有スキルを持つ。職種は固定属性としては持たない(将来的な採用テンプレート/表示ラベルとして別概念で扱う想定) |
| `Skill` | 基本スキルを表す値オブジェクト。種類(`SkillType`)とレベル(1〜5)を持ち、生成後は不変 |
| `SkillType` | 基本スキルの種類を表すenum(Frontend / Backend / Database / Infrastructure / Testing / Security / Architecture / Planning / Management / Communication / Sales / CustomerSupport) |

`Employee.AddSkill`は同一`SkillType`のスキルを上書き更新(upsert)する。将来的なスキル成長(レベルアップ)は、より高いレベルの`Skill`を再登録することで表現する想定(成長処理自体は未実装)。

### Simulation

| クラス | 役割 |
|---|---|
| `DevelopmentTask` | 開発タスク。タスク種別・必要進捗工数・スキル補正後の累積進捗工数を持ち、進捗が必要量に達すると完了扱いになる。進捗の単位は時間相当で、実勤務時間とは区別する |
| `TaskType` | タスクの種類。FrontendImplementation / BackendImplementation / Database / Infrastructure / Testing / Security / Architecture / Planning / Management / CustomerSupport |
| `TaskSkillProfile` / `TaskSkillProfiles` | タスクが参照する基本スキルと重みの不変な定義、および種別ごとの初期対応表 |
| `ProductivityCalculator` | 実勤務工数をスキルに応じた進捗工数へ変換する、副作用のない計算サービス |
| `GameSimulation` | 社員・タスクの登録、工数割り当て(`TryAllocateHours`)、週送り(`AdvanceWeek`)を管理するシミュレーション全体の入口 |
| `WeekProcessor` | 週次処理で、社員ごとの実勤務工数を基本スキルで補正し、タスク単位で合算して進捗へ反映する |

`GameSimulation.AdvanceWeek()`は、社員ごとの実勤務工数を`WeekProcessor`へ渡す。`WeekProcessor`は社員ごとにスキル補正してからタスク単位に合算し、進捗へ反映する。その後、シミュレーションがすべての割り当てをクリアして週番号を進め、各社員の利用可能工数をリセットする。

### 基本スキルによる生産性

`DevelopmentTask`の生成時にはタスク種別が必須となる。

```csharp
var task = new DevelopmentTask("API実装", 40, TaskType.BackendImplementation);
```

生産性倍率は`0.5 + 0.25 × Σ(関連スキルレベル × 重み)`、進捗工数は`実勤務工数 × 生産性倍率`で計算する。未習得は計算上0として扱い、未習得分の重みを他のスキルへ再配分しない。`Skill`自体のレベル範囲は1〜5のまま。

- 全関連スキルが未習得なら0.5倍、Lv2なら1倍、Lv5なら1.75倍。
- BackendImplementationはBackend 70% / Database 20% / Testing 10%。他の9種は対応する単一スキル100%。詳細は[TaskSkillProfile](Assets/Docs/Domains/TaskSkillProfile.md)を参照。
- CommunicationとSalesは社員の基本スキルとして保持できるが、この初期対応表では参照しない。
- `AvailableHours` / `AllocatedHours`は実勤務工数を整数で扱い、倍率の影響を受けない。
- `CompletedProgressHours`は補正後の進捗を`decimal`で蓄積し、週をまたいで端数を保持する。`RequiredProgressHours`は基準となる必要進捗工数で、整数のまま。
- スキルは週送り時点の値を参照する。必要進捗を超えた分は破棄し、勤務時間を返却・再配分しない。

## テスト

`Assets/Tests/EditMode/`配下にNUnitベースのEditModeテストを配置している。

- `EmployeeTests.cs`
- `SkillTests.cs`
- `DevelopmentTaskTests.cs`
- `GameSimulationTests.cs`
- `TaskSkillProfileTests.cs`
- `ProductivityCalculatorTests.cs`
- `ProductivitySimulationTests.cs`

Unity Test Runner経由で実行し、`TestResults.xml`の内容を正として合否を判断する(コンソールログのポーリングには依存しない)。
