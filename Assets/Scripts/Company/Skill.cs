using System;

/// <summary>
/// 社員が保有する基本スキルを表す値オブジェクト。
/// 種類とレベルを持ち、レベルは有効範囲(MinLevel~MaxLevel)内でのみ設定できる。
/// 生成後は不変。同一種別のレベル更新はEmployee側でSkillを再生成して差し替える(upsert)想定。
/// これにより将来的なスキル成長(レベルアップ)は、新しいレベルのSkillをEmployeeへ
/// 再登録することで表現できる(成長処理自体は本クラス/本Issueの対象外)。
/// </summary>
public class Skill
{
    /// <summary>
    /// スキルレベルの最小値
    /// </summary>
    public const int MinLevel = 1;

    /// <summary>
    /// スキルレベルの最大値
    /// </summary>
    public const int MaxLevel = 5;

    /// <summary>
    /// スキルの種類
    /// </summary>
    public SkillType Type { get; }

    /// <summary>
    /// スキルレベル(MinLevel~MaxLevel)
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// スキルを生成する。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">levelがMinLevel未満またはMaxLevelを超える場合</exception>
    public Skill(SkillType type, int level)
    {
        if (level < MinLevel || level > MaxLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                $"Levelは{MinLevel}以上{MaxLevel}以下である必要があります。");
        }

        Type = type;
        Level = level;
    }
}
