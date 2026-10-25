using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>確定した一週間の値のコピー。ドメインオブジェクトへの可変参照を持たない。</summary>
public sealed class WeekResult
{
    public int Week { get; }
    public IReadOnlyList<EmployeeWeekResult> Employees { get; }
    public IReadOnlyList<TaskWeekResult> Tasks { get; }
    public ProductWeekResult Product { get; }
    public long WorkedHours { get; }
    public decimal AppliedProgress { get; }

    internal WeekResult(int week, IEnumerable<EmployeeWeekResult> employees,
        IEnumerable<TaskWeekResult> tasks, ProductWeekResult product)
    {
        Week = week;
        Employees = employees.ToList().AsReadOnly();
        Tasks = tasks.ToList().AsReadOnly();
        Product = product;
        WorkedHours = Employees.Sum(e => (long)e.WorkedHours);
        AppliedProgress = Tasks.Sum(t => t.AppliedProgress);
    }
}

public sealed class EmployeeWeekResult
{
    public Guid EmployeeId { get; }
    public string Name { get; }
    public int WorkedHours { get; }
    public decimal FatigueBefore { get; }
    public decimal FatigueAfter { get; }
    internal EmployeeWeekResult(Guid employeeId, string name, int workedHours, decimal fatigueBefore, decimal fatigueAfter)
    {
        EmployeeId = employeeId;
        Name = name;
        WorkedHours = workedHours;
        FatigueBefore = fatigueBefore;
        FatigueAfter = fatigueAfter;
    }
}

public sealed class WorkContribution
{
    public Guid EmployeeId { get; }
    public string EmployeeName { get; }
    public int WorkedHours { get; }
    public decimal SkillMultiplier { get; }
    public decimal FatigueMultiplier { get; }
    public decimal DebtMultiplier { get; }
    public decimal CalculatedProgress { get; }
    internal WorkContribution(Employee employee, TaskSkillProfile profile, int hours, decimal debtMultiplier)
    {
        if (employee == null) throw new ArgumentNullException(nameof(employee));
        if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours));
        var calculator = new ProductivityCalculator();
        EmployeeId = employee.Id;
        EmployeeName = employee.Name;
        WorkedHours = hours;
        SkillMultiplier = calculator.CalculateSkillMultiplier(employee, profile);
        FatigueMultiplier = FatigueCalculator.CalculateProductivityMultiplier(employee.Fatigue);
        DebtMultiplier = debtMultiplier;
        CalculatedProgress = calculator.CalculateProgress(employee, profile, hours) * debtMultiplier;
    }
}

public sealed class TaskWeekResult
{
    public Guid TaskId { get; }
    public string Name { get; }
    public TaskType Type { get; }
    public int RequiredProgress { get; }
    public decimal ProgressBefore { get; }
    public decimal ProgressAfter { get; }
    public decimal CalculatedProgress { get; }
    public decimal AppliedProgress { get; }
    public decimal DiscardedProgress => CalculatedProgress - AppliedProgress;
    public bool CompletedThisWeek { get; }
    public long WorkedHours { get; }
    public decimal TechnicalDebtChange { get; }
    public decimal QualityChange { get; }
    public IReadOnlyList<WorkContribution> Contributions { get; }

    internal TaskWeekResult(DevelopmentTask task, IReadOnlyList<WorkContribution> contributions,
        decimal calculatedProgress, DevelopmentTask.ProgressChange change, decimal debtChange, decimal qualityChange)
    {
        TaskId = task.Id;
        Name = task.Name;
        Type = task.Type;
        RequiredProgress = task.RequiredProgressHours;
        ProgressBefore = change.Before;
        ProgressAfter = change.After;
        Contributions = contributions.ToList().AsReadOnly();
        WorkedHours = Contributions.Sum(c => (long)c.WorkedHours);
        CalculatedProgress = calculatedProgress;
        AppliedProgress = change.Applied;
        CompletedThisWeek = change.CompletedThisWeek;
        TechnicalDebtChange = debtChange;
        QualityChange = qualityChange;
    }
}

public sealed class ProductWeekResult
{
    public decimal TechnicalDebtBefore { get; }
    public decimal TechnicalDebtAfter { get; }
    public decimal QualityBefore { get; }
    public decimal QualityAfter { get; }
    public decimal RequestedTechnicalDebtChange { get; }
    public decimal RequestedQualityChange { get; }
    public decimal AppliedTechnicalDebtChange => TechnicalDebtAfter - TechnicalDebtBefore;
    public decimal AppliedQualityChange => QualityAfter - QualityBefore;

    internal ProductWeekResult(decimal debtBefore, decimal qualityBefore, decimal debtAfter, decimal qualityAfter,
        decimal debtChange, decimal qualityChange)
    {
        TechnicalDebtBefore = debtBefore;
        QualityBefore = qualityBefore;
        RequestedTechnicalDebtChange = debtChange;
        RequestedQualityChange = qualityChange;
        TechnicalDebtAfter = debtAfter;
        QualityAfter = qualityAfter;
    }
}
