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
- `DevelopmentTask`は自分の消化済み工数(`CompletedHours`)を自分で持つ

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
| `DevelopmentTask` | 開発タスク。必要工数と消化済み工数を持ち、消化済み工数が必要工数に達すると完了扱いになる |
| `GameSimulation` | 社員・タスクの登録、工数割り当て(`TryAllocateHours`)、週送り(`AdvanceWeek`)を管理するシミュレーション全体の入口 |
| `WeekProcessor` | 週次処理で、社員ごとの割り当て工数をタスクの消化済み工数へ反映する |

`GameSimulation.AdvanceWeek()`は、社員ごとの週内割り当てをタスク単位の合計工数へ変換して`WeekProcessor`へ渡し、その後すべての割り当てをクリアして各社員の利用可能工数をリセットする。

## テスト

`Assets/Tests/EditMode/`配下にNUnitベースのEditModeテストを配置している。

- `EmployeeTests.cs`
- `SkillTests.cs`
- `DevelopmentTaskTests.cs`
- `GameSimulationTests.cs`

Unity Test Runner経由で実行し、`TestResults.xml`の内容を正として合否を判断する(コンソールログのポーリングには依存しない)。
