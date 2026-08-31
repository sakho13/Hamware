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
    public void TryAllocateHours_SingleEmployeeMultipleTasks_Succeeds()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        simulation.AddEmployee(hamda);
        var login = new DevelopmentTask("ログイン機能", 30);
        var csv = new DevelopmentTask("CSV出力", 40);
        simulation.AddTask(login);
        simulation.AddTask(csv);

        var result1 = simulation.TryAllocateHours(hamda, login, 20);
        var result2 = simulation.TryAllocateHours(hamda, csv, 20);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        Assert.AreEqual(40, simulation.AllocatedHours);
        Assert.AreEqual(0, hamda.AvailableHours);
    }

    [Test]
    public void TryAllocateHours_MultipleEmployeesSameTask_Succeeds()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        var hamkawa = new Employee("ハム川", 40);
        simulation.AddEmployee(hamda);
        simulation.AddEmployee(hamkawa);
        var login = new DevelopmentTask("ログイン機能", 60);
        simulation.AddTask(login);

        var result1 = simulation.TryAllocateHours(hamda, login, 30);
        var result2 = simulation.TryAllocateHours(hamkawa, login, 20);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        Assert.AreEqual(50, simulation.AllocatedHours);
    }

    [Test]
    public void TryAllocateHours_ExceedsEmployeeWeeklyHours_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        simulation.AddEmployee(hamda);
        var task = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(task);

        var result = simulation.TryAllocateHours(hamda, task, 41);

        Assert.IsFalse(result);
        Assert.AreEqual(0, simulation.AllocatedHours);
        Assert.AreEqual(40, hamda.AvailableHours);
    }

    [Test]
    public void TryAllocateHours_ZeroOrNegativeHours_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        simulation.AddEmployee(hamda);
        var task = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(task);

        Assert.IsFalse(simulation.TryAllocateHours(hamda, task, 0));
        Assert.IsFalse(simulation.TryAllocateHours(hamda, task, -10));
    }

    [Test]
    public void TryAllocateHours_UnregisteredEmployee_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var registeredTask = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(registeredTask);
        var unregisteredEmployee = new Employee("ハム鈴", 40);

        var result = simulation.TryAllocateHours(unregisteredEmployee, registeredTask, 10);

        Assert.IsFalse(result);
        Assert.AreEqual(40, unregisteredEmployee.AvailableHours);
        Assert.AreEqual(0, simulation.AllocatedHours);
    }

    [Test]
    public void TryAllocateHours_UnregisteredTask_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        simulation.AddEmployee(hamda);
        var unregisteredTask = new DevelopmentTask("未登録タスク", 100);

        var result = simulation.TryAllocateHours(hamda, unregisteredTask, 10);

        Assert.IsFalse(result);
        Assert.AreEqual(40, hamda.AvailableHours);
        Assert.AreEqual(0, simulation.AllocatedHours);
    }

    [Test]
    public void TryAllocateHours_CompletedTask_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        simulation.AddEmployee(hamda);
        var task = new DevelopmentTask("小さいタスク", 10);
        simulation.AddTask(task);
        simulation.TryAllocateHours(hamda, task, 10);
        simulation.AdvanceWeek();

        Assert.IsTrue(task.IsCompleted);

        var result = simulation.TryAllocateHours(hamda, task, 5);

        Assert.IsFalse(result);
        Assert.AreEqual(40, hamda.AvailableHours);
    }

    [Test]
    public void AdvanceWeek_SumsMultipleEmployeeAllocationsIntoTaskProgress()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        var hamkawa = new Employee("ハム川", 40);
        simulation.AddEmployee(hamda);
        simulation.AddEmployee(hamkawa);
        var login = new DevelopmentTask("ログイン機能", 60);
        simulation.AddTask(login);
        simulation.TryAllocateHours(hamda, login, 30);
        simulation.TryAllocateHours(hamkawa, login, 20);

        simulation.AdvanceWeek();

        Assert.AreEqual(50, login.CompletedHours);
    }

    [Test]
    public void AdvanceWeek_ResetsEachEmployeeAndTeamAvailableHours()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        var hamkawa = new Employee("ハム川", 40);
        simulation.AddEmployee(hamda);
        simulation.AddEmployee(hamkawa);
        var task = new DevelopmentTask("ログイン機能", 60);
        simulation.AddTask(task);
        simulation.TryAllocateHours(hamda, task, 30);
        simulation.TryAllocateHours(hamkawa, task, 20);

        Assert.AreEqual(10, hamda.AvailableHours);
        Assert.AreEqual(20, hamkawa.AvailableHours);

        simulation.AdvanceWeek();

        Assert.AreEqual(40, hamda.AvailableHours);
        Assert.AreEqual(40, hamkawa.AvailableHours);
        Assert.AreEqual(80, simulation.AvailableHours);
    }

    [Test]
    public void AdvanceWeek_ClearsPreviousWeekAllocationState()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        simulation.AddEmployee(hamda);
        var task = new DevelopmentTask("ログイン機能", 60);
        simulation.AddTask(task);
        simulation.TryAllocateHours(hamda, task, 20);

        simulation.AdvanceWeek();

        Assert.AreEqual(0, simulation.AllocatedHours);

        var result = simulation.TryAllocateHours(hamda, task, 40);
        Assert.IsTrue(result);
    }

    [Test]
    public void AdvanceWeek_MatchesIssueExampleScenario()
    {
        var simulation = new GameSimulation();
        var hamda = new Employee("ハム田", 40);
        var hamkawa = new Employee("ハム川", 40);
        simulation.AddEmployee(hamda);
        simulation.AddEmployee(hamkawa);
        var login = new DevelopmentTask("ログイン機能", 60);
        var csv = new DevelopmentTask("CSV出力", 40);
        simulation.AddTask(login);
        simulation.AddTask(csv);

        simulation.TryAllocateHours(hamda, login, 30);
        simulation.TryAllocateHours(hamda, csv, 10);
        simulation.TryAllocateHours(hamkawa, login, 20);
        simulation.TryAllocateHours(hamkawa, csv, 20);

        Assert.AreEqual(80, simulation.AllocatedHours);
        Assert.AreEqual(0, simulation.AvailableHours);

        simulation.AdvanceWeek();

        Assert.AreEqual(2, simulation.CurrentWeek);
        Assert.AreEqual(50, login.CompletedHours);
        Assert.AreEqual(30, csv.CompletedHours);
        Assert.AreEqual(80, simulation.AvailableHours);
    }
}
