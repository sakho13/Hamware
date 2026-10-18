using System;
using System.Collections.Generic;
using NUnit.Framework;

public class WeekProcessorTests
{
    [Test]
    public void Process_ProgressOnly_DoesNotUpdateFatigueOrResetConsumedHours()
    {
        var employee = new Employee("社員", 40, new[] { new Skill(SkillType.Testing, 2) }, 50m);
        var task = new DevelopmentTask("テスト作業", 1000, TaskType.Testing);
        Assert.IsTrue(employee.TryConsumeHours(20));
        var allocations = new Dictionary<DevelopmentTask, Dictionary<Employee, int>>
        {
            { task, new Dictionary<Employee, int> { { employee, 20 } } }
        };

        new WeekProcessor().Process(allocations, new ProductState(0m));

        Assert.AreEqual(15m, task.CompletedProgressHours);
        Assert.AreEqual(50m, employee.Fatigue);
        Assert.AreEqual(20, employee.AvailableHours);
        Assert.AreEqual(20, allocations[task][employee]);
    }

    [Test]
    public void Process_ClampsProgressWithoutRefundingHoursOrMutatingAllocations()
    {
        var employee = new Employee("社員", 40, new[]
        {
            new Skill(SkillType.Backend, 4),
            new Skill(SkillType.Database, 3),
            new Skill(SkillType.Testing, 2)
        });
        var task = new DevelopmentTask("Backend", 5, TaskType.BackendImplementation);
        Assert.IsTrue(employee.TryConsumeHours(10));
        var allocations = new Dictionary<DevelopmentTask, Dictionary<Employee, int>>
        {
            { task, new Dictionary<Employee, int> { { employee, 10 } } }
        };

        new WeekProcessor().Process(allocations, new ProductState(0m));

        Assert.AreEqual(5m, task.CompletedProgressHours);
        Assert.AreEqual(30, employee.AvailableHours);
        Assert.AreEqual(1, allocations.Count);
        Assert.AreEqual(10, allocations[task][employee]);
    }

    [Test]
    public void Process_LargeCombinedHours_SumsProgressWithoutIntegerOverflow()
    {
        var a = new Employee("A", int.MaxValue);
        var b = new Employee("B", int.MaxValue);
        var task = new DevelopmentTask("Backend", int.MaxValue, TaskType.BackendImplementation);
        var allocations = new Dictionary<DevelopmentTask, Dictionary<Employee, int>>
        {
            { task, new Dictionary<Employee, int> { { a, int.MaxValue }, { b, int.MaxValue } } }
        };

        new WeekProcessor().Process(allocations, new ProductState(0m));

        Assert.AreEqual((decimal)int.MaxValue, task.CompletedProgressHours);
        Assert.IsTrue(task.IsCompleted);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void Process_InvalidAllocation_PreservesAllStateAndRetryAppliesCompletionOnce(
        bool invalidFirst, bool nullEmployeeAllocations)
    {
        var skill = new Skill(SkillType.Frontend, 2);
        var employee = new Employee("社員", 40, new[] { skill });
        Assert.IsTrue(employee.TryConsumeHours(20));
        var first = new DevelopmentTask("完了する機能", 10, TaskType.FrontendImplementation);
        var second = new DevelopmentTask("不正割当を含む機能", 100, TaskType.BackendImplementation);
        first.ApplyProgress(3m);
        second.ApplyProgress(2m);
        var firstHours = new Dictionary<Employee, int> { { employee, 10 } };
        var invalidHours = nullEmployeeAllocations ? null : new Dictionary<Employee, int> { { employee, -1 } };
        var allocations = new Dictionary<DevelopmentTask, Dictionary<Employee, int>>();
        foreach (var task in invalidFirst ? new[] { second, first } : new[] { first, second })
        {
            allocations.Add(task, task == first ? firstHours : invalidHours);
        }
        var product = new ProductState();
        var processor = new WeekProcessor();

        if (nullEmployeeAllocations)
        {
            Assert.Throws<ArgumentException>(() => processor.Process(allocations, product));
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => processor.Process(allocations, product));
        }

        Assert.AreEqual(3m, first.CompletedProgressHours, "後続の失敗でも先行進捗を確定しない");
        Assert.IsFalse(first.IsCompleted);
        Assert.AreEqual(2m, second.CompletedProgressHours);
        Assert.AreEqual(10m, product.TechnicalDebt);
        Assert.AreEqual(70m, product.Quality);
        Assert.AreEqual(20, employee.AvailableHours);
        Assert.AreEqual(0m, employee.Fatigue);
        Assert.IsTrue(employee.TryGetSkill(SkillType.Frontend, out var actualSkill));
        Assert.AreSame(skill, actualSkill);
        Assert.AreEqual(2, allocations.Count);
        Assert.AreSame(firstHours, allocations[first]);
        Assert.AreSame(invalidHours, allocations[second]);
        Assert.AreEqual(10, firstHours[employee]);
        if (invalidHours != null)
        {
            Assert.AreEqual(-1, invalidHours[employee]);
        }

        allocations[second] = new Dictionary<Employee, int> { { employee, 10 } };
        processor.Process(allocations, product);

        Assert.IsTrue(first.IsCompleted);
        Assert.AreEqual(10m, first.CompletedProgressHours);
        Assert.AreEqual(6.75m, second.CompletedProgressHours);
        Assert.AreEqual(11m, product.TechnicalDebt);
        Assert.AreEqual(69.5m, product.Quality);
        Assert.AreEqual(20, employee.AvailableHours);
        Assert.AreEqual(0m, employee.Fatigue);

        processor.Process(allocations, product);
        Assert.AreEqual(11m, product.TechnicalDebt, "再処理でも完了効果は重複しない");
        Assert.AreEqual(69.5m, product.Quality);
    }
}
