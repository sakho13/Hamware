/// <summary>試作画面専用の固定シナリオ。シミュレーションのルールを含めない。</summary>
public static class PrototypeScenario
{
    public static GameSimulation Create()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40, new[]
        {
            new Skill(SkillType.Backend, 4), new Skill(SkillType.Database, 3),
            new Skill(SkillType.Testing, 2), new Skill(SkillType.Architecture, 3)
        }));
        simulation.AddEmployee(new Employee("ハム川", 40, new[]
        {
            new Skill(SkillType.Frontend, 4), new Skill(SkillType.Testing, 4),
            new Skill(SkillType.Architecture, 2), new Skill(SkillType.Backend, 1)
        }));
        simulation.AddTask(new DevelopmentTask("ログインAPI", 80, TaskType.BackendImplementation));
        simulation.AddTask(new DevelopmentTask("ダッシュボード", 120, TaskType.FrontendImplementation));
        simulation.AddTask(new DevelopmentTask("CSVエクスポート", 160, TaskType.BackendImplementation));
        simulation.AddTask(new DevelopmentTask("通知画面", 160, TaskType.FrontendImplementation));
        simulation.AddTask(new DevelopmentTask("自動テストを整備", 40, TaskType.Testing));
        simulation.AddTask(new DevelopmentTask("データ層を整理", 40, TaskType.Architecture));
        simulation.AddTask(new DevelopmentTask("回帰テストを拡充", 80, TaskType.Testing));
        simulation.AddTask(new DevelopmentTask("API境界を整理", 80, TaskType.Architecture));
        return simulation;
    }
}
