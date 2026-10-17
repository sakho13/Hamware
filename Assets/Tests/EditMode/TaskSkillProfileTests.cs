using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class TaskSkillProfileTests
{
    [Test]
    public void Constructor_CopiesInputAndExposesReadOnlyWeights()
    {
        var weights = new Dictionary<SkillType, decimal> { { SkillType.Testing, 1m } };
        var profile = new TaskSkillProfile(weights);

        weights.Clear();

        Assert.AreEqual(1m, profile.Weights[SkillType.Testing]);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<SkillType, decimal>)profile.Weights)[SkillType.Testing] = 0.5m);
    }

    [Test]
    public void Constructor_NullOrEmptyWeights_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TaskSkillProfile(null));
        Assert.Throws<ArgumentException>(() => new TaskSkillProfile(new Dictionary<SkillType, decimal>()));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(2)]
    public void Constructor_InvalidWeight_Throws(int weight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaskSkillProfile(
            new Dictionary<SkillType, decimal> { { SkillType.Testing, weight } }));
    }

    [Test]
    public void Constructor_VeryLargeWeight_ThrowsValidationException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaskSkillProfile(
            new Dictionary<SkillType, decimal> { { SkillType.Testing, decimal.MaxValue } }));
    }

    [TestCase(-1)]
    [TestCase(12)]
    [TestCase(999)]
    public void Constructor_UndefinedSkill_Throws(int type)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaskSkillProfile(
            new Dictionary<SkillType, decimal> { { (SkillType)type, 1m } }));
    }

    [TestCase(4)]
    [TestCase(6)]
    public void Constructor_WeightsNotSummingToOne_Throws(int tenths)
    {
        Assert.Throws<ArgumentException>(() => new TaskSkillProfile(new Dictionary<SkillType, decimal>
        {
            { SkillType.Backend, 0.5m },
            { SkillType.Testing, tenths / 10m }
        }));
    }

    [Test]
    public void Get_AllDefinedTaskTypes_HaveNormalizedProfiles([Values] TaskType type)
    {
        var profile = TaskSkillProfiles.Get(type);

        Assert.IsNotEmpty(profile.Weights);
        Assert.IsTrue(profile.Weights.Values.All(weight => weight > 0m));
        Assert.AreEqual(1m, profile.Weights.Values.Sum());
    }

    [TestCase(TaskType.FrontendImplementation, SkillType.Frontend)]
    [TestCase(TaskType.Database, SkillType.Database)]
    [TestCase(TaskType.Infrastructure, SkillType.Infrastructure)]
    [TestCase(TaskType.Testing, SkillType.Testing)]
    [TestCase(TaskType.Security, SkillType.Security)]
    [TestCase(TaskType.Architecture, SkillType.Architecture)]
    [TestCase(TaskType.Planning, SkillType.Planning)]
    [TestCase(TaskType.Management, SkillType.Management)]
    [TestCase(TaskType.CustomerSupport, SkillType.CustomerSupport)]
    public void Get_SingleSkillTask_UsesMatchingSkill(TaskType type, SkillType skill)
    {
        var weights = TaskSkillProfiles.Get(type).Weights;

        Assert.AreEqual(1, weights.Count);
        Assert.AreEqual(1m, weights[skill]);
    }

    [Test]
    public void Get_Backend_UsesPrimaryAndSupportingSkills()
    {
        var weights = TaskSkillProfiles.Get(TaskType.BackendImplementation).Weights;

        Assert.AreEqual(3, weights.Count);
        Assert.AreEqual(0.7m, weights[SkillType.Backend]);
        Assert.AreEqual(0.2m, weights[SkillType.Database]);
        Assert.AreEqual(0.1m, weights[SkillType.Testing]);
    }

    [TestCase(-1)]
    [TestCase(10)]
    [TestCase(999)]
    public void Get_UndefinedTaskType_Throws(int type)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TaskSkillProfiles.Get((TaskType)type));
    }
}
