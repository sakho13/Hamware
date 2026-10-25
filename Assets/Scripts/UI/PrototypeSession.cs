/// <summary>試作画面の進行状態。週報を閉じるまで次の確定・計画編集を受け付けない。</summary>
public sealed class PrototypeSession
{
    public GameSimulation Simulation { get; private set; }
    public bool IsShowingReport { get; private set; }

    public PrototypeSession() { Restart(); }

    /// <summary>任意のシナリオを使う画面・テスト用。計算は公開ドメインAPIへ委譲する。</summary>
    public PrototypeSession(GameSimulation simulation)
    {
        Simulation = simulation ?? throw new System.ArgumentNullException(nameof(simulation));
    }

    public bool TrySetHours(Employee employee, DevelopmentTask task, int hours, out PlanningFailure failure)
    {
        if (IsShowingReport)
        {
            failure = PlanningFailure.ReportOpen;
            return false;
        }
        bool success = Simulation.TrySetAllocatedHours(employee, task, hours, out var allocationReason);
        failure = PlanningFailure.FromAllocation(allocationReason);
        return success;
    }

    public bool TryAdvanceWeek()
    {
        if (IsShowingReport) return false;
        Simulation.AdvanceWeek();
        IsShowingReport = true;
        return true;
    }

    public void ContinuePlanning() { IsShowingReport = false; }

    public void Restart()
    {
        Simulation = PrototypeScenario.Create();
        IsShowingReport = false;
    }
}
