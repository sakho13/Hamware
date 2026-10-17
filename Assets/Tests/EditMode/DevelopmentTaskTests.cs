using System;
using System.Linq;
using NUnit.Framework;

public class DevelopmentTaskTests
{
    private static int[] UndefinedTaskTypes => new[]
    {
        -1, Enum.GetValues(typeof(TaskType)).Cast<int>().Max() + 1, int.MaxValue
    };

    [Test]
    public void Constructor_DefinedType_SetsType([Values] TaskType type)
    {
        var task = new DevelopmentTask("task", 30, type);

        Assert.AreEqual(type, task.Type);
        Assert.AreEqual(0m, task.CompletedProgressHours);
        Assert.IsFalse(task.IsCompleted);
    }

    [TestCaseSource(nameof(UndefinedTaskTypes))]
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
        Assert.AreEqual(0.825m, task.CompletedProgressHours);
        Assert.IsFalse(task.IsCompleted);
        task.ApplyProgress(0.175m);

        Assert.AreEqual(1m, task.CompletedProgressHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ExceedsRemainingHours_ClampsWithoutOverflow()
    {
        var task = new DevelopmentTask("task", 30, TaskType.Testing);
        task.ApplyProgress(0.5m);

        task.ApplyProgress(decimal.MaxValue);

        Assert.AreEqual(30m, task.CompletedProgressHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_AddsToCompletedProgressHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);

        task.ApplyProgress(20);

        Assert.AreEqual(20, task.CompletedProgressHours);
    }

    [Test]
    public void ApplyProgress_ReachesRequiredProgressHours_IsCompletedBecomesTrue()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);

        task.ApplyProgress(30);

        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_BelowRequiredProgressHours_IsCompletedIsFalse()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);

        task.ApplyProgress(20);

        Assert.IsFalse(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ExceedsRequiredProgressHours_ClampsToRequiredProgressHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);
        task.ApplyProgress(30); // まず完了させる

        task.ApplyProgress(50); // 完了済みタスクへ過剰な工数を適用

        Assert.AreEqual(30, task.CompletedProgressHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ZeroOrNegativeHours_DoesNotChangeCompletedProgressHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30, TaskType.BackendImplementation);
        task.ApplyProgress(10);

        task.ApplyProgress(0);
        task.ApplyProgress(-5);

        Assert.AreEqual(10, task.CompletedProgressHours);
    }

    [Test]
    public void Constructor_RequiredProgressHoursIsZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentTask("task", 0, TaskType.BackendImplementation));
    }

    [Test]
    public void Constructor_RequiredProgressHoursIsNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentTask("task", -5, TaskType.BackendImplementation));
    }
}
