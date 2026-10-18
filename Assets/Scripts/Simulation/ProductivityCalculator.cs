using System;

/// <summary>
/// 基本スキルと疲労から実勤務工数を進捗工数へ変換する。社員やタスクの状態は変更しない。
/// </summary>
public sealed class ProductivityCalculator
{
    // 全関連スキルが未習得のときの倍率と、有効スキルレベル1あたりの増分。
    private const decimal BaseMultiplier = 0.5m;
    private const decimal MultiplierPerLevel = 0.25m;

    /// <summary>
    /// スキル倍率 = 0.5 + 0.25 × Σ(スキルレベル × 重み)。未習得は計算上0。
    /// 実勤務工数へスキル倍率と疲労倍率を乗算する。未習得の重みも維持し、decimal精度で返す。
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

        decimal skillMultiplier = BaseMultiplier + MultiplierPerLevel * effectiveLevel;
        return workedHours * skillMultiplier * FatigueCalculator.CalculateProductivityMultiplier(employee.Fatigue);
    }
}
