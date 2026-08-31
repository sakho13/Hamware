using System;
using NUnit.Framework;

public class EmployeeTests
{
    [Test]
    public void Constructor_SetsNameAndWeeklyAvailableHours()
    {
        var employee = new Employee("ハム田", 40);

        Assert.AreEqual("ハム田", employee.Name);
        Assert.AreEqual(40, employee.WeeklyAvailableHours);
    }

    [Test]
    public void Constructor_WeeklyAvailableHoursIsZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Employee("ハム田", 0));
    }

    [Test]
    public void Constructor_WeeklyAvailableHoursIsNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Employee("ハム田", -5));
    }

    [Test]
    public void Constructor_SetsAvailableHoursToWeeklyAvailableHours()
    {
        var employee = new Employee("ハム田", 40);

        Assert.AreEqual(40, employee.AvailableHours);
    }

    [Test]
    public void TryConsumeHours_ValidAmount_ReducesAvailableHoursAndReturnsTrue()
    {
        var employee = new Employee("ハム田", 40);

        var result = employee.TryConsumeHours(30);

        Assert.IsTrue(result);
        Assert.AreEqual(10, employee.AvailableHours);
    }

    [Test]
    public void TryConsumeHours_ZeroOrNegative_ReturnsFalseAndDoesNotChangeAvailableHours()
    {
        var employee = new Employee("ハム田", 40);

        Assert.IsFalse(employee.TryConsumeHours(0));
        Assert.IsFalse(employee.TryConsumeHours(-10));
        Assert.AreEqual(40, employee.AvailableHours);
    }

    [Test]
    public void TryConsumeHours_ExceedsAvailableHours_ReturnsFalseAndDoesNotChangeAvailableHours()
    {
        var employee = new Employee("ハム田", 40);

        var result = employee.TryConsumeHours(41);

        Assert.IsFalse(result);
        Assert.AreEqual(40, employee.AvailableHours);
    }

    [Test]
    public void TryConsumeHours_MultipleCalls_AccumulateCorrectly()
    {
        var employee = new Employee("ハム田", 40);

        Assert.IsTrue(employee.TryConsumeHours(30));
        Assert.IsTrue(employee.TryConsumeHours(10));
        Assert.AreEqual(0, employee.AvailableHours);
        Assert.IsFalse(employee.TryConsumeHours(1));
    }

    [Test]
    public void ResetWeeklyHours_RestoresAvailableHoursToWeeklyAvailableHours()
    {
        var employee = new Employee("ハム田", 40);
        employee.TryConsumeHours(30);

        employee.ResetWeeklyHours();

        Assert.AreEqual(40, employee.AvailableHours);
    }
}
