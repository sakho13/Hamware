using System.Collections.Generic;
using System.Linq;

/// <summary>登録・週内の計画・週次の確定を管理する入口。計算と状態更新は各ドメインへ委譲する。</summary>
public class GameSimulation
{
    private readonly List<DevelopmentTask> _tasks = new List<DevelopmentTask>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly List<WeekResult> _history = new List<WeekResult>();
    private readonly Dictionary<DevelopmentTask, Dictionary<Employee, int>> _allocations =
        new Dictionary<DevelopmentTask, Dictionary<Employee, int>>();
    private readonly WeekProcessor _weekProcessor = new WeekProcessor();

    /// <summary>このシミュレーションが所有するプロダクト。任意の状態更新は公開しない。</summary>
    public ProductState Product { get; }
    /// <summary>これから計画・確定する週番号。1ターンは1週間。</summary>
    public int CurrentWeek { get; private set; } = 1;
    /// <summary>全社員の当週の残り実勤務時間。</summary>
    public int AvailableHours => _employees.Sum(e => e.AvailableHours);
    /// <summary>当週の割当済み実勤務時間。補正後の進捗とは異なる。</summary>
    public int AllocatedHours => _allocations.Values.SelectMany(d => d.Values).Sum();
    /// <summary>在籍社員の週あたり勤務時間の上限合計。</summary>
    public int TotalWeeklyHours => _employees.Sum(e => e.WeeklyAvailableHours);
    /// <summary>登録順のタスク一覧。コレクションへの追加・削除はできない。</summary>
    public IReadOnlyList<DevelopmentTask> Tasks { get; }
    /// <summary>登録順の社員一覧。コレクションへの追加・削除はできない。</summary>
    public IReadOnlyList<Employee> Employees { get; }
    /// <summary>確定順の不変レポート。失敗した週は追加しない。</summary>
    public IReadOnlyList<WeekResult> History { get; }
    /// <summary>最後に確定した週の結果。初回確定前はnull。</summary>
    public WeekResult LastWeekResult => _history.Count == 0 ? null : _history[_history.Count - 1];

    /// <exception cref="System.ArgumentOutOfRangeException">初期負債または品質が0〜100の範囲外の場合。</exception>
    public GameSimulation(decimal initialTechnicalDebt = ProductState.DefaultTechnicalDebt,
        decimal initialQuality = ProductState.DefaultQuality)
    {
        Product = new ProductState(initialTechnicalDebt, initialQuality);
        Tasks = _tasks.AsReadOnly();
        Employees = _employees.AsReadOnly();
        History = _history.AsReadOnly();
    }

    /// <summary>タスクを登録する。null・同一インスタンスの再登録は無視し、同名の別インスタンスは許可する。</summary>
    public void AddTask(DevelopmentTask task)
    {
        if (task != null && !_tasks.Contains(task)) _tasks.Add(task);
    }

    /// <summary>社員を登録する。null・同一インスタンスの再登録は無視する。</summary>
    public void AddEmployee(Employee employee)
    {
        if (employee != null && !_employees.Contains(employee)) _employees.Add(employee);
    }

    /// <summary>取得時点の割当のコピー。後の編集・週送りでも変わらない。</summary>
    public IReadOnlyList<AllocationSnapshot> GetAllocations()
    {
        var result = new List<AllocationSnapshot>();
        foreach (var task in _tasks)
        {
            if (!_allocations.TryGetValue(task, out var hours)) continue;
            foreach (var employee in _employees)
                if (hours.TryGetValue(employee, out var value))
                    result.Add(new AllocationSnapshot(employee, task, value));
        }
        return result.AsReadOnly();
    }

    /// <summary>指定ペアの当週の合計実勤務時間。割当がない場合は0。</summary>
    public int GetAllocatedHours(Employee employee, DevelopmentTask task)
    {
        return employee != null && task != null &&
            _allocations.TryGetValue(task, out var employees) && employees.TryGetValue(employee, out var hours)
            ? hours : 0;
    }

    /// <summary>既存API。実勤務を加算する。0以下の追加は拒否する。</summary>
    public bool TryAllocateHours(Employee employee, DevelopmentTask task, int hours)
        => TryAllocateHours(employee, task, hours, out _);

