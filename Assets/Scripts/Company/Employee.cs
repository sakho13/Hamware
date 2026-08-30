using System;

/// <summary>
/// 社員を表すドメインモデル。
/// 名前と1週間あたりの利用可能工数を持つ。
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
    }
}
