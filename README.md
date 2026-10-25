# Hamware: Software Development Simulator

かわいいハムスターたちが、実在する技術を使って、毎週発生する仕様変更・問い合わせ・障害などに対応しながらプロダクトを育てる開発現場シミュレーションゲーム。

現在は、社員2名へ工数を配分し、週次結果を確認できる最小プレイ画面まで実装している段階。

## 試作版を遊ぶ

Unity 6000.5.10f1で `Assets/Scenes/Hamware.unity` を開き、Playを押す。
メニュー `Hamware > Open Playable Prototype` からも開ける。ビルド対象の先頭シーンもHamware。

1. 左側でハムスター社員を選ぶ。
2. タスクの入力欄へ、その社員から割り当てる**合計時間**を入力し「設定」を押す。「取消」または0hで取り消せる。
3. 「1週間進める」で計画を確認し、確定する。残した時間は休養になる。
4. 週報で勤務時間・進捗・完了効果・疲労の変化を確認し、次週の計画へ進む。

社員2名、機能開発4件、Testing / Architecture各2件の固定シナリオ。画面に表示する進捗は小数3桁までだが、計算・レポートはdecimalのまま保持する。
週報を閉じるまで次の週を確定できない。「最初から」は確認後に状態を初期化する。
日本語表示はOSのYu Gothic / Meiryo / Noto Sans CJK JP / Hiragino Sansを利用する。Windowsの日本語環境を試作対象とし、フォントファイルは同梱しない。
保存・復元、イベント、プロダクト公開は後続計画の対象。品質は現在、増減を確認する指標であり、障害への影響はまだない。

割当・結果の契約は[PlanningAndReports](Assets/Docs/Domains/PlanningAndReports.md)、試作UIの責務は[PrototypeUI](Assets/Docs/PrototypeUI.md)を参照。

## 開発の進め方

- 各GitHub Issueが1つの機能スライスをスコープする
- 実装前に詳細な開発計画を立て、合意した上で実装する(plan-first)
- 各変更はNUnit EditModeテストで検証し、Unity Test Runnerの結果(全PASS)をもって完了とする
- git / GitHub操作(commit, push, PR作成)は開発者が手動で行う

## 設計方針

### 自己管理型ドメインオブジェクト

`Employee`や`DevelopmentTask`のような各ドメインオブジェクトは、自分自身の状態を自分で管理する。

- `Employee`は自分の週あたり残り利用可能工数(`AvailableHours`)を持つ。公開の工数消費・返却は`GameSimulation.TryAllocateHours` / `TrySetAllocatedHours`経由に限定する。リセットは社員の週末処理内からのみ行う
- `Employee`は自分が保有する基本スキル(`Skill`の集合)も自分で管理する(`AddSkill`)
- `Employee`は疲労(`Fatigue`)を保持し、週末に消費勤務時間から疲労更新と勤務時間のリセットを行う(`CompleteWeek`)
- `DevelopmentTask`は自分の補正後の累積進捗工数(`CompletedProgressHours`)を自分で持つ
- `ProductState`は技術的負債・品質を保持し、週次の完了効果を有効範囲内で反映する

上位クラス(`GameSimulation`)は個々のオブジェクトの内部状態を直接書き換えず、各オブジェクトが提供するメソッド経由でのみ操作する。シミュレーション内部用の工数消費・週末更新・タスク進捗反映は外部へ公開しない。

### Try*パターン

不正な操作は例外ではなくbool戻り値で表現し、失敗時は状態を変更しない。

- `Employee.TryConsumeHours(hours)`（内部用）
- `GameSimulation.TryAllocateHours(employee, task, hours)`
- `GameSimulation.TrySetAllocatedHours(employee, task, hours)`：合計時間の設定。0で当週の割当を削除する

一方、コンストラクタでの不正な初期値(範囲外のスキルレベルなど)は`ArgumentOutOfRangeException`をスローする。

### フォルダ構成