    /// <summary>正の実勤務時間を追加する。未登録・完了済み・残時間超過は理由付きで拒否し、状態を変えない。</summary>
    public bool TryAllocateHours(Employee employee, DevelopmentTask task, int hours, out AllocationFailure failure)
    {
        failure = ValidateTarget(employee, task, hours);
        if (failure == AllocationFailure.None && hours == 0) failure = AllocationFailure.InvalidHours;
        if (failure != AllocationFailure.None) return false;
        if (hours > employee.AvailableHours)
        {
            failure = AllocationFailure.InsufficientHours;
            return false;
        }
        return TrySetAllocatedHours(employee, task, GetAllocatedHours(employee, task) + hours, out failure);
    }

    /// <summary>指定ペアの当週の合計時間を設定する。0で取消。過去の週は変更しない。</summary>
    public bool TrySetAllocatedHours(Employee employee, DevelopmentTask task, int hours)
        => TrySetAllocatedHours(employee, task, hours, out _);

    /// <summary>当週の割当を指定量へ変更する。0は取消。検証失敗では何も変更しない。</summary>
    public bool TrySetAllocatedHours(Employee employee, DevelopmentTask task, int hours, out AllocationFailure failure)
    {
        failure = ValidateTarget(employee, task, hours);
        if (failure != AllocationFailure.None) return false;
        int previous = GetAllocatedHours(employee, task);
        int delta = hours - previous;
        if (delta > employee.AvailableHours)
        {
            failure = AllocationFailure.InsufficientHours;
            return false;
        }
        if (delta == 0) return true;
        if (delta > 0)
        {
            if (!employee.TryConsumeHours(delta))
            {
                failure = AllocationFailure.InsufficientHours;
                return false;
            }
        }
        else employee.ReturnAllocatedHours(-delta);

        if (hours == 0)
        {
            _allocations[task].Remove(employee);
            if (_allocations[task].Count == 0) _allocations.Remove(task);
        }
        else
        {
            if (!_allocations.TryGetValue(task, out var perEmployee))
            {
                perEmployee = new Dictionary<Employee, int>();
                _allocations.Add(task, perEmployee);
            }
            perEmployee[employee] = hours;
        }
        return true;
    }

    private AllocationFailure ValidateTarget(Employee employee, DevelopmentTask task, int hours)
    {
        if (employee == null) return AllocationFailure.MissingEmployee;
        if (task == null) return AllocationFailure.MissingTask;
        if (hours < 0) return AllocationFailure.InvalidHours;
        if (!_employees.Contains(employee)) return AllocationFailure.UnregisteredEmployee;
        if (!_tasks.Contains(task)) return AllocationFailure.UnregisteredTask;
        if (task.IsCompleted) return AllocationFailure.CompletedTask;
        return AllocationFailure.None;
    }

    /// <summary>
    /// 1. 次週番号・全割当を検証し、更新前のスキル・疲労・負債で計算する。
    /// 2. 各ドメインが計算した確定値で不変レポートを確保する。ここまでは状態を変えない。
    /// 3. 全変更を検証してから進捗、合算完了効果、社員の疲労・時間を確定する。
    /// 4. 割当をクリアし、週番号・履歴を更新する。新しい疲労・負債は翌週から効く。
    /// 単一スレッドで呼び出す。レポートには表示用の丸めを適用しない。
    /// </summary>
    /// <exception cref="System.OverflowException">週番号または計算結果が数値の範囲を超える場合。確定前に失敗する。</exception>
    /// <exception cref="System.InvalidOperationException">計算から確定の間に状態が変更された場合。確定前に失敗する。</exception>
    public WeekResult AdvanceWeek()
    {
        int nextWeek = checked(CurrentWeek + 1);
        var prepared = _weekProcessor.Prepare(_allocations, Product, _tasks, _employees);
        var result = new WeekResult(CurrentWeek, prepared.EmployeeResults, prepared.TaskResults, prepared.ProductResult);
        prepared.Commit();
        _allocations.Clear();
        CurrentWeek = nextWeek;
        _history.Add(result);
        return result;
    }
}
