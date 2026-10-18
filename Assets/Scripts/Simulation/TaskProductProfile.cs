/// <summary>
/// タスク種別によるプロダクトへの完了効果と、負債の影響対象を表す不変な定義。
/// 増減係数の単位は、必要進捗1h相当あたりの指標増減。
/// </summary>
public sealed class TaskProductProfile
{
    public decimal TechnicalDebtChangePerProgressHour { get; }
    public decimal QualityChangePerProgressHour { get; }
    public bool IsDebtSensitive { get; }

    internal TaskProductProfile(decimal technicalDebtChangePerProgressHour, decimal qualityChangePerProgressHour,
        bool isDebtSensitive)
    {
        TechnicalDebtChangePerProgressHour = technicalDebtChangePerProgressHour;
        QualityChangePerProgressHour = qualityChangePerProgressHour;
        IsDebtSensitive = isDebtSensitive;
    }
}
