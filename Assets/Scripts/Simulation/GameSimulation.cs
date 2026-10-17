using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゲーム全体のシミュレーション状態を保持するオーケストレーター。
/// 週・登録タスク・登録社員・当週の社員ごとの割当状態を管理し、
/// AdvanceWeek() で週次処理をまとめて実行する。
/// </summary>
public class GameSimulation
{
    private readonly List<DevelopmentTask> _tasks = new List<DevelopmentTask>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly Dictionary<DevelopmentTask, Dictionary<Employee, int>> _allocations =
        new Dictionary<DevelopmentTask, Dictionary<Employee, int>>();
    private readonly WeekProcessor _weekProcessor = new WeekProcessor();

    /// <summary>
    /// 現在の週 ゲームの基本単位
    /// </summary>
    public int CurrentWeek { get; private set; } = 1;

    /// <summary>
    /// 在籍社員のAvailableHoursの合計。
    /// 各社員のAvailableHoursから都度計算される。
    /// </summary>
    public int AvailableHours => _employees.Sum(e => e.AvailableHours);

    /// <summary>
    /// 今週、既にタスクへ割り当て済みの実勤務工数の合計(補正後の進捗ではない)
    /// </summary>
    public int AllocatedHours => _allocations.Values.SelectMany(d => d.Values).Sum();

    /// <summary>
    /// 登録されている開発タスク一覧
    /// </summary>
    public IReadOnlyList<DevelopmentTask> Tasks => _tasks;

    /// <summary>
    /// 登録されている社員一覧
    /// </summary>
    public IReadOnlyList<Employee> Employees => _employees;

    /// <summary>
    /// 在籍社員のWeeklyAvailableHoursの合計。
    /// </summary>
    public int TotalWeeklyHours => _employees.Sum(e => e.WeeklyAvailableHours);

    /// <summary>
    /// 開発タスクを登録する。
    /// </summary>
    public void AddTask(DevelopmentTask task)
    {
        if (task == null)
        {
            return;
        }

        _tasks.Add(task);
    }

    /// <summary>
    /// 社員を登録する。
    /// </summary>
    public void AddEmployee(Employee employee)
    {
        if (employee == null)
        {
            return;
        }

        _employees.Add(employee);
    }

    /// <summary>
    /// 指定した社員から指定したタスクへ工数を割り当てる。
    /// 以下のいずれかに該当する場合は割当を拒否しfalseを返す:
    /// - employeeまたはtaskがnull
    /// - hoursが0以下
    /// - employeeが未登録
    /// - taskが未登録
    /// - taskが完了済み
    /// - hoursがemployeeの残り利用可能工数を超える
    /// 割当に成功すると、employeeのAvailableHoursがhours分減少する。
    /// 同一社員から同一タスクへ複数回呼び出した場合は加算される。
    /// </summary>
    public bool TryAllocateHours(Employee employee, DevelopmentTask task, int hours)
    {
        if (employee == null || task == null || hours <= 0)
        {
            return false;
        }

        if (!_employees.Contains(employee) || !_tasks.Contains(task))
        {
            return false;
        }

        if (task.IsCompleted)
        {
            return false;
        }

        if (!employee.TryConsumeHours(hours))
        {
            return false;
        }

        if (!_allocations.TryGetValue(task, out var perEmployee))
        {
            perEmployee = new Dictionary<Employee, int>();
            _allocations[task] = perEmployee;
        }

        perEmployee.TryGetValue(employee, out var current);
        perEmployee[employee] = current + hours;

        return true;
    }

    /// <summary>
    /// 週を進める。
    /// 1. 更新前の疲労とスキルで社員ごとの割当工数を補正・合算し、全タスクの進捗へ反映する
    /// 2. 各社員の疲労を今週の消費勤務時間で更新し、翌週の利用可能工数を復元する
    /// 3. 割当状態をクリアする(翌週へ持ち越さない)
    /// 4. 週番号を1進める
    /// </summary>
    public void AdvanceWeek()
    {
        _weekProcessor.Process(_allocations);

        foreach (var employee in _employees.Distinct())
        {
            employee.CompleteWeek();
        }

        _allocations.Clear();
        CurrentWeek++;
    }
}
