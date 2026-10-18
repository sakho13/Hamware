using NUnit.Framework;

public class FatigueSimulationTests
{
    private static Employee CreateEmployee(decimal fatigue = 0m, int weeklyHours = 40, int skillLevel = 2)
    {
        return new Employee("社員", weeklyHours, new[] { new Skill(SkillType.Testing, skillLevel) }, fatigue);
    }

    private static DevelopmentTask CreateTask(int requiredProgress = 1000)
    {
        return new DevelopmentTask("テスト作業", requiredProgress, TaskType.Testing);
    }

    [Test]
    public void AdvanceWeek_ContinuousFullWork_AccumulatesFatigueAndReducesNextWeeksProgress()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee();
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);

        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));
        Assert.AreEqual(0m, employee.Fatigue, "勤務時間の消費時点では疲労を更新しない");
        simulation.AdvanceWeek();
        Assert.AreEqual(40m, task.CompletedProgressHours);
        Assert.AreEqual(10m, employee.Fatigue);
        Assert.AreEqual(40, employee.AvailableHours);

        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));
        simulation.AdvanceWeek();

        Assert.AreEqual(78m, task.CompletedProgressHours, "2週目の進捗は更新前の疲労10で38になる");
        Assert.AreEqual(20m, employee.Fatigue);
        Assert.AreEqual(40, employee.AvailableHours);
        Assert.AreEqual(0, simulation.AllocatedHours);
        Assert.AreEqual(3, simulation.CurrentWeek);
    }

    [Test]
    public void AdvanceWeek_LowUtilization_RecoversFatigueAndImprovesFollowingWeek()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(50m);
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));

        simulation.AdvanceWeek();

        Assert.AreEqual(7.5m, task.CompletedProgressHours);
        Assert.AreEqual(30m, employee.Fatigue);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));
        simulation.AdvanceWeek();
        Assert.AreEqual(16m, task.CompletedProgressHours, "翌週は回復した疲労30で8.5の進捗を得る");
        Assert.AreEqual(10m, employee.Fatigue);
    }

    [Test]
    public void AdvanceWeek_NoTasksOrAllocations_RecoversUnassignedEmployee()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(50m);
        simulation.AddEmployee(employee);

        simulation.AdvanceWeek();
        Assert.AreEqual(20m, employee.Fatigue);
        simulation.AdvanceWeek();

        Assert.AreEqual(0m, employee.Fatigue);
        Assert.AreEqual(40, employee.AvailableHours);
    }

    [TestCase(1, 30)]
    [TestCase(5, 70)]
    public void AdvanceWeek_DifferentSkillProductivity_UsesActualHoursForFatigue(int level, int expectedProgress)
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(skillLevel: level);
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));

        simulation.AdvanceWeek();

        Assert.AreEqual((decimal)expectedProgress, task.CompletedProgressHours);
        Assert.AreEqual(10m, employee.Fatigue);
    }

    [Test]
    public void AdvanceWeek_TaskProgressClamped_StillCountsAllConsumedHours()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(skillLevel: 5);
        var task = CreateTask(1);
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));

        simulation.AdvanceWeek();

        Assert.AreEqual(1m, task.CompletedProgressHours);
        Assert.AreEqual(10m, employee.Fatigue);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AdvanceWeek_MultipleTasks_UsesSameFatigueAndUpdatesOnceRegardlessOfOrder(bool reverse)
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(40m);
        var first = CreateTask();
        var second = CreateTask();
        simulation.AddEmployee(employee);
        foreach (var task in reverse ? new[] { second, first } : new[] { first, second })
        {
            simulation.AddTask(task);
            Assert.IsTrue(simulation.TryAllocateHours(employee, task, 20));
        }

        simulation.AdvanceWeek();

        Assert.AreEqual(16m, first.CompletedProgressHours);
        Assert.AreEqual(16m, second.CompletedProgressHours);
        Assert.AreEqual(50m, employee.Fatigue);
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(40)]
    public void AdvanceWeek_SplitAllocationsWithinWeek_DoNotChangeFatigueOrProgress(int hoursPerAllocation)
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(50m);
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        for (int hours = 0; hours < 40; hours += hoursPerAllocation)
        {
            Assert.IsTrue(simulation.TryAllocateHours(employee, task, hoursPerAllocation));
        }

        simulation.AdvanceWeek();

        Assert.AreEqual(30m, task.CompletedProgressHours);
        Assert.AreEqual(60m, employee.Fatigue);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AdvanceWeek_MultipleEmployees_AppliesIndividualFatigueRegardlessOfOrder(bool reverse)
    {
        var simulation = new GameSimulation();
        var first = CreateEmployee(40m);
        var second = CreateEmployee(70m);
        var task = CreateTask();
        simulation.AddTask(task);
        foreach (var employee in reverse ? new[] { second, first } : new[] { first, second })
        {
            simulation.AddEmployee(employee);
            Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));
        }

        simulation.AdvanceWeek();

        Assert.AreEqual(58m, task.CompletedProgressHours);
        Assert.AreEqual(50m, first.Fatigue);
        Assert.AreEqual(80m, second.Fatigue);
    }

    [Test]
    public void TryAllocateHours_FullWeek_RejectsExtraWorkAndPreservesFatigueAccounting()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee();
        var first = CreateTask();
        var extra = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(first);
        simulation.AddTask(extra);
        Assert.IsTrue(simulation.TryAllocateHours(employee, first, 40));

        Assert.IsFalse(simulation.TryAllocateHours(employee, extra, 40));
        Assert.AreEqual(0, employee.AvailableHours);
        Assert.AreEqual(40, simulation.AllocatedHours);
        Assert.AreEqual(0m, employee.Fatigue);

        simulation.AdvanceWeek();

        Assert.AreEqual(40m, first.CompletedProgressHours);
        Assert.AreEqual(0m, extra.CompletedProgressHours);
        Assert.AreEqual(10m, employee.Fatigue);
        Assert.AreEqual(40, employee.AvailableHours);
    }

    [Test]
    public void AdvanceWeek_DifferentWeeklyCapacities_UsesUtilization()
    {
        var simulation = new GameSimulation();
        var partTime = CreateEmployee(weeklyHours: 20);
        var fullTime = CreateEmployee();
        var task = CreateTask();
        simulation.AddTask(task);
        simulation.AddEmployee(partTime);
        simulation.AddEmployee(fullTime);
        Assert.IsTrue(simulation.TryAllocateHours(partTime, task, 20));
        Assert.IsTrue(simulation.TryAllocateHours(fullTime, task, 40));

        simulation.AdvanceWeek();

        Assert.AreEqual(10m, partTime.Fatigue);
        Assert.AreEqual(10m, fullTime.Fatigue);
        Assert.AreEqual(20, partTime.AvailableHours);
        Assert.AreEqual(40, fullTime.AvailableHours);
    }

    [Test]
    public void TryAllocateHours_RejectedWork_DoesNotAffectFatigueOrWeeklyConsumption()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(50m);
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 30));

        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 11));
        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 0));
        Assert.IsFalse(simulation.TryAllocateHours(employee, null, 1));
        Assert.IsFalse(simulation.TryAllocateHours(employee, CreateTask(), 1));
        Assert.AreEqual(50m, employee.Fatigue);
        Assert.AreEqual(10, employee.AvailableHours);
        Assert.AreEqual(30, simulation.AllocatedHours);
        simulation.AdvanceWeek();

        Assert.AreEqual(22.5m, task.CompletedProgressHours);
        Assert.AreEqual(50m, employee.Fatigue, "成功した30hのみを中立稼働として扱う");
    }

    [Test]
    public void AdvanceWeek_PreviousAllocations_DoNotCarryOverIntoNextWeeksFatigue()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee();
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));
        simulation.AdvanceWeek();
        Assert.AreEqual(10m, employee.Fatigue);

        simulation.AdvanceWeek();

        Assert.AreEqual(0m, employee.Fatigue);
        Assert.AreEqual(40m, task.CompletedProgressHours);
        Assert.AreEqual(0, simulation.AllocatedHours);
    }

    [Test]
    public void AddEmployee_DuplicateRegistration_DoesNotIncreaseCapacityOrResetConsumedHours()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee(50m);
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 20));

        simulation.AddEmployee(employee);

        Assert.AreEqual(1, simulation.Employees.Count);
        Assert.AreEqual(40, simulation.TotalWeeklyHours);
        Assert.AreEqual(20, simulation.AvailableHours);
        Assert.AreEqual(20, employee.AvailableHours);
        Assert.AreEqual(20, simulation.AllocatedHours);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 20));
        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 1));

        simulation.AdvanceWeek();

        Assert.AreEqual(60m, employee.Fatigue);
        Assert.AreEqual(30m, task.CompletedProgressHours);
        Assert.AreEqual(40, employee.AvailableHours);
        Assert.AreEqual(40, simulation.AvailableHours);
    }

    [Test]
    public void AdvanceWeek_FatigueChanges_DoNotModifySkills()
    {
        var simulation = new GameSimulation();
        var backend = new Skill(SkillType.Backend, 4);
        var testing = new Skill(SkillType.Testing, 2);
        var employee = new Employee("社員", 40, new[] { backend, testing }, 50m);
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));

        simulation.AdvanceWeek();
        Assert.AreEqual(60m, employee.Fatigue);
        simulation.AdvanceWeek();

        Assert.AreEqual(30m, employee.Fatigue);
        Assert.AreEqual(2, employee.Skills.Count);
        Assert.IsTrue(employee.TryGetSkill(SkillType.Backend, out var actualBackend));
        Assert.IsTrue(employee.TryGetSkill(SkillType.Testing, out var actualTesting));
        Assert.AreSame(backend, actualBackend);
        Assert.AreSame(testing, actualTesting);
        Assert.AreEqual(4, actualBackend.Level);
        Assert.AreEqual(2, actualTesting.Level);
    }

    [Test]
    public void AdvanceWeek_LongWorkAndRest_KeepFatigueWithinBounds()
    {
        var simulation = new GameSimulation();
        var employee = CreateEmployee();
        var task = CreateTask();
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        for (int week = 0; week < 12; week++)
        {
            Assert.IsTrue(simulation.TryAllocateHours(employee, task, 40));
            simulation.AdvanceWeek();
        }

        Assert.AreEqual(100m, employee.Fatigue);
        for (int week = 0; week < 4; week++) simulation.AdvanceWeek();
        Assert.AreEqual(0m, employee.Fatigue);
    }
}
