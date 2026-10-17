using System;
using System.Collections.Generic;
using NUnit.Framework;

public class ProductivityCalculatorTests
{
    private readonly ProductivityCalculator _calculator = new ProductivityCalculator();

    [TestCase(0, 20)]
    [TestCase(1, 30)]
    [TestCase(2, 40)]
    [TestCase(3, 50)]
    [TestCase(4, 60)]
    [TestCase(5, 70)]
    public void CalculateProgress_LevelZeroThroughFive_UsesAgreedBalance(int level, int expectedProgress)
    {
        var employee = new Employee("社員", 40);
        if (level > 0) employee.AddSkill(new Skill(SkillType.Testing, level));

        var progress = _calculator.CalculateProgress(employee, TaskSkillProfiles.Get(TaskType.Testing), 40);

        Assert.AreEqual((decimal)expectedProgress, progress);
        Assert.AreEqual(40, employee.AvailableHours);
    }

    [Test]
    public void CalculateProgress_AllRelevantSkillsAtLevelTwo_IsNeutral([Values] TaskType type)
    {
        var profile = TaskSkillProfiles.Get(type);
        var employee = new Employee("社員", 40);
        foreach (var skill in profile.Weights.Keys) employee.AddSkill(new Skill(skill, 2));

        Assert.AreEqual(10m, _calculator.CalculateProgress(employee, profile, 10));
    }

    [Test]
    public void CalculateProgress_MissingSupportSkills_DoesNotRedistributeWeights()
    {
        var employee = new Employee("社員", 40, new[] { new Skill(SkillType.Backend, 4) });

        var progress = _calculator.CalculateProgress(employee, TaskSkillProfiles.Get(TaskType.BackendImplementation), 10);

        Assert.AreEqual(12m, progress);
    }

    [TestCase(SkillType.Backend, 7)]
    [TestCase(SkillType.Database, 2)]
    [TestCase(SkillType.Testing, 1)]
    public void CalculateProgress_RaisingRelatedSkill_IncreasesProgressByItsWeight(SkillType type, int expectedGain)
    {
        var employee = new Employee("社員", 40, new[] { new Skill(type, 1) });
        var profile = TaskSkillProfiles.Get(TaskType.BackendImplementation);
        var before = _calculator.CalculateProgress(employee, profile, 40);
        employee.AddSkill(new Skill(type, 2));

        var after = _calculator.CalculateProgress(employee, profile, 40);

        Assert.AreEqual((decimal)expectedGain, after - before);
    }

    [TestCase(SkillType.Frontend)]
    [TestCase(SkillType.Infrastructure)]
    [TestCase(SkillType.Security)]
    [TestCase(SkillType.Architecture)]
    [TestCase(SkillType.Planning)]
    [TestCase(SkillType.Management)]
    [TestCase(SkillType.Communication)]
    [TestCase(SkillType.Sales)]
    [TestCase(SkillType.CustomerSupport)]
    public void CalculateProgress_UnrelatedSkill_DoesNotAffectProgress(SkillType type)
    {
        var profile = TaskSkillProfiles.Get(TaskType.BackendImplementation);
        var employee = new Employee("社員", 40);
        var before = _calculator.CalculateProgress(employee, profile, 10);

        employee.AddSkill(new Skill(type, 5));

        Assert.AreEqual(before, _calculator.CalculateProgress(employee, profile, 10));
    }

    [Test]
    public void CalculateProgress_CustomProfile_ControlsSkillInfluence()
    {
        var employee = new Employee("社員", 40, new[]
        {
            new Skill(SkillType.Communication, 4), new Skill(SkillType.Sales, 2)
        });
        var profile = new TaskSkillProfile(new Dictionary<SkillType, decimal>
        {
            { SkillType.Communication, 0.5m }, { SkillType.Sales, 0.5m }
        });

        Assert.AreEqual(12.5m, _calculator.CalculateProgress(employee, profile, 10));
    }

    [Test]
    public void CalculateProgress_ZeroHours_ReturnsZero()
    {
        Assert.AreEqual(0m, _calculator.CalculateProgress(
            new Employee("社員", 40), TaskSkillProfiles.Get(TaskType.Testing), 0));
    }

    [Test]
    public void CalculateProgress_InvalidArguments_Throws()
    {
        var employee = new Employee("社員", 40);
        var profile = TaskSkillProfiles.Get(TaskType.Testing);

        Assert.Throws<ArgumentNullException>(() => _calculator.CalculateProgress(null, profile, 10));
        Assert.Throws<ArgumentNullException>(() => _calculator.CalculateProgress(employee, null, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.CalculateProgress(employee, profile, -1));
    }

    [Test]
    public void CalculateProgress_LargeWorkHours_DoesNotOverflowIntegerRange()
    {
        var employee = new Employee("社員", int.MaxValue, new[] { new Skill(SkillType.Testing, 5) });

        Assert.AreEqual(3758096382.25m, _calculator.CalculateProgress(
            employee, TaskSkillProfiles.Get(TaskType.Testing), int.MaxValue));
    }
}
