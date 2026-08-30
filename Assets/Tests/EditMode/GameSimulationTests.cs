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
    public void TryAllocateHours_MultipleTasks_Succeeds()
    {
        var simulation = new GameSimulation();
        var login = new DevelopmentTask("ログイン機能", 30);
        var csv = new DevelopmentTask("CSV出力", 40);
        simulation.AddTask(login);
        simulation.AddTask(csv);

        var result1 = simulation.TryAllocateHours(login, 20);
        var result2 = simulation.TryAllocateHours(csv, 20);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        Assert.AreEqual(40, simulation.AllocatedHours);
    }

    [Test]
    public void TryAllocateHours_ExceedsAvailableHours_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var task = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(task);

        var result = simulation.TryAllocateHours(task, 41); // AvailableHours=40 を超過

        Assert.IsFalse(result);
        Assert.AreEqual(0, simulation.AllocatedHours);
    }

    [Test]
    public void TryAllocateHours_ZeroOrNegativeHours_ReturnsFalse()
    {
        var simulation = new GameSimulation();
        var task = new DevelopmentTask("CSV出力", 100);
        simulation.AddTask(task);

        Assert.IsFalse(simulation.TryAllocateHours(task, 0));
        Assert.IsFalse(simulation.TryAllocateHours(task, -10));
    }

    [Test]
    public void AdvanceWeek_ReflectsAllocatedHoursIntoTaskProgress()
    {
        var simulation = new GameSimulation();
        var task = new DevelopmentTask("ログイン機能", 30);
        simulation.AddTask(task);
        simulation.TryAllocateHours(task, 20);

        simulation.AdvanceWeek();

        Assert.AreEqual(20, task.CompletedHours);
    }

    [Test]
    public void AdvanceWeek_ResetsAvailableHours()
    {
        var simulation = new GameSimulation();
        var task = new DevelopmentTask("ログイン機能", 30);
        simulation.AddTask(task);
        simulation.TryAllocateHours(task, 20);

        simulation.AdvanceWeek();

        Assert.AreEqual(40, simulation.AvailableHours);
    }

    [Test]
    public void AdvanceWeek_ClearsAllocationState()
    {
        var simulation = new GameSimulation();
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
        var login = new DevelopmentTask("ログイン機能", 30);
        var csv = new DevelopmentTask("CSV出力", 40);
        simulation.AddTask(login);
        simulation.AddTask(csv);

        simulation.TryAllocateHours(login, 20);
        simulation.TryAllocateHours(csv, 20);
        simulation.AdvanceWeek();

        Assert.AreEqual(2, simulation.CurrentWeek);
        Assert.AreEqual(20, login.CompletedHours);
        Assert.AreEqual(20, csv.CompletedHours);
        Assert.AreEqual(40, simulation.AvailableHours);
    }
}
