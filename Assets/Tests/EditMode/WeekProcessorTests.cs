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
}
