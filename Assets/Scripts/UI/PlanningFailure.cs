/// <summary>画面の操作拒否。ドメインの拒否理由はそのまま保持し、画面状態と混同しない。</summary>
public readonly struct PlanningFailure
{
    public bool IsReportOpen { get; }
    public AllocationFailure AllocationReason { get; }

    private PlanningFailure(bool reportOpen, AllocationFailure allocationReason)
    {
        IsReportOpen = reportOpen;
        AllocationReason = allocationReason;
    }

    public static PlanningFailure ReportOpen => new PlanningFailure(true, AllocationFailure.None);
    public static PlanningFailure FromAllocation(AllocationFailure reason) => new PlanningFailure(false, reason);
}
