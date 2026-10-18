using NUnit.Framework;

public class ProductivitySimulationTests
{
    private static Employee EmployeeA()
    {
        return new Employee("社員A", 40, new[]
        {
            new Skill(SkillType.Backend, 4),
            new Skill(SkillType.Database, 3),
            new Skill(SkillType.Testing, 2)
        });
    }

    private static Employee EmployeeB()
    {
        return new Employee("社員B", 40, new[]
        {
            new Skill(SkillType.Backend, 1),
            new Skill(SkillType.Database, 1),
            new Skill(SkillType.Testing, 4)
        });
    }

    [Test]
    public void AdvanceWeek_IssueExample_ChangesBestEmployeeByTaskType()
    {
        var simulation = new GameSimulation();
        var a = EmployeeA();
        var b = EmployeeB();
        simulation.AddEmployee(a);
        simulation.AddEmployee(b);
        var backendA = new DevelopmentTask("Backend A", 100, TaskType.BackendImplementation);
        var backendB = new DevelopmentTask("Backend B", 100, TaskType.BackendImplementation);
        var testingA = new DevelopmentTask("Testing A", 100, TaskType.Testing);
        var testingB = new DevelopmentTask("Testing B", 100, TaskType.Testing);
        foreach (var task in new[] { backendA, backendB, testingA, testingB }) simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(a, backendA, 10));
        Assert.IsTrue(simulation.TryAllocateHours(b, backendB, 10));
        Assert.IsTrue(simulation.TryAllocateHours(a, testingA, 10));
        Assert.IsTrue(simulation.TryAllocateHours(b, testingB, 10));

        simulation.AdvanceWeek();

        Assert.AreEqual(14m, backendA.CompletedProgressHours);
        Assert.AreEqual(8.25m, backendB.CompletedProgressHours);
        Assert.AreEqual(10m, testingA.CompletedProgressHours);
        Assert.AreEqual(15m, testingB.CompletedProgressHours);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AdvanceWeek_MultipleEmployees_SumsAdjustedProgressRegardlessOfOrder(bool reverse)
    {
        var simulation = new GameSimulation();
        var a = EmployeeA();
        var b = EmployeeB();
        var task = new DevelopmentTask("Backend", 100, TaskType.BackendImplementation);
        simulation.AddTask(task);
        foreach (var employee in reverse ? new[] { b, a } : new[] { a, b })
        {
            simulation.AddEmployee(employee);
            Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));
        }
        Assert.AreEqual(0m, task.CompletedProgressHours);
        Assert.AreEqual(30, a.AvailableHours);
        Assert.AreEqual(30, b.AvailableHours);
        Assert.AreEqual(60, simulation.AvailableHours);
        Assert.AreEqual(20, simulation.AllocatedHours);

        simulation.AdvanceWeek();

        Assert.AreEqual(22.25m, task.CompletedProgressHours);
        Assert.AreEqual(0, simulation.AllocatedHours);
        Assert.AreEqual(80, simulation.AvailableHours);
        Assert.AreEqual(2, simulation.CurrentWeek);
        simulation.AdvanceWeek();
        Assert.AreEqual(22.25m, task.CompletedProgressHours, "翌週に同じ割当を二重反映しない");
    }

    [Test]
    public void AdvanceWeek_UnskilledEmployee_HalvesProgressButConsumesActualHours()
    {
        var simulation = new GameSimulation();
        var employee = new Employee("未習得", 40);
        var task = new DevelopmentTask("Backend", 100, TaskType.BackendImplementation);
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));
        Assert.AreEqual(30, employee.AvailableHours);
        Assert.AreEqual(10, simulation.AllocatedHours);

        simulation.AdvanceWeek();

        Assert.AreEqual(5m, task.CompletedProgressHours);
        Assert.AreEqual(40, employee.AvailableHours);
        Assert.AreEqual(0, employee.Skills.Count);
    }

    [TestCase(10, false, TestName = "AdvanceWeek_SingleAllocation_PreservesFractionalProgress")]
    [TestCase(1, false, TestName = "AdvanceWeek_SplitHoursWithinWeek_PreservesFractionalProgress")]
    [TestCase(1, true, TestName = "AdvanceWeek_SplitHoursAcrossWeeks_PreservesFractionalProgress")]
    public void AdvanceWeek_SplitHoursWithinWeekOrAcrossWeeks_PreservesFractionalProgress(
        int hoursPerAllocation, bool advanceAfterEachAllocation)
    {
        var simulation = new GameSimulation();
        var employee = EmployeeB();
        var task = new DevelopmentTask("Backend", 100, TaskType.BackendImplementation);
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        for (var hours = 0; hours < 10; hours += hoursPerAllocation)
        {
            Assert.IsTrue(simulation.TryAllocateHours(employee, task, hoursPerAllocation));
            if (advanceAfterEachAllocation)
            {
                simulation.AdvanceWeek();
            }
        }

        if (!advanceAfterEachAllocation)
        {
            simulation.AdvanceWeek();
        }

        Assert.AreEqual(8.25m, task.CompletedProgressHours);
    }

    [Test]
    public void AdvanceWeek_SkillsUpdatedAfterAllocation_UsesLevelsAtWeekEnd()
    {
        var simulation = new GameSimulation();
        var employee = new Employee("社員", 40, new[] { new Skill(SkillType.Testing, 1) });
        var task = new DevelopmentTask("Testing", 100, TaskType.Testing);
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));
        employee.AddSkill(new Skill(SkillType.Testing, 5));

        simulation.AdvanceWeek();

        Assert.AreEqual(17.5m, task.CompletedProgressHours);
    }

    [Test]
    public void AdvanceWeek_ProgressExceedsRequirement_CompletesAndRejectsFurtherAllocation()
    {
        var simulation = new GameSimulation();
        var employee = EmployeeA();
        var task = new DevelopmentTask("Backend", 10, TaskType.BackendImplementation);
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));
        Assert.AreEqual(30, employee.AvailableHours);
        Assert.AreEqual(10, simulation.AllocatedHours);

        simulation.AdvanceWeek();

        Assert.AreEqual(10m, task.CompletedProgressHours);
        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 1));
        Assert.AreEqual(40, employee.AvailableHours);
        Assert.AreEqual(0, simulation.AllocatedHours);
    }

    [Test]
    public void TryAllocateHours_RejectedExtraAllocation_PreservesExistingAllocation()
    {
        var simulation = new GameSimulation();
        var employee = EmployeeB();
        var task = new DevelopmentTask("Backend", 100, TaskType.BackendImplementation);
        simulation.AddEmployee(employee);
        simulation.AddTask(task);
        Assert.IsTrue(simulation.TryAllocateHours(employee, task, 10));

        Assert.IsFalse(simulation.TryAllocateHours(employee, task, 31));
        Assert.IsFalse(simulation.TryAllocateHours(null, task, 1));
        Assert.IsFalse(simulation.TryAllocateHours(employee, null, 1));

        Assert.AreEqual(30, employee.AvailableHours);
        Assert.AreEqual(10, simulation.AllocatedHours);
        simulation.AdvanceWeek();
        Assert.AreEqual(8.25m, task.CompletedProgressHours);
    }
}