- `Assets/Scripts/Company/` : 社員・組織に関するドメインモデル(`Employee`, `Skill`, `SkillType`)
- `Assets/Scripts/Product/` : プロダクトの状態と負債倍率(`ProductState`, `ProductHealthCalculator`)
- `Assets/Scripts/Simulation/` : シミュレーション進行に関するドメインモデル(`DevelopmentTask`, `WeekProcessor`, `GameSimulation`)
- `Assets/Scripts/UI/` : 別アセンブリ`Hamware.UI`。画面・セッション・固定シナリオを持ち、`Hamware.Game`の公開APIだけを参照する

`Employee`が`Simulation/`ではなく`Company/`に置かれているのは意図的な構造判断で、「誰が」を表す概念と「何を進めるか」を表す概念を分離している。

## ドメインモデル

### Company

| クラス | 役割 |
|---|---|
| `Employee` | 社員。名前・週あたり利用可能工数・保有スキル・疲労を持つ。職種は固定属性としては持たない(将来的な採用テンプレート/表示ラベルとして別概念で扱う想定) |
| `Skill` | 基本スキルを表す値オブジェクト。種類(`SkillType`)とレベル(1〜5)を持ち、生成後は不変 |
| `SkillType` | 基本スキルの種類を表すenum(Frontend / Backend / Database / Infrastructure / Testing / Security / Architecture / Planning / Management / Communication / Sales / CustomerSupport) |
| `FatigueCalculator` | 稼働率による疲労増減と疲労による生産性倍率を返す、副作用のない計算クラス |

`Employee.AddSkill`は同一`SkillType`のスキルを上書き更新(upsert)する。将来的なスキル成長(レベルアップ)は、より高いレベルの`Skill`を再登録することで表現する想定(成長処理自体は未実装)。

### Simulation

| クラス | 役割 |
|---|---|
| `DevelopmentTask` | 開発タスク。タスク種別・必要進捗工数・スキル補正後の累積進捗工数を持ち、進捗が必要量に達すると完了扱いになる。進捗の単位は時間相当で、実勤務時間とは区別する |
| `TaskType` | タスクの種類。FrontendImplementation / BackendImplementation / Database / Infrastructure / Testing / Security / Architecture / Planning / Management / CustomerSupport |
| `TaskSkillProfile` / `TaskSkillProfiles` | タスクが参照する基本スキルと重みの不変な定義、および種別ごとの初期対応表 |
| `ProductivityCalculator` | 実勤務工数をスキルと疲労に応じた進捗工数へ変換する、副作用のない計算サービス |
| `TaskProductProfile` / `TaskProductProfiles` | タスク種別ごとの完了効果と、負債による進捗低下の対象を表す不変な定義と対応表 |
| `GameSimulation` | 自身のプロダクトを所有し、社員・タスクの登録、工数割り当て(`TryAllocateHours`)、週送り(`AdvanceWeek`)を管理する入口 |
| `WeekProcessor` | スキル・疲労・開始時の負債で進捗を補正し、新規完了によるプロダクトの変化を合算して反映する |

