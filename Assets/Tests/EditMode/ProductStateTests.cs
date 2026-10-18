using System;
using System.Reflection;
using NUnit.Framework;

public class ProductStateTests
{
    [Test]
    public void Constructor_Defaults_StartsWithDebt10AndQuality70()
    {
        var product = new ProductState();
        Assert.AreEqual(10m, product.TechnicalDebt);
        Assert.AreEqual(70m, product.Quality);
    }

    [TestCase(0, 0)]
    [TestCase(0, 100)]
    [TestCase(100, 0)]
    [TestCase(100, 100)]
    [TestCase(42, 63)]
    public void Constructor_ValidInitialValues_PreservesBoth(int debt, int quality)
    {
        var product = new ProductState(debt, quality);
        Assert.AreEqual((decimal)debt, product.TechnicalDebt);
        Assert.AreEqual((decimal)quality, product.Quality);
    }

    [TestCase(-1, 70, "initialTechnicalDebt")]
    [TestCase(101, 70, "initialTechnicalDebt")]
    [TestCase(10, -1, "initialQuality")]
    [TestCase(10, 101, "initialQuality")]
    public void Constructor_InvalidInitialValue_ThrowsWithParameter(int debt, int quality, string parameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ProductState(debt, quality));
        Assert.AreEqual(parameter, exception.ParamName);
    }

    [Test]
    public void ApplyChanges_FractionalChanges_UpdatesIndependentlyWithoutRounding()
    {
        var product = new ProductState(10.5m, 70.25m);
        product.ApplyChanges(-0.125m, 0.01m);
        Assert.AreEqual(10.375m, product.TechnicalDebt);
        Assert.AreEqual(70.26m, product.Quality);
    }

    [TestCase(95, 5, 20, -20, 100, 0)]
    [TestCase(5, 95, -20, 20, 0, 100)]
    [TestCase(100, 70, 5, -1, 100, 69)]
    [TestCase(10, 0, -2, -1, 8, 0)]
    public void ApplyChanges_OutsideBounds_ClampsEachMetric(int debt, int quality, int debtChange, int qualityChange,
        int expectedDebt, int expectedQuality)
    {
        var product = new ProductState(debt, quality);
        product.ApplyChanges(debtChange, qualityChange);
        Assert.AreEqual((decimal)expectedDebt, product.TechnicalDebt);
        Assert.AreEqual((decimal)expectedQuality, product.Quality);
    }

    [Test]
    public void ApplyChanges_ExtremeChanges_ClampsWithoutOverflow()
    {
        var product = new ProductState();
        product.ApplyChanges(decimal.MaxValue, decimal.MinValue);
        Assert.AreEqual(100m, product.TechnicalDebt);
        Assert.AreEqual(0m, product.Quality);
        product.ApplyChanges(decimal.MinValue, decimal.MaxValue);
        Assert.AreEqual(0m, product.TechnicalDebt);
        Assert.AreEqual(100m, product.Quality);
    }

    [Test]
    public void PublicApi_DoesNotExposeStateSettersOrUpdateOperation()
    {
        Assert.IsNull(typeof(ProductState).GetProperty(nameof(ProductState.TechnicalDebt)).GetSetMethod());
        Assert.IsNull(typeof(ProductState).GetProperty(nameof(ProductState.Quality)).GetSetMethod());
        Assert.IsNull(typeof(ProductState).GetMethod("ApplyChanges", BindingFlags.Public | BindingFlags.Instance));
        Assert.IsNull(typeof(GameSimulation).GetProperty(nameof(GameSimulation.Product)).GetSetMethod(true));
    }
}
