using System;
using System.Collections.Generic;
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

    [Test]
    public void Constructor_NoInitialSkills_CreatesEmployeeWithEmptySkills()
    {
        var employee = new Employee("ハム田", 40);

        Assert.AreEqual(0, employee.Skills.Count);
    }

    [Test]
    public void Constructor_WithInitialSkills_SetsSkills()
    {
        var backend = new Skill(SkillType.Backend, 4);
        var database = new Skill(SkillType.Database, 3);

        var employee = new Employee("ハム田", 40, new List<Skill> { backend, database });

        Assert.AreEqual(2, employee.Skills.Count);
        CollectionAssert.Contains(employee.Skills, backend);
        CollectionAssert.Contains(employee.Skills, database);
    }

    [Test]
    public void Constructor_WithInitialSkillsContainingDuplicateType_LastOneWins()
    {
        var first = new Skill(SkillType.Backend, 2);
        var second = new Skill(SkillType.Backend, 4);

        var employee = new Employee("ハム田", 40, new List<Skill> { first, second });

        Assert.AreEqual(1, employee.Skills.Count);
        Assert.AreEqual(4, GetSkillLevel(employee, SkillType.Backend));
    }

    [Test]
    public void AddSkill_NewType_AddsToSkills()
    {
        var employee = new Employee("ハム田", 40);

        employee.AddSkill(new Skill(SkillType.Frontend, 3));

        Assert.AreEqual(1, employee.Skills.Count);
        Assert.AreEqual(3, GetSkillLevel(employee, SkillType.Frontend));
    }

    [Test]
    public void AddSkill_DuplicateType_OverwritesLevel()
    {
        var employee = new Employee("ハム田", 40);
        employee.AddSkill(new Skill(SkillType.Frontend, 2));

        employee.AddSkill(new Skill(SkillType.Frontend, 5));

        Assert.AreEqual(1, employee.Skills.Count);
        Assert.AreEqual(5, GetSkillLevel(employee, SkillType.Frontend));
    }

    [Test]
    public void AddSkill_Null_DoesNotThrowAndIsIgnored()
    {
        var employee = new Employee("ハム田", 40);

        Assert.DoesNotThrow(() => employee.AddSkill(null));
        Assert.AreEqual(0, employee.Skills.Count);
    }

    private static int GetSkillLevel(Employee employee, SkillType type)
    {
        foreach (var skill in employee.Skills)
        {
            if (skill.Type == type)
            {
                return skill.Level;
            }
        }

        Assert.Fail($"Skill of type {type} was not found.");
        return -1;
    }
}
