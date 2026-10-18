using System;

/// <summary>
/// 開発タスクを表すドメインモデル。
/// 名前・タスク種別・必要進捗工数・完了済み進捗工数を保持する。
/// </summary>
public class DevelopmentTask
{
    /// <summary>
    /// タスク名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 基本スキルとの対応を決める仕事の種類
    /// </summary>
    public TaskType Type { get; }

    /// <summary>
    /// タスク完了に必要な基準進捗工数(時間相当)
    /// </summary>
    public int RequiredProgressHours { get; }

    /// <summary>
    /// 補正後の累積進捗工数(時間相当)。実際の消費勤務時間とは異なる。
    /// </summary>
    public decimal CompletedProgressHours { get; private set; }

    /// <summary>
    /// 完了済み進捗工数が必要進捗工数以上になっている場合にtrue
    /// </summary>
    public bool IsCompleted => CompletedProgressHours >= RequiredProgressHours;

    /// <summary>
    /// 開発タスクを生成する。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">requiredProgressHoursが0以下、またはtypeが未定義の場合</exception>
    public DevelopmentTask(string name, int requiredProgressHours, TaskType type)
    {
        if (requiredProgressHours <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredProgressHours), "RequiredProgressHoursは1以上である必要があります。");
        }

        if (!Enum.IsDefined(typeof(TaskType), type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "定義済みのTaskTypeが必要です。");
        }

        Name = name;
        Type = type;
        RequiredProgressHours = requiredProgressHours;
        CompletedProgressHours = 0;
    }

    /// <summary>
    /// 補正後の進捗工数を反映する。端数はdecimalの精度で蓄積する。
    /// 0以下の進捗工数は無視する。
    /// 完了済みタスクへ過剰な進捗を適用しても、累積進捗工数はRequiredProgressHoursを超えない。
    /// </summary>
    public void ApplyProgress(decimal progressHours)
    {
        if (progressHours <= 0)
        {
            return;
        }

        // 加算前に残工数で上限をかけ、大きな入力でもオーバーフローを避ける。
        CompletedProgressHours += Math.Min(progressHours, RequiredProgressHours - CompletedProgressHours);
    }
}
