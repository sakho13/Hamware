using System;

/// <summary>
/// 技術的負債から進捗倍率を計算する。プロダクト・社員・タスクの状態は変更しない。
/// </summary>
public static class ProductHealthCalculator
{
    private const decimal MinProductivityMultiplier = 0.5m;

    /// <summary>
    /// 負債0で1、負債100で0.5となる線形の倍率を返す。影響対象の選択はタスク別定義で行う。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">technicalDebtが0〜100の範囲外の場合</exception>
    public static decimal CalculateProductivityMultiplier(decimal technicalDebt)
    {
        if (technicalDebt < ProductState.MinValue || technicalDebt > ProductState.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(technicalDebt), "技術的負債は0以上100以下である必要があります。");
        }

        decimal debtRatio = (technicalDebt - ProductState.MinValue) / (ProductState.MaxValue - ProductState.MinValue);
        return 1m - (1m - MinProductivityMultiplier) * debtRatio;
    }
}
