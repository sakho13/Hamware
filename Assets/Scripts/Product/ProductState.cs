using System;

/// <summary>
/// プロダクト全体の技術的負債と品質。週次の完了効果を自身で有効範囲に制限する。
/// </summary>
public sealed class ProductState
{
    public const decimal MinValue = 0m;
    public const decimal MaxValue = 100m;
    public const decimal DefaultTechnicalDebt = 10m;
    public const decimal DefaultQuality = 70m;

    public decimal TechnicalDebt { get; private set; }
    public decimal Quality { get; private set; }

    /// <exception cref="ArgumentOutOfRangeException">初期負債または初期品質が0〜100の範囲外の場合</exception>
    public ProductState(decimal initialTechnicalDebt = DefaultTechnicalDebt, decimal initialQuality = DefaultQuality)
    {
        ValidateValue(initialTechnicalDebt, nameof(initialTechnicalDebt));
        ValidateValue(initialQuality, nameof(initialQuality));
        TechnicalDebt = initialTechnicalDebt;
        Quality = initialQuality;
    }

    /// <summary>
    /// 当週の全完了効果を合算してから一度だけ反映する。外部からの任意更新は公開しない。
    /// </summary>
    internal void ApplyChanges(decimal technicalDebtChange, decimal qualityChange)
    {
        TechnicalDebt = ApplyClampedChange(TechnicalDebt, technicalDebtChange);
        Quality = ApplyClampedChange(Quality, qualityChange);
    }

    internal static decimal ApplyClampedChange(decimal current, decimal change)
    {
        // 加算前に増減量を制限し、decimalの最大・最小値でも加算をオーバーフローさせない。
        return current + Math.Max(MinValue - current, Math.Min(change, MaxValue - current));
    }

    private static void ValidateValue(decimal value, string paramName)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ArgumentOutOfRangeException(paramName, "プロダクトの指標は0以上100以下である必要があります。");
        }
    }
}
