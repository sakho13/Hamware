/// <summary>試作画面の進行状態。週報を閉じるまで次の確定・計画編集を受け付けない。</summary>
public sealed class PrototypeSession
{
    public GameSimulation Simulation { get; private set; }
    public bool IsShowingReport { get; private set; }

    public PrototypeSession() { Restart(); }

    public bool TrySetHours(Employee employee, DevelopmentTask task, int hours, out AllocationFailure failure)
    {
        if (IsShowingReport)
        {
            failure = AllocationFailure.PlanningLocked;
            return false;
        }
        return Simulation.TrySetAllocatedHours(employee, task, hours, out failure);
    }

    public bool TryAdvanceWeek()
    {
        if (IsShowingReport) return false;
        Simulation.AdvanceWeek();
        IsShowingReport = true;
        return true;
    }

    public void ContinuePlanning() { IsShowingReport = false; }

    public void Restart()
    {
        Simulation = new GameSimulation();
        Simulation.AddEmployee(new Employee("ハム田", 40, new[]
        {
            new Skill(SkillType.Backend, 4), new Skill(SkillType.Database, 3),
            new Skill(SkillType.Testing, 2), new Skill(SkillType.Architecture, 3)
        }));
        Simulation.AddEmployee(new Employee("ハム川", 40, new[]
        {
            new Skill(SkillType.Frontend, 4), new Skill(SkillType.Testing, 4),
            new Skill(SkillType.Architecture, 2), new Skill(SkillType.Backend, 1)
        }));
        Simulation.AddTask(new DevelopmentTask("ログインAPI", 80, TaskType.BackendImplementation));
        Simulation.AddTask(new DevelopmentTask("ダッシュボード", 120, TaskType.FrontendImplementation));
        Simulation.AddTask(new DevelopmentTask("CSVエクスポート", 160, TaskType.BackendImplementation));
        Simulation.AddTask(new DevelopmentTask("通知画面", 160, TaskType.FrontendImplementation));
        Simulation.AddTask(new DevelopmentTask("自動テストを整備", 40, TaskType.Testing));
        Simulation.AddTask(new DevelopmentTask("データ層を整理", 40, TaskType.Architecture));
        Simulation.AddTask(new DevelopmentTask("回帰テストを拡充", 80, TaskType.Testing));
        Simulation.AddTask(new DevelopmentTask("API境界を整理", 80, TaskType.Architecture));
        IsShowingReport = false;
    }
}
