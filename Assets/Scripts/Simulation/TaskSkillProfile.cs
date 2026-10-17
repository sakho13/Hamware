using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// タスクの進捗に影響する基本スキルと重みの不変な対応。
/// 重みは正数、合計は1。含まれないスキルの影響は0。
/// </summary>
public sealed class TaskSkillProfile
{
    public IReadOnlyDictionary<SkillType, decimal> Weights { get; }

    /// <exception cref="ArgumentNullException">weightsがnullの場合</exception>
    /// <exception cref="ArgumentException">空、または重みの合計が1でない場合</exception>
    /// <exception cref="ArgumentOutOfRangeException">未定義スキル、または重みが0以下・1超の場合</exception>
    public TaskSkillProfile(IReadOnlyDictionary<SkillType, decimal> weights)
    {
        if (weights == null)
        {
            throw new ArgumentNullException(nameof(weights));
        }

        var copy = new Dictionary<SkillType, decimal>();
        decimal total = 0m;
        foreach (var entry in weights)
        {
            if (!Enum.IsDefined(typeof(SkillType), entry.Key) || entry.Value <= 0m || entry.Value > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(weights), "定義済みスキルと0より大きく1以下の重みが必要です。");
            }

            copy.Add(entry.Key, entry.Value);
            total += entry.Value;
        }

        if (copy.Count == 0 || total != 1m)
        {
            throw new ArgumentException("スキルの重みの合計は1である必要があります。", nameof(weights));
        }

        Weights = new ReadOnlyDictionary<SkillType, decimal>(copy);
    }
}
