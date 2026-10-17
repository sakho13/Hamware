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
    public int RequiredHours { get; }

    /// <summary>
    /// 補正後の累積進捗工数(時間相当)。実際の消費勤務時間とは異なる。
    /// </summary>
    public decimal CompletedHours { get; private set; }

    /// <summary>
    /// 完了済み工数が必要工数以上になっている場合にtrue
    /// </summary>
    public bool IsCompleted => CompletedHours >= RequiredHours;

    /// <summary>
    /// 開発タスクを生成する。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">requiredHoursが0以下、またはtypeが未定義の場合</exception>
    public DevelopmentTask(string name, int requiredHours, TaskType type)
    {
        if (requiredHours <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredHours), "RequiredHoursは1以上である必要があります。");
        }

        if (!Enum.IsDefined(typeof(TaskType), type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "定義済みのTaskTypeが必要です。");
        }

        Name = name;
        Type = type;
        RequiredHours = requiredHours;
        CompletedHours = 0;
    }

    /// <summary>
    /// 補正後の進捗工数を反映する。端数は丸めずに蓄積する。
    /// 0以下の工数は無視する。
    /// 完了済みタスクへ過剰な工数を適用しても、完了済み工数はRequiredHoursを超えない。
    /// </summary>
    public void ApplyProgress(decimal hours)
    {
        if (hours <= 0)
        {
            return;
        }

        // 加算前に残工数で上限をかけ、大きな入力でもオーバーフローを避ける。
        CompletedHours += Math.Min(hours, RequiredHours - CompletedHours);
    }
}
