using System;

/// <summary>
/// 社員を表すドメインモデル。
/// 名前と1週間あたりの利用可能工数を持ち、
/// 週内の残り利用可能工数を自身で管理する。
/// </summary>
public class Employee
{
    /// <summary>
    /// 社員名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 1週間あたりの利用可能工数(時間)
    /// </summary>
    public int WeeklyAvailableHours { get; }

    /// <summary>
    /// 今週まだ割り当てられていない残り利用可能工数(時間)。
    /// コンストラクタでWeeklyAvailableHoursへ初期化され、
    /// TryConsumeHoursで減少し、ResetWeeklyHoursでWeeklyAvailableHoursへ戻る。
    /// </summary>
    public int AvailableHours { get; private set; }

    /// <summary>
    /// 社員を生成する。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">weeklyAvailableHoursが0以下の場合</exception>
    public Employee(string name, int weeklyAvailableHours)
    {
        if (weeklyAvailableHours <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weeklyAvailableHours),
                "WeeklyAvailableHoursは1以上である必要があります。");
        }

        Name = name;
        WeeklyAvailableHours = weeklyAvailableHours;
        AvailableHours = weeklyAvailableHours;
    }

    /// <summary>
    /// 残り利用可能工数からhours分を消費する。
    /// 0以下の工数、または残り利用可能工数を超える消費は拒否しfalseを返す。
    /// 成功した場合のみAvailableHoursを減少させる。
    /// </summary>
    public bool TryConsumeHours(int hours)
    {
        if (hours <= 0 || hours > AvailableHours)
        {
            return false;
        }

        AvailableHours -= hours;
        return true;
    }

    /// <summary>
    /// 残り利用可能工数をWeeklyAvailableHoursへリセットする。
    /// 週次処理(AdvanceWeek)から呼び出される想定。
    /// </summary>
    public void ResetWeeklyHours()
    {
        AvailableHours = WeeklyAvailableHours;
    }
}
