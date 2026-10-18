using System;

/// <summary>
/// 稼働率による疲労の増減と、生産性への疲労倍率を計算する。状態は変更しない。
/// </summary>
public static class FatigueCalculator
{
    public const decimal MinFatigue = 0m;
    public const decimal MaxFatigue = 100m;

    private const decimal NeutralUtilization = 0.75m;
    private const decimal ChangePerUtilization = 40m;
    private const decimal MinProductivityMultiplier = 0.5m;

    /// <summary>
    /// 週の稼働率から翌週の疲労を計算し、有効範囲内に制限する。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// currentFatigueが0〜100の範囲外、weeklyAvailableHoursが0以下、
    /// workedHoursが0〜weeklyAvailableHoursの範囲外の場合。
    /// </exception>
    public static decimal CalculateNextFatigue(decimal currentFatigue, int workedHours, int weeklyAvailableHours)
    {
        ValidateFatigue(currentFatigue, nameof(currentFatigue));

        if (weeklyAvailableHours <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weeklyAvailableHours), "週の利用可能工数は1以上である必要があります。");
        }

        if (workedHours < 0 || workedHours > weeklyAvailableHours)
        {
            throw new ArgumentOutOfRangeException(nameof(workedHours), "実勤務工数は0以上かつ週の利用可能工数以下である必要があります。");
        }

        decimal utilization = (decimal)workedHours / weeklyAvailableHours;
        decimal change = ChangePerUtilization * (utilization - NeutralUtilization);
        return Math.Max(MinFatigue, Math.Min(MaxFatigue, currentFatigue + change));
    }

    /// <summary>
    /// 疲労の有効範囲に応じた線形の生産性倍率を返す。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">fatigueが0〜100の範囲外の場合</exception>
    public static decimal CalculateProductivityMultiplier(decimal fatigue)
    {
        ValidateFatigue(fatigue, nameof(fatigue));
        decimal fatigueRatio = (fatigue - MinFatigue) / (MaxFatigue - MinFatigue);
        return 1m - (1m - MinProductivityMultiplier) * fatigueRatio;
    }

    private static void ValidateFatigue(decimal fatigue, string paramName)
    {
        if (fatigue < MinFatigue || fatigue > MaxFatigue)
        {
            throw new ArgumentOutOfRangeException(paramName, "疲労は0以上100以下である必要があります。");
        }
    }
}
