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
}
