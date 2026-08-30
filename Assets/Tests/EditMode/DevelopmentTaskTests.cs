using System;
using NUnit.Framework;

public class DevelopmentTaskTests
{
    [Test]
    public void ApplyProgress_AddsToCompletedHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30);

        task.ApplyProgress(20);

        Assert.AreEqual(20, task.CompletedHours);
    }

    [Test]
    public void ApplyProgress_ReachesRequiredHours_IsCompletedBecomesTrue()
    {
        var task = new DevelopmentTask("ログイン機能", 30);

        task.ApplyProgress(30);

        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_BelowRequiredHours_IsCompletedIsFalse()
    {
        var task = new DevelopmentTask("ログイン機能", 30);

        task.ApplyProgress(20);

        Assert.IsFalse(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ExceedsRequiredHours_ClampsToRequiredHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30);
        task.ApplyProgress(30); // まず完了させる

        task.ApplyProgress(50); // 完了済みタスクへ過剰な工数を適用

        Assert.AreEqual(30, task.CompletedHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void ApplyProgress_ZeroOrNegativeHours_DoesNotChangeCompletedHours()
    {
        var task = new DevelopmentTask("ログイン機能", 30);
        task.ApplyProgress(10);

        task.ApplyProgress(0);
        task.ApplyProgress(-5);

        Assert.AreEqual(10, task.CompletedHours);
    }

    [Test]
    public void Constructor_RequiredHoursIsZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentTask("task", 0));
    }

    [Test]
    public void Constructor_RequiredHoursIsNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentTask("task", -5));
    }
}
