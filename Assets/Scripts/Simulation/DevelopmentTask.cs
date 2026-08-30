using System;

/// <summary>
/// 開発タスクを表すドメインモデル。
/// 名前・必要工数・完了済み工数を保持し、工数の進捗反映を行う。
/// </summary>
public class DevelopmentTask
{
    /// <summary>
    /// タスク名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// タスク完了に必要な工数(時間)
    /// </summary>
    public int RequiredHours { get; }

    /// <summary>
    /// これまでに完了した工数(時間)
    /// </summary>
    public int CompletedHours { get; private set; }

    /// <summary>
    /// 完了済み工数が必要工数以上になっている場合にtrue
    /// </summary>
    public bool IsCompleted => CompletedHours >= RequiredHours;

    /// <summary>
    /// 開発タスクを生成する。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">requiredHoursが0以下の場合</exception>
    public DevelopmentTask(string name, int requiredHours)
    {
        if (requiredHours <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredHours), "RequiredHoursは1以上である必要があります。");
        }

        Name = name;
        RequiredHours = requiredHours;
        CompletedHours = 0;
    }

    /// <summary>
    /// 工数をこのタスクの進捗へ反映する。
    /// 0以下の工数は無視する。
    /// 完了済みタスクへ過剰な工数を適用しても、完了済み工数はRequiredHoursを超えない。
    /// </summary>
    public void ApplyProgress(int hours)
    {
        if (hours <= 0)
        {
            return;
        }

        CompletedHours = Math.Min(CompletedHours + hours, RequiredHours);
    }
}
