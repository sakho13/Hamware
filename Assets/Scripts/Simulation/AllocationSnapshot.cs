using System;

public enum AllocationFailure
{
    None, MissingEmployee, MissingTask, InvalidHours, UnregisteredEmployee,
    UnregisteredTask, CompletedTask, InsufficientHours, PlanningLocked
}

/// <summary>参照取得時の割当。可変の社員・タスク・辞書を公開しない。</summary>
public sealed class AllocationSnapshot
{
    public Guid EmployeeId { get; }
    public string EmployeeName { get; }
    public Guid TaskId { get; }
    public string TaskName { get; }
    public int Hours { get; }

    internal AllocationSnapshot(Employee employee, DevelopmentTask task, int hours)
    {
        EmployeeId = employee.Id;
        EmployeeName = employee.Name;
        TaskId = task.Id;
        TaskName = task.Name;
        Hours = hours;
    }
}
