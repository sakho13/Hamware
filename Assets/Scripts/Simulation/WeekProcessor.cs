using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 更新前のスキル・疲労・負債で社員ごとの進捗を計算する。
/// 状態変更と上限制御は各ドメインへ委譲し、新規完了の効果を合算する。
/// </summary>
internal sealed class WeekProcessor
{
    /// <summary>タスクとプロダクトだけを確定する。社員・割当辞書は変更しない。</summary>
    /// <exception cref="ArgumentNullException">割当またはプロダクトがnullの場合。</exception>
    /// <exception cref="ArgumentException">社員別割当がnullの場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException">勤務時間が負の場合。</exception>
    internal void Process(IReadOnlyDictionary<DevelopmentTask, Dictionary<Employee, int>> allocations, ProductState product)
    {
        Prepare(allocations, product).Commit();
    }

    /// <summary>
    /// 全入力の検証、ドメインによる変更結果の計算、週報用の値の確保を済ませる。
    /// 登録順を指定した場合、割当操作の順序によらずタスク・社員を登録順に報告する。
    /// 社員一覧を指定した場合、無割当社員を含め週末処理も準備する。
    /// </summary>
    /// <exception cref="ArgumentNullException">allocationsまたはproductがnullの場合。</exception>
    /// <exception cref="ArgumentException">割当の内容や登録順が不正な場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException">勤務時間が負の場合。</exception>
    /// <exception cref="OverflowException">進捗・完了効果の合計がdecimalの範囲を超える場合。</exception>
    internal PreparedWeek Prepare(IReadOnlyDictionary<DevelopmentTask, Dictionary<Employee, int>> allocations,
        ProductState product, IReadOnlyList<DevelopmentTask> taskOrder = null, IReadOnlyList<Employee> employees = null)
    {
        if (allocations == null) throw new ArgumentNullException(nameof(allocations));
        if (product == null) throw new ArgumentNullException(nameof(product));
        var tasks = taskOrder == null ? allocations.Keys.ToList() : taskOrder.Where(allocations.ContainsKey).ToList();
        if (tasks.Count != allocations.Count)
            throw new ArgumentException("登録済みタスクの順序に全割当を含めてください。", nameof(taskOrder));

        var changes = new List<IPreparedChange>();
        var taskResults = new List<TaskWeekResult>();
        var employeeResults = new List<EmployeeWeekResult>();
        decimal debtMultiplier = ProductHealthCalculator.CalculateProductivityMultiplier(product.TechnicalDebt);
        foreach (var task in tasks)
        {
            var allocation = allocations[task];
            if (allocation == null)
                throw new ArgumentException("社員別の割当はnullにできません。", nameof(allocations));
            if (task.IsCompleted) continue;
            var orderedEmployees = employees == null ? allocation.Keys.ToList() : employees.Where(allocation.ContainsKey).ToList();
            if (orderedEmployees.Count != allocation.Count)
                throw new ArgumentException("未登録社員の割当があります。", nameof(employees));

            var profile = TaskSkillProfiles.Get(task.Type);
            var productProfile = TaskProductProfiles.Get(task.Type);
            decimal multiplier = productProfile.IsDebtSensitive ? debtMultiplier : 1m;
            var contributions = orderedEmployees.Select(e => new WorkContribution(e, profile, allocation[e], multiplier)).ToList();
            decimal calculated = contributions.Sum(c => c.CalculatedProgress);
            var progress = task.PrepareProgress(calculated);
            changes.Add(progress);
            bool completed = progress.Result.CompletedThisWeek;
            taskResults.Add(new TaskWeekResult(task, contributions, calculated, progress.Result,
                completed ? task.RequiredProgressHours * productProfile.TechnicalDebtChangePerProgressHour : 0m,
                completed ? task.RequiredProgressHours * productProfile.QualityChangePerProgressHour : 0m));
        }

        var productChange = product.PrepareChanges(taskResults.Sum(t => t.TechnicalDebtChange), taskResults.Sum(t => t.QualityChange));
        changes.Add(productChange);
        if (employees != null)
        {
            foreach (var employee in employees)
            {
                var completion = employee.PrepareWeekCompletion();
                changes.Add(completion);
                employeeResults.Add(completion.Result);
            }
        }
        return new PreparedWeek(changes, taskResults, employeeResults, productChange.Result);
    }
}

/// <summary>
/// 計算済みの週を一度だけ確定する。全所有者の状態を先に検証し、その後は代入だけを行う。
/// 結果は各ドメインの確定値と共通で、週報側には疲労式・クランプ・完了判定を持たせない。
/// </summary>
internal sealed class PreparedWeek
{
    private readonly IReadOnlyList<IPreparedChange> _changes;
    internal IReadOnlyList<TaskWeekResult> TaskResults { get; }
    internal IReadOnlyList<EmployeeWeekResult> EmployeeResults { get; }
    internal ProductWeekResult ProductResult { get; }

    internal PreparedWeek(IEnumerable<IPreparedChange> changes, IEnumerable<TaskWeekResult> tasks,
        IEnumerable<EmployeeWeekResult> employees, ProductWeekResult product)
    {
        _changes = changes.ToList().AsReadOnly();
        TaskResults = tasks.ToList().AsReadOnly();
        EmployeeResults = employees.ToList().AsReadOnly();
        ProductResult = product;
    }

    /// <exception cref="InvalidOperationException">再確定、または計算後に状態が変わった場合。状態更新前に拒否する。</exception>
    internal void Commit()
    {
        foreach (var change in _changes) change.Validate();
        foreach (var change in _changes) change.Commit();
    }
}
