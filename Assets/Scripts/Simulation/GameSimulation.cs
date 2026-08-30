using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゲーム全体のシミュレーション状態を保持するオーケストレーター。
/// 週・残り利用可能工数・登録タスク・当週の割当状態を管理し、
/// AdvanceWeek() で週次処理をまとめて実行する。
/// </summary>
public class GameSimulation
{
    private readonly List<DevelopmentTask> _tasks = new List<DevelopmentTask>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly Dictionary<DevelopmentTask, int> _allocations = new Dictionary<DevelopmentTask, int>();
    private readonly WeekProcessor _weekProcessor = new WeekProcessor();

    /// <summary>
    /// 現在の週 ゲームの基本単位
    /// </summary>
    public int CurrentWeek { get; private set; } = 1;

    /// <summary>
    /// 今週、まだ割り当てられていない残り工数。
    /// AddEmployeeで社員を登録するたびに増加し、TryAllocateHoursで割り当てるたびに減少し、
    /// AdvanceWeek()で在籍社員の合計工数へリセットされる。
    /// </summary>
    public int AvailableHours { get; private set; } = 0;

    /// <summary>
    /// 今週、既にタスクへ割り当て済みの工数の合計
    /// </summary>
    public int AllocatedHours => _allocations.Values.Sum();

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
    /// AdvanceWeek()実行後のAvailableHoursはこの値へリセットされる。
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
    /// 登録と同時に、その社員のWeeklyAvailableHours分だけAvailableHoursへ加算される。
    /// </summary>
    public void AddEmployee(Employee employee)
    {
        if (employee == null)
        {
            return;
        }

        _employees.Add(employee);
        AvailableHours += employee.WeeklyAvailableHours;
    }

    /// <summary>
    /// 指定タスクへ工数を割り当てる。
    /// 0以下の工数、または残り利用可能工数を超える割り当ては拒否しfalseを返す。
    /// 割当に成功すると、その分だけAvailableHoursが減少する。
    /// 同一タスクへ複数回呼び出した場合は加算される。
    /// </summary>
    public bool TryAllocateHours(DevelopmentTask task, int hours)
    {
        if (task == null || hours <= 0)
        {
            return false;
        }

        if (hours > AvailableHours)
        {
            return false;
        }

        _allocations.TryGetValue(task, out var current);
        _allocations[task] = current + hours;
        AvailableHours -= hours;
        return true;
    }

    /// <summary>
    /// 週を進める。
    /// 1. 今週の割当を各タスクの進捗へ反映する
    /// 2. 週番号を1進める
    /// 3. 利用可能工数を在籍社員の合計工数(TotalWeeklyHours)へ戻す
    /// 4. 割当状態をクリアする(翌週へ持ち越さない)
    /// </summary>
    public void AdvanceWeek()
    {
        _weekProcessor.Process(_allocations);

        _allocations.Clear();
        CurrentWeek++;
        AvailableHours = TotalWeeklyHours;
    }
}
