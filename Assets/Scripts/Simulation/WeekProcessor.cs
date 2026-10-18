using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>開始時の条件で全計算を済ませてから、進捗と完了効果を一括反映する。</summary>
internal sealed class WeekProcessor
{
    internal void Process(IReadOnlyDictionary<DevelopmentTask, Dictionary<Employee, int>> allocations, ProductState product)
        => Prepare(allocations, product).Commit();

    internal PreparedWeek Prepare(IReadOnlyDictionary<DevelopmentTask, Dictionary<Employee, int>> allocations,
        ProductState product)
    {
        if (allocations == null) throw new ArgumentNullException(nameof(allocations));
        if (product == null) throw new ArgumentNullException(nameof(product));
        decimal debtMultiplier = ProductHealthCalculator.CalculateProductivityMultiplier(product.TechnicalDebt);
        var entries = new List<(DevelopmentTask Task, TaskWeekResult Result)>();
        foreach (var allocation in allocations)
        {
            var task = allocation.Key;
            if (task == null || allocation.Value == null)
                throw new ArgumentException("タスクと社員別の割当はnullにできません。", nameof(allocations));
            if (task.IsCompleted) continue;
            var profile = TaskSkillProfiles.Get(task.Type);
            var productProfile = TaskProductProfiles.Get(task.Type);
            decimal multiplier = productProfile.IsDebtSensitive ? debtMultiplier : 1m;
            var contributions = new List<WorkContribution>();
            foreach (var hours in allocation.Value)
                contributions.Add(new WorkContribution(hours.Key, profile, hours.Value, multiplier));
            entries.Add((task, new TaskWeekResult(task, contributions)));
        }
        return new PreparedWeek(entries, product);
    }
}

/// <summary>検証と結果の作成に成功した週だけを、GameSimulationから一度確定する。</summary>
internal sealed class PreparedWeek
{
    private readonly List<(DevelopmentTask Task, TaskWeekResult Result)> _entries;
    private readonly ProductState _product;
    private bool _committed;
    internal IReadOnlyList<TaskWeekResult> TaskResults { get; }
    internal ProductWeekResult ProductResult { get; }

    internal PreparedWeek(List<(DevelopmentTask Task, TaskWeekResult Result)> entries, ProductState product)
    {
        _entries = entries;
        _product = product;
        TaskResults = entries.Select(e => e.Result).ToList().AsReadOnly();
        ProductResult = new ProductWeekResult(product,
            TaskResults.Sum(t => t.TechnicalDebtChange), TaskResults.Sum(t => t.QualityChange));
    }

    internal void Commit()
    {
        if (_committed) throw new InvalidOperationException("この週は確定済みです。");
        foreach (var entry in _entries) entry.Task.ApplyProgress(entry.Result.AppliedProgress);
        _product.ApplyChanges(ProductResult.RequestedTechnicalDebtChange, ProductResult.RequestedQualityChange);
        _committed = true;
    }
}
