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

    public ProductState Product { get; }
    public int CurrentWeek { get; private set; } = 1;
    public int AvailableHours => _employees.Sum(e => e.AvailableHours);
    public int AllocatedHours => _allocations.Values.SelectMany(d => d.Values).Sum();
    public int TotalWeeklyHours => _employees.Sum(e => e.WeeklyAvailableHours);
    public IReadOnlyList<DevelopmentTask> Tasks { get; }
    public IReadOnlyList<Employee> Employees { get; }
    public IReadOnlyList<WeekResult> History { get; }
    public WeekResult LastWeekResult => _history.Count == 0 ? null : _history[_history.Count - 1];

    public GameSimulation(decimal initialTechnicalDebt = ProductState.DefaultTechnicalDebt,
        decimal initialQuality = ProductState.DefaultQuality)
    {
        Product = new ProductState(initialTechnicalDebt, initialQuality);
        Tasks = _tasks.AsReadOnly();
        Employees = _employees.AsReadOnly();
        History = _history.AsReadOnly();
    }

    public void AddTask(DevelopmentTask task)
    {
        if (task != null && !_tasks.Contains(task)) _tasks.Add(task);
    }

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

    public int GetAllocatedHours(Employee employee, DevelopmentTask task)
    {
        return employee != null && task != null &&
            _allocations.TryGetValue(task, out var employees) && employees.TryGetValue(employee, out var hours)
            ? hours : 0;
    }

    /// <summary>既存API。実勤務を加算する。0以下の追加は拒否する。</summary>
    public bool TryAllocateHours(Employee employee, DevelopmentTask task, int hours)
        => TryAllocateHours(employee, task, hours, out _);

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
        if (delta > 0) employee.TryConsumeHours(delta);
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

    /// <summary>全計算と結果作成に成功してから確定。現在週には更新前の条件を使う。</summary>
    public WeekResult AdvanceWeek()
    {
        int nextWeek = checked(CurrentWeek + 1);
        var prepared = _weekProcessor.Prepare(_allocations, Product);
        var employees = _employees.Select(e => new EmployeeWeekResult(e)).ToList();
        var result = new WeekResult(CurrentWeek, employees, prepared.TaskResults, prepared.ProductResult);
        prepared.Commit();
        foreach (var employee in _employees) employee.CompleteWeek();
        _allocations.Clear();
        CurrentWeek = nextWeek;
        _history.Add(result);
        return result;
    }
}
