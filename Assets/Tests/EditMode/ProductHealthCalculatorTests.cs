using System;
using NUnit.Framework;

public class ProductHealthCalculatorTests
{
    [TestCase(0, 100)]
    [TestCase(10, 95)]
    [TestCase(50, 75)]
    [TestCase(100, 50)]
    public void CalculateProductivityMultiplier_ValidDebt_ReturnsExpectedRate(int debt, int expectedPercent)
    {
        Assert.AreEqual(expectedPercent / 100m, ProductHealthCalculator.CalculateProductivityMultiplier(debt));
    }

    [Test]
    public void CalculateProductivityMultiplier_FractionalDebt_PreservesFraction()
    {
        Assert.AreEqual(0.9405m, ProductHealthCalculator.CalculateProductivityMultiplier(11.9m));
    }

    [TestCase(-1)]
    [TestCase(101)]
    public void CalculateProductivityMultiplier_InvalidDebt_Throws(int debt)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductHealthCalculator.CalculateProductivityMultiplier(debt));
    }

    [Test]
    public void CalculateProductivityMultiplier_IncreasingDebt_StrictlyReducesPositiveMultiplier()
    {
        decimal previous = ProductHealthCalculator.CalculateProductivityMultiplier(0m);
        for (int debt = 1; debt <= 100; debt++)
        {
            decimal current = ProductHealthCalculator.CalculateProductivityMultiplier(debt);
            Assert.Less(current, previous);
            Assert.GreaterOrEqual(current, 0.5m);
            previous = current;
        }
    }
}
