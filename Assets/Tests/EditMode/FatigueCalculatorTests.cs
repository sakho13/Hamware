using System;
using NUnit.Framework;

public class FatigueCalculatorTests
{
    [TestCase(0, 20)]
    [TestCase(10, 30)]
    [TestCase(20, 40)]
    [TestCase(30, 50)]
    [TestCase(40, 60)]
    public void CalculateNextFatigue_WeeklyUtilization_RecoversOrAccumulates(int workedHours, int expected)
    {
        Assert.AreEqual((decimal)expected, FatigueCalculator.CalculateNextFatigue(50m, workedHours, 40));
    }

    [TestCase(0, 0)]
    [TestCase(5, 0)]
    [TestCase(90, 100)]
    [TestCase(99, 100)]
    [TestCase(100, 100)]
    public void CalculateNextFatigue_AtBounds_ClampsResult(int fatigue, int expected)
    {
        int workedHours = fatigue < 50 ? 0 : 40;

        Assert.AreEqual((decimal)expected, FatigueCalculator.CalculateNextFatigue(fatigue, workedHours, 40));
    }

    [TestCase(10, 20)]
    [TestCase(20, 40)]
    [TestCase(40, 80)]
    public void CalculateNextFatigue_SameUtilizationDifferentCapacities_HasSameChange(int workedHours, int weeklyHours)
    {
        Assert.AreEqual(40m, FatigueCalculator.CalculateNextFatigue(50m, workedHours, weeklyHours));
    }

    [Test]
    public void CalculateNextFatigue_FractionalChange_PreservesPrecision()
    {
        Assert.AreEqual(32.5m, FatigueCalculator.CalculateNextFatigue(50m, 5, 16));
    }

    [Test]
    public void CalculateNextFatigue_LargeHours_DoesNotOverflowOrTruncateUtilization()
    {
        Assert.AreEqual(60m, FatigueCalculator.CalculateNextFatigue(50m, int.MaxValue, int.MaxValue));
        Assert.AreEqual(50m, FatigueCalculator.CalculateNextFatigue(50m, 1500000000, 2000000000));
    }

    [TestCase(0, 100)]
    [TestCase(50, 75)]
    [TestCase(100, 50)]
    public void CalculateProductivityMultiplier_Fatigue_ReducesProductivity(int fatigue, int percent)
    {
        Assert.AreEqual(percent / 100m, FatigueCalculator.CalculateProductivityMultiplier(fatigue));
    }

    [Test]
    public void CalculateProductivityMultiplier_FractionalFatigue_PreservesPrecision()
    {
        Assert.AreEqual(0.9875m, FatigueCalculator.CalculateProductivityMultiplier(2.5m));
    }

    [TestCase(-1)]
    [TestCase(101)]
    public void CalculateNextFatigue_InvalidCurrentFatigue_Throws(int fatigue)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => FatigueCalculator.CalculateNextFatigue(fatigue, 10, 40));

        Assert.AreEqual("currentFatigue", exception.ParamName);
    }

    [TestCase(-1)]
    [TestCase(101)]
    public void CalculateProductivityMultiplier_InvalidFatigue_Throws(int fatigue)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => FatigueCalculator.CalculateProductivityMultiplier(fatigue));

        Assert.AreEqual("fatigue", exception.ParamName);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CalculateNextFatigue_InvalidWeeklyHours_Throws(int weeklyHours)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => FatigueCalculator.CalculateNextFatigue(50m, 0, weeklyHours));

        Assert.AreEqual("weeklyAvailableHours", exception.ParamName);
    }

    [TestCase(-1)]
    [TestCase(41)]
    public void CalculateNextFatigue_InvalidWorkedHours_Throws(int workedHours)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => FatigueCalculator.CalculateNextFatigue(50m, workedHours, 40));

        Assert.AreEqual("workedHours", exception.ParamName);
    }
}
