using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class ProductHealthSimulationTests
{
    private static Employee CreateEmployee(string name = "社員")
    {
        var skills = Enum.GetValues(typeof(SkillType)).Cast<SkillType>().Select(type => new Skill(type, 2));
        return new Employee(name, 40, skills);
    }

    private static void Allocate(GameSimulation simulation, Employee employee, DevelopmentTask task, int hours)
    {
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, hours));
    }

    [Test]
    public void Constructor_DefaultAndCustomState_AreIndependentAndStartAtExpectedValues()
    {
        var first = new GameSimulation();
        var second = new GameSimulation(42m, 63m);
        Assert.AreEqual(10m, first.Product.TechnicalDebt);
        Assert.AreEqual(70m, first.Product.Quality);
        Assert.AreEqual(42m, second.Product.TechnicalDebt);
        Assert.AreEqual(63m, second.Product.Quality);
        Assert.AreNotSame(first.Product, second.Product);
        first.Product.ApplyChanges(5m, -2m);
        Assert.AreEqual(42m, second.Product.TechnicalDebt);
        Assert.AreEqual(63m, second.Product.Quality);
    }

    [TestCase(-1, 70)]
    [TestCase(101, 70)]
    [TestCase(10, -1)]
    [TestCase(10, 101)]
    public void Constructor_InvalidProductState_Throws(int debt, int quality)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSimulation(debt, quality));
    }

    [Test]
    public void AdvanceWeek_DefaultDebt_ReducesDevelopmentProgressByFivePercent()
    {
        var simulation = new GameSimulation();
        var task = new DevelopmentTask("機能", 100, TaskType.FrontendImplementation);
        Allocate(simulation, CreateEmployee(), task, 10);
        simulation.AdvanceWeek();
        Assert.AreEqual(9.5m, task.CompletedProgressHours);
        Assert.AreEqual(10m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(70m, simulation.Product.Quality);
    }

    [TestCase(TaskType.FrontendImplementation, 52, 69)]
    [TestCase(TaskType.BackendImplementation, 52, 69)]
    [TestCase(TaskType.Database, 50, 70)]
    [TestCase(TaskType.Infrastructure, 50, 70)]
    [TestCase(TaskType.Testing, 50, 74)]
    [TestCase(TaskType.Security, 50, 72)]
    [TestCase(TaskType.Architecture, 46, 71)]
    [TestCase(TaskType.Planning, 50, 70)]
    [TestCase(TaskType.Management, 50, 70)]
    [TestCase(TaskType.CustomerSupport, 50, 70)]
    public void AdvanceWeek_TaskCompletes_AppliesTypeAndSizeBasedEffect(TaskType type, int debt, int quality)
    {
        var simulation = new GameSimulation(50m);
        var task = new DevelopmentTask("完了タスク", 20, type);
        Allocate(simulation, CreateEmployee(), task, 40);
        simulation.AdvanceWeek();
        Assert.IsTrue(task.IsCompleted);
        Assert.AreEqual((decimal)debt, simulation.Product.TechnicalDebt);
        Assert.AreEqual((decimal)quality, simulation.Product.Quality);
    }

    [TestCase(TaskType.FrontendImplementation, 5)]
    [TestCase(TaskType.BackendImplementation, 5)]
    [TestCase(TaskType.Database, 5)]
    [TestCase(TaskType.Infrastructure, 5)]
    [TestCase(TaskType.Testing, 10)]
    [TestCase(TaskType.Security, 10)]
    [TestCase(TaskType.Architecture, 10)]
    [TestCase(TaskType.Planning, 10)]
    [TestCase(TaskType.Management, 10)]
    [TestCase(TaskType.CustomerSupport, 10)]
    public void AdvanceWeek_HighDebt_OnlyReducesDevelopmentTypes(TaskType type, int highDebtProgress)
    {
        var low = new GameSimulation(0m);
        var high = new GameSimulation(100m);
        var lowTask = new DevelopmentTask("低負債", 1000, type);
        var highTask = new DevelopmentTask("高負債", 1000, type);
        Allocate(low, CreateEmployee(), lowTask, 10);
        Allocate(high, CreateEmployee(), highTask, 10);
        low.AdvanceWeek();
        high.AdvanceWeek();
        Assert.AreEqual(10m, lowTask.CompletedProgressHours);
        Assert.AreEqual((decimal)highDebtProgress, highTask.CompletedProgressHours);
    }

    [Test]
    public void AdvanceWeek_PartialTaskAcrossWeeks_AppliesEffectOnlyWhenItCompletes()
    {
        var simulation = new GameSimulation(0m);
        var employee = CreateEmployee();
        var task = new DevelopmentTask("機能", 20, TaskType.BackendImplementation);
        Allocate(simulation, employee, task, 10);
        simulation.AdvanceWeek();
        Assert.AreEqual(10m, task.CompletedProgressHours);
        Assert.AreEqual(0m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(70m, simulation.Product.Quality);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));
        simulation.AdvanceWeek();
        Assert.AreEqual(2m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(69m, simulation.Product.Quality);
        simulation.AdvanceWeek();
        Assert.AreEqual(2m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(69m, simulation.Product.Quality);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AdvanceWeek_MultipleEmployeesSplitAllocationsAndDuplicateRegistration_ApplyEffectOnce(bool reverse)
    {
        var simulation = new GameSimulation(0m);
        var task = new DevelopmentTask("機能", 20, TaskType.FrontendImplementation);
        var first = CreateEmployee("A");
        var second = CreateEmployee("B");
        var employees = reverse ? new[] { second, first } : new[] { first, second };
        foreach (var employee in employees)
        {
            Allocate(simulation, employee, task, 5);
            Allocate(simulation, employee, task, 5);
        }
        Assert.AreEqual(20, simulation.AllocatedHours);
        simulation.AdvanceWeek();
        Assert.IsTrue(task.IsCompleted);
        Assert.AreEqual(2m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(69m, simulation.Product.Quality);
        Assert.IsFalse(simulation.TryAllocateHours(first, task, 1));
        simulation.AdvanceWeek();
        Assert.AreEqual(2m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(69m, simulation.Product.Quality);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AdvanceWeek_ExternallyCompletedTask_DoesNotApplyRetroactiveEffect(bool completeAfterAllocation)
    {
        var simulation = new GameSimulation();
        var task = new DevelopmentTask("事前完了", 10, TaskType.FrontendImplementation);
        var employee = CreateEmployee();
        simulation.AddEmployee(employee);
        if (completeAfterAllocation) Allocate(simulation, employee, task, 10);
        task.ApplyProgress(10m);
        simulation.AddTask(task);
        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 1));
        simulation.AdvanceWeek();
        Assert.AreEqual(10m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(70m, simulation.Product.Quality);
    }

    [Test]
    public void AdvanceWeek_RejectedAllocationAndIdleWeek_DoNotChangeProduct()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee();
        var task = new DevelopmentTask("未着手", 100, TaskType.FrontendImplementation);
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 41));
        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 0));
        Assert.IsFalse(simulation.TryAllocateHours(employee, null, 10));
        Assert.IsFalse(simulation.TryAllocateHours(CreateEmployee(), task, 10));
        Assert.AreEqual(0, simulation.AllocatedHours);
        Assert.AreEqual(40, employee.AvailableHours);
        simulation.AdvanceWeek();
        Assert.AreEqual(0m, task.CompletedProgressHours);
        Assert.AreEqual(10m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(70m, simulation.Product.Quality);
    }

    [Test]
    public void AdvanceWeek_ExcessProgress_EffectUsesRequiredProgressAndHoursAreNotRefunded()
    {
        var simulation = new GameSimulation(50m);
        var employee = new Employee("高スキル", 40, new[] { new Skill(SkillType.Frontend, 5) });
        var task = new DevelopmentTask("小規模機能", 10, TaskType.FrontendImplementation);
        Allocate(simulation, employee, task, 40);
        Assert.AreEqual(0, employee.AvailableHours);
        Assert.AreEqual(40, simulation.AllocatedHours);
        simulation.AdvanceWeek();
        Assert.AreEqual(10m, task.CompletedProgressHours);
        Assert.AreEqual(51m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(69.5m, simulation.Product.Quality);
        Assert.AreEqual(10m, employee.Fatigue);
        Assert.AreEqual(40, employee.AvailableHours);
        Assert.AreEqual(0, simulation.AllocatedHours);
    }

    [Test]
    public void AdvanceWeek_SameTotalRequiredProgress_WholeAndSplitTasksHaveSameTotalEffect()
    {
        var whole = new GameSimulation(0m);
        var split = new GameSimulation(0m);
        Allocate(whole, CreateEmployee(), new DevelopmentTask("全体", 20, TaskType.FrontendImplementation), 20);
        var employee = CreateEmployee();
        Allocate(split, employee, new DevelopmentTask("前半", 10, TaskType.FrontendImplementation), 10);
        Allocate(split, employee, new DevelopmentTask("後半", 10, TaskType.FrontendImplementation), 10);
        whole.AdvanceWeek();
        split.AdvanceWeek();
        Assert.AreEqual(2m, whole.Product.TechnicalDebt);
        Assert.AreEqual(69m, whole.Product.Quality);
        Assert.AreEqual(whole.Product.TechnicalDebt, split.Product.TechnicalDebt);
        Assert.AreEqual(whole.Product.Quality, split.Product.Quality);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AdvanceWeek_MixedCompletions_UseStartingDebtThenClampNetChangeRegardlessOfOrder(bool reverse)
    {
        var simulation = new GameSimulation(99m);
        var feature = new DevelopmentTask("機能", 20, TaskType.FrontendImplementation);
        var improvement = new DevelopmentTask("改善", 20, TaskType.Architecture);
        var ongoing = new DevelopmentTask("継続中", 1000, TaskType.FrontendImplementation);
        var probeEmployee = CreateEmployee("継続担当");
        var tasks = reverse ? new[] { ongoing, improvement, feature } : new[] { feature, improvement, ongoing };
        foreach (var task in tasks)
        {
            Allocate(simulation, task == ongoing ? probeEmployee : CreateEmployee(task.Name), task,
                task == feature ? 40 : task == improvement ? 20 : 10);
        }
        simulation.AdvanceWeek();
        Assert.AreEqual(5.05m, ongoing.CompletedProgressHours);
        Assert.AreEqual(97m, simulation.Product.TechnicalDebt, "99 + 2 - 4を合算してから制限する");
        Assert.AreEqual(70m, simulation.Product.Quality);
        Assert.IsTrue(simulation.TryAllocateHours(probeEmployee, ongoing, 10));
        simulation.AdvanceWeek();
        Assert.AreEqual(10.20m, ongoing.CompletedProgressHours, "翌週の進捗は負債97による5.15");
    }

    [TestCase(false, 1, 3)]
    [TestCase(true, 1, 3)]
    [TestCase(false, 99, 100)]
    [TestCase(true, 99, 100)]
    public void AdvanceWeek_MixedQualityEffects_ClampAfterSummingRegardlessOfOrder(bool reverse, int quality, int expected)
    {
        var simulation = new GameSimulation(0m, quality);
        var feature = new DevelopmentTask("機能", 40, TaskType.FrontendImplementation);
        var testing = new DevelopmentTask("テスト", 20, TaskType.Testing);
        foreach (var task in reverse ? new[] { testing, feature } : new[] { feature, testing })
        {
            Allocate(simulation, CreateEmployee(task.Name), task, task.RequiredProgressHours);
        }
        simulation.AdvanceWeek();
        Assert.AreEqual((decimal)expected, simulation.Product.Quality);
        Assert.AreEqual(4m, simulation.Product.TechnicalDebt);
    }

    [Test]
    public void AdvanceWeek_SkillFatigueAndDebt_CombineWithoutChangingHoursOrSkills()
    {
        var low = new GameSimulation(0m);
        var high = new GameSimulation(50m);
        var skills = new[] { new Skill(SkillType.Backend, 4), new Skill(SkillType.Database, 3), new Skill(SkillType.Testing, 2) };
        var lowEmployee = new Employee("低負債側", 40, skills, 50m);
        var highEmployee = new Employee("高負債側", 40, skills, 50m);
        var lowTask = new DevelopmentTask("機能", 1000, TaskType.BackendImplementation);
        var highTask = new DevelopmentTask("機能", 1000, TaskType.BackendImplementation);
        Allocate(low, lowEmployee, lowTask, 10);
        Allocate(high, highEmployee, highTask, 10);
        Assert.AreEqual(10, low.AllocatedHours);
        Assert.AreEqual(low.AllocatedHours, high.AllocatedHours);
        Assert.AreEqual(30, high.AvailableHours);
        Assert.AreEqual(low.AvailableHours, high.AvailableHours);
        Assert.AreEqual(40, high.TotalWeeklyHours);
        low.AdvanceWeek();
        high.AdvanceWeek();
        Assert.AreEqual(10.5m, lowTask.CompletedProgressHours);
        Assert.AreEqual(7.875m, highTask.CompletedProgressHours);
        Assert.AreEqual(30m, highEmployee.Fatigue);
        Assert.AreEqual(lowEmployee.Fatigue, highEmployee.Fatigue);
        Assert.AreEqual(40, highEmployee.AvailableHours);
        Assert.AreEqual(0, high.AllocatedHours);
        Assert.AreEqual(3, highEmployee.Skills.Count);
        foreach (var skill in skills)
        {
            Assert.IsTrue(highEmployee.TryGetSkill(skill.Type, out var actual));
            Assert.AreSame(skill, actual);
        }
        Assert.AreEqual(4, skills[0].Level);
        Assert.AreEqual(3, skills[1].Level);
        Assert.AreEqual(2, skills[2].Level);
    }

    [Test]
    public void AdvanceWeek_QualityAlone_DoesNotChangeProductivity()
    {
        var low = new GameSimulation(50m, 0m);
        var high = new GameSimulation(50m, 100m);
        var lowTask = new DevelopmentTask("機能", 1000, TaskType.FrontendImplementation);
        var highTask = new DevelopmentTask("機能", 1000, TaskType.FrontendImplementation);
        Allocate(low, CreateEmployee(), lowTask, 10);
        Allocate(high, CreateEmployee(), highTask, 10);
        low.AdvanceWeek();
        high.AdvanceWeek();
        Assert.AreEqual(7.5m, lowTask.CompletedProgressHours);
        Assert.AreEqual(lowTask.CompletedProgressHours, highTask.CompletedProgressHours);
    }

    [Test]
    public void AdvanceWeek_FiveWeekAcceptanceScenario_ShowsCostAndRecoveryFromInvestment()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee();
        var first = new DevelopmentTask("最初の機能", 19, TaskType.FrontendImplementation);
        Allocate(simulation, employee, first, 20);
        simulation.AdvanceWeek();
        Assert.AreEqual(19m, first.CompletedProgressHours);
        Assert.AreEqual(11.9m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(69.05m, simulation.Product.Quality);

        var ongoing = new DevelopmentTask("次の機能", 100, TaskType.FrontendImplementation);
        Allocate(simulation, employee, ongoing, 20);
        simulation.AdvanceWeek();
        Assert.AreEqual(18.81m, ongoing.CompletedProgressHours);

        Allocate(simulation, employee, new DevelopmentTask("設計改善", 20, TaskType.Architecture), 20);
        simulation.AdvanceWeek();
        Assert.AreEqual(18.81m, ongoing.CompletedProgressHours, "改善へ工数を割いた週は機能が進まない");
        Assert.AreEqual(7.9m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(70.05m, simulation.Product.Quality);

        Assert.IsTrue(simulation.TryAllocateHours(employee, ongoing, 20));
        simulation.AdvanceWeek();
        Assert.AreEqual(38.02m, ongoing.CompletedProgressHours, "改善翌週は19.21の進捗");
        Allocate(simulation, employee, new DevelopmentTask("自動テスト", 20, TaskType.Testing), 20);
        simulation.AdvanceWeek();
        Assert.AreEqual(7.9m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(74.05m, simulation.Product.Quality);
        Assert.AreEqual(0m, employee.Fatigue);
        Assert.AreEqual(6, simulation.CurrentWeek);
    }

    [Test]
    public void AdvanceWeek_LongFeatureDevelopmentAndImprovements_StayWithinBoundsAndRecover()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee();
        foreach (var type in Enumerable.Repeat(TaskType.FrontendImplementation, 160)
            .Concat(Enumerable.Repeat(TaskType.Architecture, 60)).Concat(Enumerable.Repeat(TaskType.Testing, 30)))
        {
            var task = new DevelopmentTask("継続作業", type == TaskType.Testing ? 20 : 10, type);
            Allocate(simulation, employee, task, 40);
            simulation.AdvanceWeek();
            Assert.IsTrue(task.IsCompleted);
            Assert.That(simulation.Product.TechnicalDebt, Is.InRange(0m, 100m));
            Assert.That(simulation.Product.Quality, Is.InRange(0m, 100m));
            if (simulation.CurrentWeek == 161)
            {
                Assert.AreEqual(100m, simulation.Product.TechnicalDebt);
                Assert.AreEqual(0m, simulation.Product.Quality);
            }
        }
        Assert.AreEqual(0m, simulation.Product.TechnicalDebt);
        Assert.AreEqual(100m, simulation.Product.Quality);
    }

    [Test]
    public void Process_RepeatedCompletedAllocation_DoesNotRepeatEffectOrMutateInput()
    {
        var product = new ProductState(0m);
        var employee = CreateEmployee();
        var task = new DevelopmentTask("機能", 20, TaskType.FrontendImplementation);
        var allocations = new Dictionary<DevelopmentTask, Dictionary<Employee, int>>
        {
            { task, new Dictionary<Employee, int> { { employee, 20 } } }
        };
        var processor = new WeekProcessor();
        processor.Process(allocations, product);
        processor.Process(allocations, product);
        Assert.AreEqual(2m, product.TechnicalDebt);
        Assert.AreEqual(69m, product.Quality);
        Assert.AreEqual(20, allocations[task][employee]);
        Assert.AreEqual(40, employee.AvailableHours);
        Assert.AreEqual(0m, employee.Fatigue);
    }

    [Test]
    public void Process_NullInput_RejectsBeforeMutatingAnyState()
    {
        var product = new ProductState();
        var employee = CreateEmployee();
        var task = new DevelopmentTask("機能", 10, TaskType.FrontendImplementation);
        var allocations = new Dictionary<DevelopmentTask, Dictionary<Employee, int>>
        {
            { task, new Dictionary<Employee, int> { { employee, 40 } } }
        };
        var processor = new WeekProcessor();
        Assert.Throws<ArgumentNullException>(() => processor.Process(allocations, null));
        Assert.Throws<ArgumentNullException>(() => processor.Process(null, product));
        Assert.AreEqual(0m, task.CompletedProgressHours);
        Assert.AreEqual(10m, product.TechnicalDebt);
        Assert.AreEqual(70m, product.Quality);
        Assert.AreEqual(40, allocations[task][employee]);
    }
}
