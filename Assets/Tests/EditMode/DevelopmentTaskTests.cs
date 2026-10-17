using System;
using NUnit.Framework;

public class DevelopmentTaskTests
{
    [Test]
    public void Constructor_DefinedType_SetsType([Values] TaskType type)
    {
        var task = new DevelopmentTask("task", 30, type);

        Assert.AreEqual(type, task.Type);
        Assert.AreEqual(0m, task.CompletedHours);
        Assert.IsFalse(task.IsCompleted);
    }

    [TestCase(-1)]
    [TestCase(10)]
    [TestCase(999)]
    public void Constructor_UndefinedType_Throws(int type)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DevelopmentTask("task", 30, (TaskType)type));

        Assert.AreEqual("type", exception.ParamName);
    }

    [Test]
    public void ApplyProgress_FractionalHours_AccumulatesAndCompletesExactly()
    {
        var task = new DevelopmentTask("task", 1, TaskType.Testing);

        task.ApplyProgress(0.825m);
        Assert.AreEqual(0.825m, task.CompletedHours);
        Assert.IsFalse(task.IsCompleted);
        task.ApplyProgress(0.175m);

        Assert.AreEqual(1m, task.CompletedHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ExceedsRemainingHours_ClampsWithoutOverflow()
    {
        var task = new DevelopmentTask("task", 30, TaskType.Testing);
        task.ApplyProgress(0.5m);

        task.ApplyProgress(decimal.MaxValue);

        Assert.AreEqual(30m, task.CompletedHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_AddsToCompletedHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);

        task.ApplyProgress(20);

        Assert.AreEqual(20, task.CompletedHours);
    }

    [Test]
    public void ApplyProgress_ReachesRequiredHours_IsCompletedBecomesTrue()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);

        task.ApplyProgress(30);

        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_BelowRequiredHours_IsCompletedIsFalse()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);

        task.ApplyProgress(20);

        Assert.IsFalse(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ExceedsRequiredHours_ClampsToRequiredHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);
        task.ApplyProgress(30); // まず完了させる

        task.ApplyProgress(50); // 完了済みタスクへ過剰な工数を適用

        Assert.AreEqual(30, task.CompletedHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ZeroOrNegativeHours_DoesNotChangeCompletedHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);
        task.ApplyProgress(10);

        task.ApplyProgress(0);
        task.ApplyProgress(-5);

        Assert.AreEqual(10, task.CompletedHours);
    }

    [Test]
    public void Constructor_RequiredHoursIsZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentTask("task", 0, TaskType.BackendImplementation));
    }

    [Test]
    public void Constructor_RequiredHoursIsNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentTask("task", -5, TaskType.BackendImplementation));
    }
}
