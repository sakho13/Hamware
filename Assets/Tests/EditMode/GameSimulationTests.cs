using NUnit.Framework;

public class GameSimulationTests
{
    [Test]
    public void AdvanceWeek_IncreasesCurrentWeek()
    {
        var simulation = new GameSimulation();

        simulation.AdvanceWeek();

        Assert.AreEqual(2, simulation.CurrentWeek);
    }

    [Test]
    public void AddEmployee_MultipleEmployees_AreAllRegistered()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        var hamkawa = new Employee("ハム川", 40);

        simulation.AddEmployee(hamda);
        simulation.AddEmployee(hamkawa);

        Assert.AreEqual(2, simulation.Employees.Count);
        CollectionAssert.Contains(simulation.Employees, hamda);
        CollectionAssert.Contains(simulation.Employees, hamkawa);
    }

    [Test]
    public void AddEmployee_ReflectsWeeklyTotalIntoAvailableHours()
    {
        var simulation = new GameSimulation();

        simulation.AddEmployee(new Employee("ハム田", 40));
        simulation.AddEmployee(new Employee("ハム川", 40));

        Assert.AreEqual(80, simulation.TotalWeeklyHours);
        Assert.AreEqual(80, simulation.AvailableHours);
    }

    [Test]
    public void TryAllocateHours_MultipleTasks_Succeeds()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40));
        var login = new DevelopmentTask("ログイン機能", 30);
        var csv = new DevelopmentTask("CSV出力", 40);
        simulation.AddTask(login);
        simulation.AddTask(csv);

        var result1 = simulation.TryAllocateHours(login, 20);
        var result2 = simulation.TryAllocateHours(csv, 20);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        Assert.AreEqual(40, simulation.AllocatedHours);
        Assert.AreEqual(0, simulation.AvailableHours);
    }

    [Test]
    public void TryAllocateHours_ExceedsAvailableHours_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40));
        var task = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(task);

        var result = simulation.TryAllocateHours(task, 41);

        Assert.IsFalse(result);
        Assert.AreEqual(0, simulation.AllocatedHours);
        Assert.AreEqual(40, simulation.AvailableHours);
    }

    [Test]
    public void TryAllocateHours_ZeroOrNegativeHours_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40));
        var task = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(task);

        Assert.IsFalse(simulation.TryAllocateHours(task, 0));
        Assert.IsFalse(simulation.TryAllocateHours(task, -10));
    }

    [Test]
    public void TryAllocateHours_NoEmployeesRegistered_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var task = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(task);

        var result = simulation.TryAllocateHours(task, 10);

        Assert.IsFalse(result);
    }

    [Test]
    public void AdvanceWeek_ReflectsAllocatedHoursIntoTaskProgress()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40));
        var task = new DevelopmentTask("ログイン機能", 30);
        simulation.AddTask(task);
        simulation.TryAllocateHours(task, 20);

        simulation.AdvanceWeek();

        Assert.AreEqual(20, task.CompletedHours);
    }

    [Test]
    public void AdvanceWeek_ResetsAvailableHoursToEmployeeTotal()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40));
        var task = new DevelopmentTask("ログイン機能", 30);
        simulation.AddTask(task);
        simulation.TryAllocateHours(task, 20);

        Assert.AreEqual(20, simulation.AvailableHours);

        simulation.AdvanceWeek();

        Assert.AreEqual(40, simulation.AvailableHours);
    }

    [Test]
    public void AdvanceWeek_ClearsAllocationState()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40));
        var task = new DevelopmentTask("ログイン機能", 30);
        simulation.AddTask(task);
        simulation.TryAllocateHours(task, 20);

        simulation.AdvanceWeek();

        Assert.AreEqual(0, simulation.AllocatedHours);
    }

    [Test]
    public void AdvanceWeek_MatchesIssueExampleScenario()
    {
        var simulation = new GameSimulation();
        simulation.AddEmployee(new Employee("ハム田", 40));
        simulation.AddEmployee(new Employee("ハム川", 40));
        var login = new DevelopmentTask("ログイン機能", 50);
        var csv = new DevelopmentTask("CSV出力", 30);
        simulation.AddTask(login);
        simulation.AddTask(csv);

        Assert.AreEqual(80, simulation.AvailableHours);

        simulation.TryAllocateHours(login, 50);
        simulation.TryAllocateHours(csv, 30);

        Assert.AreEqual(0, simulation.AvailableHours);

        simulation.AdvanceWeek();

        Assert.AreEqual(2, simulation.CurrentWeek);
        Assert.AreEqual(50, login.CompletedHours);
        Assert.AreEqual(30, csv.CompletedHours);
        Assert.AreEqual(80, simulation.AvailableHours);
    }
}