`GameSimulation.AdvanceWeek()`が内部の`WeekProcessor`と各社員へ処理を委譲し、不変の`WeekResult`を返す。`History`と`LastWeekResult`から確定済み結果を参照できる。週次順序・状態変化の反映時期は[ProductHealth](Assets/Docs/Domains/ProductHealth.md#週次順序)を参照。

### Product

| クラス | 役割 |
|---|---|
| `ProductState` | プロダクト全体の技術的負債と品質を保持し、初期値検証・増減時の上下限制御を行う |
| `ProductHealthCalculator` | 技術的負債から進捗倍率を返す、副作用のない計算クラス |

### 基本スキルによる生産性

`DevelopmentTask`の生成時にはタスク種別が必須となる。

```csharp
var task = new DevelopmentTask("API実装", 40, TaskType.BackendImplementation);
```

基本スキル倍率と疲労倍率を組み合わせて、実勤務工数から進捗工数を計算する。未習得は計算上0として扱い、未習得分の重みを他のスキルへ再配分しない。基本スキル倍率の式と例は[TaskSkillProfile](Assets/Docs/Domains/TaskSkillProfile.md)を参照。`Skill`自体のレベル範囲は1〜5のまま。

- BackendImplementationはBackend 70% / Database 20% / Testing 10%。他の9種は対応する単一スキル100%。詳細は[TaskSkillProfile](Assets/Docs/Domains/TaskSkillProfile.md)を参照。
- CommunicationとSalesは社員の基本スキルとして保持できるが、この初期対応表では参照しない。
- `AvailableHours` / `AllocatedHours`は実勤務工数を整数で扱い、倍率の影響を受けない。
- `CompletedProgressHours`は補正後の進捗を`decimal`で蓄積し、週をまたいで端数を保持する。`RequiredProgressHours`は基準となる必要進捗工数で、整数のまま。
- スキルは週送り時点の値を参照する。必要進捗を超えた分は破棄し、勤務時間を返却・再配分しない。

### 稼働量と疲労

- 社員は勤務の稼働率に応じて疲労が蓄積・回復し、疲労が高いほど進捗が下がる。基本スキル自体の値は変えない。
- 全タスクの進捗反映後に疲労を更新し、当週の負荷は翌週から効く。無割当の社員も回復する。
- 疲労の値域・増減式・数値例・精度と調整方法は[Fatigue](Assets/Docs/Domains/Fatigue.md)を参照。残業、休暇、スキル成長は対象外。

### 技術的負債と品質

- プロダクトは負債・品質を継続保持する。機能開発を続けると後続の開発効率が下がり、TestingやArchitectureへ投資すると健全性が改善する。
- 値域・初期値・係数表・計算式・週次順序・対象外は[ProductHealth](Assets/Docs/Domains/ProductHealth.md)を参照。

## テスト

`Assets/Tests/EditMode/`配下にNUnitベースのEditModeテストを配置している。

- `EmployeeTests.cs`
- `SkillTests.cs`
- `DevelopmentTaskTests.cs`
- `GameSimulationTests.cs`
- `TaskSkillProfileTests.cs`
- `ProductivityCalculatorTests.cs`
- `ProductivitySimulationTests.cs`
- `FatigueCalculatorTests.cs`
- `FatigueSimulationTests.cs`
- `WeekProcessorTests.cs`
- `ProductStateTests.cs`
- `ProductHealthCalculatorTests.cs`
- `TaskProductProfileTests.cs`
- `ProductHealthSimulationTests.cs`
- `PlanningAndReportTests.cs`：割当編集・取消、不変のレポート、確定結果との一致、失敗時の状態維持、登録順の結果

`Assets/Tests/UIEditMode/PrototypeSessionTests.cs`は、二重確定と週報中の編集拒否、再開始、アセンブリ境界、ドメイン定義からの文言生成を確認する。

`Assets/Tests/PlayMode/PrototypeUITests.cs`は、画面の入力・ボタンから割当、拒否理由、取消、担当変更、確認、週報、12週の進行、改善・休養、全タスク完了後の週送り、再開始を確認する。UI ToolkitのClickEventを送る操作テストであり、OSのマウス装置を経由した操作ではない。

環境変数`HAMWARE_CAPTURE_DIR`へ出力先を指定すると、同テストで1600×1000と1280×720の画面画像を保存し、数値カードと主要操作が画面内に収まることも確認する。社員欄・タスク欄・週報は必要に応じてスクロールできる。

ドメイン内部操作の単体テストは、`InternalsVisibleTo`でテストアセンブリにアクセスを許可している。公開APIでは社員の勤務時間を直接消費・リセットしたり、タスクの進捗を直接反映したりできない。

Unity Test Runner経由で実行し、`TestResults.xml`の内容を正として合否を判断する(コンソールログのポーリングには依存しない)。
