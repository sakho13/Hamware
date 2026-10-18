using System;
using NUnit.Framework;

public class TaskProductProfileTests
{
    [TestCase(TaskType.FrontendImplementation, 10, -5, true)]
    [TestCase(TaskType.BackendImplementation, 10, -5, true)]
    [TestCase(TaskType.Database, 0, 0, true)]
    [TestCase(TaskType.Infrastructure, 0, 0, true)]
    [TestCase(TaskType.Testing, 0, 20, false)]
    [TestCase(TaskType.Security, 0, 10, false)]
    [TestCase(TaskType.Architecture, -20, 5, false)]
    [TestCase(TaskType.Planning, 0, 0, false)]
    [TestCase(TaskType.Management, 0, 0, false)]
    [TestCase(TaskType.CustomerSupport, 0, 0, false)]
    public void Get_DefinedType_ReturnsExpectedProfile(TaskType type, int debtHundredths, int qualityHundredths,
        bool isDebtSensitive)
    {
        var profile = TaskProductProfiles.Get(type);
        Assert.AreEqual(debtHundredths / 100m, profile.TechnicalDebtChangePerProgressHour);
        Assert.AreEqual(qualityHundredths / 100m, profile.QualityChangePerProgressHour);
        Assert.AreEqual(isDebtSensitive, profile.IsDebtSensitive);
    }

    [Test]
    public void Get_EveryDefinedType_HasProfile()
    {
        foreach (TaskType type in Enum.GetValues(typeof(TaskType)))
        {
            Assert.IsNotNull(TaskProductProfiles.Get(type), type.ToString());
        }
    }

    [TestCase(-1)]
    [TestCase(999)]
    public void Get_UndefinedType_Throws(int type)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TaskProductProfiles.Get((TaskType)type));
    }

    [Test]
    public void Profile_Properties_HaveNoSetters()
    {
        foreach (var property in typeof(TaskProductProfile).GetProperties())
        {
            Assert.IsNull(property.GetSetMethod(true), property.Name);
        }
    }
}
