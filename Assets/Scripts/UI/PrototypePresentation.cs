using System.Collections.Generic;

/// <summary>ドメインの公開値を文言・表情へ変換する。数値ルールはドメインから参照する。</summary>
public static class PrototypePresentation
{
    // 表情の切替は表示上の設定。疲労の計算式とは独立している。
    public const decimal RestMessageThreshold = 30m;
    public const decimal SleepyFaceThreshold = 60m;

    public static string FatigueMessage(decimal fatigue)
        => fatigue >= SleepyFaceThreshold ? "少し休みたい…" :
            fatigue >= RestMessageThreshold ? "ひと息つこう" : "今日も、こつこつ。";

    public static string RestHint(int weeklyHours)
        => $"休養も、大切な仕事。\n週{weeklyHours}hのうち{weeklyHours * FatigueCalculator.NeutralUtilization:0.###}hで疲労は変わらず、\n少なく働くと回復します。";

    public static string Kind(TaskType type)
    {
        var profile = TaskProductProfiles.Get(type);
        if (profile.TechnicalDebtChangePerProgressHour < 0m) return "負債改善";
        if (profile.QualityChangePerProgressHour > 0m) return "品質改善";
        if (profile.TechnicalDebtChangePerProgressHour > 0m || profile.QualityChangePerProgressHour < 0m) return "機能開発";
        return "開発・運用";
    }

    public static string Effect(TaskType type)
    {
        var profile = TaskProductProfiles.Get(type);
        var effects = new List<string>();
        if (profile.TechnicalDebtChangePerProgressHour < 0m) effects.Add("負債が減ります");
        if (profile.TechnicalDebtChangePerProgressHour > 0m) effects.Add("負債が増えます");
        if (profile.QualityChangePerProgressHour < 0m) effects.Add("品質が下がります");
        if (profile.QualityChangePerProgressHour > 0m) effects.Add("品質が上がります");
        return effects.Count == 0 ? "完了による負債・品質の変化はありません。" : "完了すると" + string.Join("・", effects) + "。";
    }
}
