using System;

/// <summary>
/// 基本スキルから実勤務工数を進捗工数へ変換する。社員やタスクの状態は変更しない。
/// </summary>
public sealed class ProductivityCalculator
{
    /// <summary>
    /// 倍率 = 0.5 + 0.25 × Σ(スキルレベル × 重み)。未習得は計算上0。
    /// 未習得の重みも維持し、端数を丸めずに返す。
    /// </summary>
    /// <exception cref="ArgumentNullException">employeeまたはprofileがnullの場合</exception>
    /// <exception cref="ArgumentOutOfRangeException">workedHoursが負の場合</exception>
    public decimal CalculateProgress(Employee employee, TaskSkillProfile profile, int workedHours)
    {
        if (employee == null)
        {
            throw new ArgumentNullException(nameof(employee));
        }

        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (workedHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workedHours), "実勤務工数は0以上である必要があります。");
        }

        decimal effectiveLevel = 0m;
        foreach (var entry in profile.Weights)
        {
            if (employee.TryGetSkill(entry.Key, out var skill))
            {
                effectiveLevel += skill.Level * entry.Value;
            }
        }

        return workedHours * (0.5m + 0.25m * effectiveLevel);
    }
}
