using System;
using System.Collections.Generic;

/// <summary>
/// タスク種別ごとの初期スキル対応表。バランス調整はここで行う。
/// </summary>
public static class TaskSkillProfiles
{
    private static readonly IReadOnlyDictionary<TaskType, TaskSkillProfile> Profiles =
        new Dictionary<TaskType, TaskSkillProfile>
        {
            { TaskType.FrontendImplementation, Single(SkillType.Frontend) },
            { TaskType.BackendImplementation, new TaskSkillProfile(new Dictionary<SkillType, decimal>
                {
                    { SkillType.Backend, 0.7m },
                    { SkillType.Database, 0.2m },
                    { SkillType.Testing, 0.1m }
                })
            },
            { TaskType.Database, Single(SkillType.Database) },
            { TaskType.Infrastructure, Single(SkillType.Infrastructure) },
            { TaskType.Testing, Single(SkillType.Testing) },
            { TaskType.Security, Single(SkillType.Security) },
            { TaskType.Architecture, Single(SkillType.Architecture) },
            { TaskType.Planning, Single(SkillType.Planning) },
            { TaskType.Management, Single(SkillType.Management) },
            { TaskType.CustomerSupport, Single(SkillType.CustomerSupport) }
        };

    /// <exception cref="ArgumentOutOfRangeException">typeが未定義の場合</exception>
    public static TaskSkillProfile Get(TaskType type)
    {
        if (!Profiles.TryGetValue(type, out var profile))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "定義済みのTaskTypeが必要です。");
        }

        return profile;
    }

    private static TaskSkillProfile Single(SkillType type)
    {
        return new TaskSkillProfile(new Dictionary<SkillType, decimal> { { type, 1m } });
    }
}
