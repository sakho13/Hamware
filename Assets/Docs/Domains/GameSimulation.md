# GameSimulation

GameSimulation はゲーム全体のシミュレーション進行を管理します。

- 各シミュレーションが1つの`ProductState`を生成し、`Product`から参照できる。初期状態は生成時に指定できる。初期値・値域・増減は[ProductHealth](./ProductHealth.md)を参照。
- 社員とタスクを登録できる。同じインスタンスの再登録は無視し、同名の別インスタンスは登録できる。双方が名前と独立したGuidのIdを持つ。
- 社員をタスクへ割り当て、工数を消費させることができる。
- 割当はタスク→社員→実勤務工数として保持する。`AvailableHours`と`AllocatedHours`は実時間を整数で表す。
- `AdvanceWeek()`は内部の`WeekProcessor`によるタスク・プロダクトの処理、社員の`CompleteWeek()`、割当クリア、週番号更新を取りまとめる。週次順序と状態変化の反映時期は[ProductHealth](./ProductHealth.md#週次順序)を参照。
- 無割当の社員も疲労の回復対象となる。社員の重複登録を入口で防ぎ、チームの利用可能工数の計上と週末更新を一人につき一度行う。
- 生産性の補正式とスキル対応表は別クラスで管理する。社員ごとの情報が失われるため、補正前にタスク単位で合算することはしない。
- 同一社員・同一タスクへの追加割当は実時間を加算する。完了済みタスクや残り勤務時間を超える割当は拒否し、失敗時の状態は変えない。
- 外部からの進捗反映や週次プロセッサーの直接呼出しは提供しない。タスクを進める公開経路は、工数割当と`AdvanceWeek()`を組み合わせる。

- `TrySetAllocatedHours`で当週の割当合計を指定する。0は取消。減らした分だけ社員へ勤務時間を戻し、疲労・進捗は変更しない。`out AllocationFailure`付きのオーバーロードで拒否理由を取得できる。
- `GetAllocations()`は不変の値スナップショットを返す。`Employees` / `Tasks` / `History`は変更できないビューで、Listへキャストしても変更できない。
- `AdvanceWeek()`は不変の`WeekResult`を返し、`History`と`LastWeekResult`へ記録する。入力検証・進捗・完了効果・疲労の計算と結果オブジェクトの作成を反映前に済ませる。詳細は[PlanningAndReports](./PlanningAndReports.md)。
