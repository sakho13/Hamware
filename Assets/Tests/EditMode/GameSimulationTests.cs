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
}
