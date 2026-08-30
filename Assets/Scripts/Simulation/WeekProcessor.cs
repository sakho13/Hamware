using System.Collections.Generic;

/// <summary>
/// その週に割り当てられた工数を、各タスクの進捗へ反映するドメインサービス。
/// 自身は状態を持たず、GameSimulationから週次処理のたびに呼び出される。
/// </summary>
public class WeekProcessor
{
    /// <summary>
    /// 割り当て(タスク→工数)を元に、各タスクの進捗を更新する。
    /// </summary>
    public void Process(IReadOnlyDictionary<DevelopmentTask, int> allocations)
    {
        foreach (var allocation in allocations)
        {
            allocation.Key.ApplyProgress(allocation.Value);
        }
    }
}
