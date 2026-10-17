# TaskSkillProfile / ProductivityCalculator

`TaskSkillProfile`はタスクが参照する基本スキルと重みを保持する。定義は空にできず、定義済みの`SkillType`と正の重みを使用し、重みの合計を1とする。入力はコピーされ、公開する対応表も変更不可となる。

`TaskSkillProfiles.Get(TaskType)`で次の初期対応表を取得する。未定義のタスク種別は拒否する。

| タスク種別 | 基本スキルと重み |
|---|---|
| FrontendImplementation | Frontend: 100% |
| BackendImplementation | Backend: 70%、Database: 20%、Testing: 10% |
| Database | Database: 100% |
| Infrastructure | Infrastructure: 100% |
| Testing | Testing: 100% |
| Security | Security: 100% |
| Architecture | Architecture: 100% |
| Planning | Planning: 100% |
| Management | Management: 100% |
| CustomerSupport | CustomerSupport: 100% |

`ProductivityCalculator.CalculateProgress(Employee, TaskSkillProfile, int workedHours)`は、状態を変更せずに以下の計算結果を`decimal`で返す。

```text
有効スキルレベル = Σ(関連スキルレベル × 重み)
生産性倍率 = 0.5 + 0.25 × 有効スキルレベル
進捗工数 = 実勤務工数 × 生産性倍率
```

未習得は計算上0とし、固定の重みを維持する。`Skill`の範囲は1〜5のままで、未習得を表すスキルオブジェクトは作らない。対応表に含まれないスキルは影響しない。

| 有効スキルレベル | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---:|---:|---:|---:|---:|---:|
| 倍率 | 0.5 | 0.75 | 1 | 1.25 | 1.5 | 1.75 |

Backend 4 / Database 3 / Testing 2の社員がBackendImplementationへ10h勤務すると14h相当、Backend 1 / Database 1 / Testing 4なら8.25h相当の進捗になる。両社員が同じタスクへ参加すると22.25h相当の進捗になり、消費勤務時間は合計20h。

社員・プロフィールがnullの場合は`ArgumentNullException`、負の勤務工数は`ArgumentOutOfRangeException`となる。0時間の計算結果は0。勤務時間の消費や割当可否はこの計算サービスでは扱わない。

スキルは週送り時点で参照する。小数の進捗は丸めず、完了まで次週へ蓄積する。個別技術、職種、疲労、AI、スキル成長、UIは対象外。
