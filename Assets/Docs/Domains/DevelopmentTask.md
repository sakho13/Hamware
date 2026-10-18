# DevelopmentTask

DevelopmentTask は開発対象となるタスクを表します。

- 生成時に名前、必要進捗工数、`TaskType`を指定する。必要進捗工数は1以上、種別は定義済みの値に限る。
- `TaskType`は生成後に変更できない。種別ごとの関連スキルは[TaskSkillProfile](./TaskSkillProfile.md)を参照。
- `RequiredProgressHours`は完了までに必要な基準進捗工数を整数で持つ。
- `CompletedProgressHours`はスキル・疲労・負債の補正後の累積進捗工数を`decimal`で持つ。社員の実際の消費勤務時間とは異なる。
- 進捗工数の単位は基準となる時間相当であり、`Employee.AvailableHours`などの実勤務時間とは区別する。
- `ApplyProgress(decimal progressHours)`は正の進捗をdecimalの精度で蓄積し、0以下は無視する。
- 進捗が必要工数に達すると完了扱いになる。必要工数を超える進捗は切り捨て、完了後に追加しても増えない。
- 週次処理で未完了から完了になったとき、`WeekProcessor`が種別と必要進捗工数によるプロダクトへの効果を一度だけ集計する。タスク自身はプロダクトを保持・変更しない。詳細は[ProductHealth](./ProductHealth.md)を参照。
