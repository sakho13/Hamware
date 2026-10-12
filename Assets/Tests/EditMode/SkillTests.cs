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
    public void Constructor_DefinedType_Succeeds([Values] SkillType type)
    {
        var skill = new Skill(type, 3);

        Assert.AreEqual(type, skill.Type);
        Assert.AreEqual(3, skill.Level);
    }

    [TestCase(-1)]
    [TestCase(12)]
    [TestCase(999)]
    public void Constructor_UndefinedType_ThrowsArgumentOutOfRangeException(int type)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Skill((SkillType)type, 3));

        Assert.AreEqual("type", exception.ParamName);
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
