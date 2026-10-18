using System;
using System.Collections.Generic;

/// <summary>
/// 全タスク種別の完了効果と負債の影響対象を定義する。初期バランスの調整箇所。
/// </summary>
public static class TaskProductProfiles
{
    private static readonly IReadOnlyDictionary<TaskType, TaskProductProfile> Profiles =
        new Dictionary<TaskType, TaskProductProfile>
        {
            { TaskType.FrontendImplementation, new TaskProductProfile(0.10m, -0.05m, true) },
            { TaskType.BackendImplementation, new TaskProductProfile(0.10m, -0.05m, true) },
            { TaskType.Database, new TaskProductProfile(0m, 0m, true) },
            { TaskType.Infrastructure, new TaskProductProfile(0m, 0m, true) },
            { TaskType.Testing, new TaskProductProfile(0m, 0.20m, false) },
            { TaskType.Security, new TaskProductProfile(0m, 0.10m, false) },
            { TaskType.Architecture, new TaskProductProfile(-0.20m, 0.05m, false) },
            { TaskType.Planning, new TaskProductProfile(0m, 0m, false) },
            { TaskType.Management, new TaskProductProfile(0m, 0m, false) },
            { TaskType.CustomerSupport, new TaskProductProfile(0m, 0m, false) }
        };

    /// <exception cref="ArgumentOutOfRangeException">typeが未定義の場合</exception>
    public static TaskProductProfile Get(TaskType type)
    {
        if (!Profiles.TryGetValue(type, out var profile))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "定義済みのTaskTypeが必要です。");
        }

        return profile;
    }
}
