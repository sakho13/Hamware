# Employee

Employee は会社に所属する社員を表します。

- 社員は週ごとに割当可能な勤務工数の上限(`WeeklyAvailableHours`)を持つ。契約勤務時間や、疲労が増えない稼働量を意味する値ではない。
- 一人の社員を複数のタスクへ割り当てることができる。
- 複数社員を同じタスクへ割り当てることもできる。
- 社員は基本スキル(種類とレベル)を保有できる。詳細は [Skill](./Skill.md) を参照。
- `TryGetSkill(SkillType, out Skill)`で最新の登録済みスキルを取得できる。未習得・未定義種別ならfalse、出力はnullとなる。
- 利用可能工数と消費工数は実際の勤務時間を表し、生産性倍率によって増減しない。未習得スキルを計算上0として扱う処理は生産性計算側が担当する。
- 疲労(`Fatigue`)を`decimal`の0〜100で保持する。初期値は0。コンストラクタ末尾の`initialFatigue`で指定でき、範囲外の初期値は`ArgumentOutOfRangeException`となる。公開setterは持たない。
- `internal CompleteWeek()`で、今週の消費勤務時間から疲労を更新した後、翌週の利用可能工数へ戻す。全タスクの進捗反映後にシミュレーションから呼ぶ。
- `TryConsumeHours`はinternalで、シミュレーションの割当処理から呼ぶ。`ResetWeeklyHours`はprivateで、疲労更新後の`CompleteWeek`からのみ呼ぶ。外部からの工数消費は`TryAllocateHours`、週次進行は`AdvanceWeek`を使う。
- 疲労は基本スキルと独立しており、スキルの種類・レベルを変更しない。増減式と倍率は[Fatigue](./Fatigue.md)を参照。
