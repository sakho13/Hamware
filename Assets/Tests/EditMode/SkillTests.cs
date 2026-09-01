using System;
using NUnit.Framework;

public class SkillTests
{
    [Test]
    public void Constructor_SetsTypeAndLevel()
    {
        var skill = new Skill(SkillType.Backend, 3);

        Assert.AreEqual(SkillType.Backend, skill.Type);
        Assert.AreEqual(3, skill.Level);
    }

    [Test]
    public void Constructor_LevelBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Skill(SkillType.Backend, 0));
    }

    [Test]
    public void Constructor_LevelAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Skill(SkillType.Backend, 6));
    }

    [Test]
    public void Constructor_NegativeLevel_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Skill(SkillType.Backend, -1));
    }

    [Test]
    public void Constructor_LevelAtMinimum_Succeeds()
    {
        var skill = new Skill(SkillType.Backend, Skill.MinLevel);

        Assert.AreEqual(Skill.MinLevel, skill.Level);
    }

    [Test]
    public void Constructor_LevelAtMaximum_Succeeds()
    {
        var skill = new Skill(SkillType.Backend, Skill.MaxLevel);

        Assert.AreEqual(Skill.MaxLevel, skill.Level);
    }
}
