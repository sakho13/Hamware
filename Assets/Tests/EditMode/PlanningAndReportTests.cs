using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public class PlanningAndReportTests
{
    [Test]
    public void EditingAndCancelling_ReturnsOnlyCurrentPlannedHours()
    {
        var game = new GameSimulation();
        var employee = new Employee("ハム", 40, initialFatigue: 50m);
        var a = new DevelopmentTask("A", 100, TaskType.Testing);
        var b = new DevelopmentTask("B", 100, TaskType.Testing);
        game.AddEmployee(employee); game.AddTask(a); game.AddTask(b);
        game.TryAllocateHours(employee, a, 20); game.TryAllocateHours(employee, b, 10);
        var snapshot = game.GetAllocations();
        Assert.IsTrue(game.TrySetAllocatedHours(employee, a, 5));
        Assert.AreEqual(25, employee.AvailableHours);
        Assert.AreEqual(15, game.AllocatedHours);
        Assert.IsTrue(game.TrySetAllocatedHours(employee, b, 0));
        Assert.AreEqual(35, employee.AvailableHours);
        Assert.AreEqual(5, game.AllocatedHours);
        Assert.AreEqual(20, snapshot[0].Hours);
        Assert.AreEqual(50m, employee.Fatigue);
        Assert.AreEqual(0m, a.CompletedProgressHours);
        var result = game.AdvanceWeek();
        decimal fatigue = employee.Fatigue;
        decimal progress = a.CompletedProgressHours;
        game.TrySetAllocatedHours(employee, a, 0);
        Assert.AreEqual(40, employee.AvailableHours);
        Assert.AreEqual(fatigue, employee.Fatigue);
        Assert.AreEqual(progress, a.CompletedProgressHours);
        Assert.AreEqual(5, result.WorkedHours);
    }

    [TestCase(-1, AllocationFailure.InvalidHours)]
    [TestCase(41, AllocationFailure.InsufficientHours)]
    [TestCase(int.MaxValue, AllocationFailure.InsufficientHours)]
    public void InvalidReplacement_PreservesAllocationAndTime(int hours, AllocationFailure expected)
    {
        var session = new PrototypeSession(); var game = session.Simulation;
        var employee = game.Employees[0]; var task = game.Tasks[0];
        game.TryAllocateHours(employee, task, 20);
        Assert.IsFalse(game.TrySetAllocatedHours(employee, task, hours, out var failure));
        Assert.AreEqual(expected, failure);
        Assert.AreEqual(20, employee.AvailableHours);
        Assert.AreEqual(20, game.GetAllocatedHours(employee, task));
        Assert.IsFalse(game.TryAllocateHours(employee, task, int.MaxValue));
        Assert.AreEqual(20, employee.AvailableHours);
    }

    [Test]
    public void TargetValidation_ReportsReasonsWithoutMutatingState()
    {
        var game = new GameSimulation(); var employee = new Employee("ハム", 40);
        var task = new DevelopmentTask("作業", 1, TaskType.Testing);
        Assert.IsFalse(game.TrySetAllocatedHours(null, task, 1, out var reason));
        Assert.AreEqual(AllocationFailure.MissingEmployee, reason);
        Assert.IsFalse(game.TrySetAllocatedHours(employee, null, 1, out reason));
        Assert.AreEqual(AllocationFailure.MissingTask, reason);
        Assert.IsFalse(game.TrySetAllocatedHours(employee, task, 1, out reason));
        Assert.AreEqual(AllocationFailure.UnregisteredEmployee, reason);
        game.AddEmployee(employee);
        Assert.IsFalse(game.TrySetAllocatedHours(employee, task, 1, out reason));
        Assert.AreEqual(AllocationFailure.UnregisteredTask, reason);
        game.AddTask(task); game.TryAllocateHours(employee, task, 2); game.AdvanceWeek();
        Assert.IsFalse(game.TrySetAllocatedHours(employee, task, 1, out reason));
        Assert.AreEqual(AllocationFailure.CompletedTask, reason);
        Assert.AreEqual(40, employee.AvailableHours);
    }

    [Test]
    public void NamesAreNotIdentity_AndPublicCollectionsCannotBeMutated()
    {
        var game = new GameSimulation();
        var a = new Employee("同名", 40); var b = new Employee("同名", 40);
        var x = new DevelopmentTask("同名", 50, TaskType.Testing);
        var y = new DevelopmentTask("同名", 50, TaskType.Testing);
        game.AddEmployee(a); game.AddEmployee(b); game.AddTask(x); game.AddTask(y); game.AddTask(x);
        Assert.AreNotEqual(a.Id, b.Id); Assert.AreNotEqual(x.Id, y.Id);
        Assert.AreEqual(2, game.Tasks.Count);
        game.TrySetAllocatedHours(a, x, 10); game.TrySetAllocatedHours(b, y, 20);
        Assert.AreEqual(a.Id, game.GetAllocations()[0].EmployeeId);
        Assert.AreEqual(x.Id, game.GetAllocations()[0].TaskId);
        Assert.Throws<NotSupportedException>(() => ((IList<Employee>)game.Employees).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DevelopmentTask>)game.Tasks).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<AllocationSnapshot>)game.GetAllocations()).Clear());
    }

    [TestCase(100, 7.875, 0)]
    [TestCase(5, 5, 2.875)]
    public void Report_DistinguishesWorkCalculatedAppliedAndDiscardedProgress(int required, double applied, double discarded)
    {
        var game = new GameSimulation(50m);
        var employee = new Employee("ハム田", 40, new[] { new Skill(SkillType.Backend, 4),
            new Skill(SkillType.Database, 3), new Skill(SkillType.Testing, 2) }, 50m);
        var task = new DevelopmentTask("API", required, TaskType.BackendImplementation);
        game.AddEmployee(employee); game.AddTask(task); game.TryAllocateHours(employee, task, 10);
        var result = game.AdvanceWeek(); var detail = result.Tasks.Single();
        Assert.AreEqual(10, result.WorkedHours);
        Assert.AreEqual(7.875m, detail.CalculatedProgress);
        Assert.AreEqual((decimal)applied, detail.AppliedProgress);
        Assert.AreEqual((decimal)discarded, detail.DiscardedProgress);
        Assert.AreEqual(1.4m, detail.Contributions[0].SkillMultiplier);
        Assert.AreEqual(0.75m, detail.Contributions[0].FatigueMultiplier);
        Assert.AreEqual(0.75m, detail.Contributions[0].DebtMultiplier);
        Assert.AreEqual(30m, result.Employees[0].FatigueAfter);
        Assert.AreEqual(result.Employees[0].FatigueAfter, employee.Fatigue);
        Assert.AreEqual(result.Product.TechnicalDebtAfter, game.Product.TechnicalDebt);
    }

    [Test]
    public void Report_ClampedEffectsAndHistoryAreImmutableAndNotRepeated()
    {
        var game = new GameSimulation(0m, 99m);
        var employee = new Employee("テスト担当", 40, new[] { new Skill(SkillType.Testing, 2) });
        var task = new DevelopmentTask("テスト", 20, TaskType.Testing);
        game.AddEmployee(employee); game.AddTask(task); game.TryAllocateHours(employee, task, 20);
        var first = game.AdvanceWeek();
        Assert.AreEqual(4m, first.Product.RequestedQualityChange);
        Assert.AreEqual(1m, first.Product.AppliedQualityChange);
        Assert.IsTrue(first.Tasks[0].CompletedThisWeek);
        employee.AddSkill(new Skill(SkillType.Testing, 5));
        var second = game.AdvanceWeek();
        Assert.AreEqual(0, second.Tasks.Count);
        Assert.AreEqual(0m, second.Product.RequestedQualityChange);
        Assert.AreEqual(1, first.Week);
        Assert.AreEqual(1m, first.Tasks[0].Contributions[0].SkillMultiplier);
        Assert.AreEqual(99m, first.Product.QualityBefore);
        Assert.AreSame(first, game.History[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<TaskWeekResult>)first.Tasks).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<WorkContribution>)first.Tasks[0].Contributions).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<WeekResult>)game.History).Clear());
    }

    [Test]
    public void IdleWeek_ReportsRecoveryAndNoProductChanges()
    {
        var game = new GameSimulation(); var employee = new Employee("休養", 40, initialFatigue: 60m);
        game.AddEmployee(employee);
        var result = game.AdvanceWeek();
        Assert.AreEqual(0, result.WorkedHours);
        Assert.AreEqual(30m, result.Employees[0].FatigueAfter);
        Assert.AreEqual(0m, result.Product.AppliedQualityChange);
        Assert.AreEqual(0m, result.Product.AppliedTechnicalDebtChange);
    }

    [Test]
    public void InvalidWeek_PreservesAllStateAndPreviousReportThenRetryCompletesOnce()
    {
        var session = new PrototypeSession(); var game = session.Simulation;
        var previous = game.AdvanceWeek();
        var employee = game.Employees[0]; var task = new DevelopmentTask("短いテスト", 5, TaskType.Testing);
        game.AddTask(task); game.TryAllocateHours(employee, task, 10);
        var allocations = (Dictionary<DevelopmentTask, Dictionary<Employee, int>>)typeof(GameSimulation)
            .GetField("_allocations", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
        allocations.Add(game.Tasks[0], null);
        Assert.Throws<ArgumentException>(() => game.AdvanceWeek());
        Assert.AreSame(previous, game.LastWeekResult);
        Assert.AreEqual(1, game.History.Count);
        Assert.AreEqual(2, game.CurrentWeek);
        Assert.AreEqual(0m, task.CompletedProgressHours);
        Assert.AreEqual(30, employee.AvailableHours);
        Assert.AreEqual(0m, employee.Fatigue);
        Assert.AreEqual(70m, game.Product.Quality);
        allocations.Remove(game.Tasks[0]);
        var result = game.AdvanceWeek();
        Assert.AreEqual(1m, result.Product.RequestedQualityChange);
        Assert.AreEqual(71m, game.Product.Quality);
    }

    [Test]
    public void Prototype_TwelveWeeksSupportsEditingRecoveryReportsAndRestart()
    {
        var session = new PrototypeSession(); var game = session.Simulation;
        for (int week = 1; week <= 12; week++)
        {
            if (week % 4 != 0)
                foreach (var employee in game.Employees)
                {
                    var task = game.Tasks.First(t => !t.IsCompleted);
                    Assert.IsTrue(session.TrySetHours(employee, task, 30, out _));
                    Assert.IsTrue(session.TrySetHours(employee, task, 40, out _));
                }
            Assert.IsTrue(session.TryAdvanceWeek());
            Assert.IsFalse(session.TryAdvanceWeek(), "週報を閉じるまでは二重確定を拒否");
            Assert.IsFalse(session.TrySetHours(game.Employees[0], game.Tasks[0], 0, out var reason));
            Assert.AreEqual(AllocationFailure.PlanningLocked, reason);
            Assert.AreEqual(week + 1, game.CurrentWeek);
            Assert.AreEqual(80, game.AvailableHours);
            Assert.AreEqual(0, game.GetAllocations().Count);
            Assert.AreEqual(game.Product.Quality, game.LastWeekResult.Product.QualityAfter);
            session.ContinuePlanning();
        }
        Assert.AreEqual(12, game.History.Count);
        session.Restart();
        Assert.AreEqual(1, session.Simulation.CurrentWeek);
        Assert.IsNull(session.Simulation.LastWeekResult);
        Assert.AreNotSame(game.Employees[0], session.Simulation.Employees[0]);
    }
}
