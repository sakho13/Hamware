using System;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

public class PrototypeSessionTests
{
    [Test]
    public void UIAssembly_CannotAccessDomainInternals()
    {
        var game = typeof(GameSimulation).Assembly;
        var ui = typeof(HamwareUI).Assembly;
        Assert.AreEqual("Hamware.UI", ui.GetName().Name);
        Assert.AreNotSame(game, ui);
        Assert.AreSame(ui, typeof(PrototypeSession).Assembly);
        Assert.IsFalse(game.GetReferencedAssemblies().Any(a => a.Name == ui.GetName().Name));
        Assert.IsFalse(game.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
            .Cast<InternalsVisibleToAttribute>().Any(a => a.AssemblyName == ui.GetName().Name));
    }

    [Test]
    public void ReportOpen_RejectsRepeatedConfirmationAndEditing_UntilContinue()
    {
        var game = new GameSimulation();
        var employee = new Employee("社員", 40);
        var task = new DevelopmentTask("テスト", 100, TaskType.Testing);
        game.AddEmployee(employee);
        game.AddTask(task);
        var session = new PrototypeSession(game);
        Assert.IsTrue(session.TrySetHours(employee, task, 10, out _));

        Assert.IsTrue(session.TryAdvanceWeek());
        for (int i = 0; i < 3; i++) Assert.IsFalse(session.TryAdvanceWeek());
        Assert.IsFalse(session.TrySetHours(employee, task, 5, out var failure));

        Assert.IsTrue(failure.IsReportOpen);
        Assert.AreEqual(AllocationFailure.None, failure.AllocationReason);
        Assert.AreEqual(2, game.CurrentWeek);
        Assert.AreEqual(1, game.History.Count);
        Assert.AreEqual(0, game.AllocatedHours);
        session.ContinuePlanning();
        Assert.IsTrue(session.TrySetHours(employee, task, 5, out _));
        Assert.IsTrue(session.TryAdvanceWeek());
        Assert.AreEqual(3, game.CurrentWeek);
    }

    [Test]
    public void InvalidHours_KeepTheDomainReason()
    {
        var game = new GameSimulation();
        var employee = new Employee("社員", 40);
        var task = new DevelopmentTask("テスト", 100, TaskType.Testing);
        game.AddEmployee(employee);
        game.AddTask(task);
        var session = new PrototypeSession(game);

        Assert.IsFalse(session.TrySetHours(employee, task, -1, out var failure));

        Assert.IsFalse(failure.IsReportOpen);
        Assert.AreEqual(AllocationFailure.InvalidHours, failure.AllocationReason);
        Assert.AreEqual(40, employee.AvailableHours);
    }

    [Test]
    public void Restart_CreatesAFreshScenarioAndClearsTheReport()
    {
        var session = new PrototypeSession();
        var original = session.Simulation;
        Assert.IsTrue(session.TryAdvanceWeek());

        session.Restart();

        Assert.AreNotSame(original, session.Simulation);
        Assert.AreEqual(1, session.Simulation.CurrentWeek);
        Assert.IsEmpty(session.Simulation.History);
        Assert.IsFalse(session.IsShowingReport);
    }

    [TestCase(40)]
    [TestCase(32)]
    public void RestHint_UsesTheDomainUtilization(int weeklyHours)
    {
        string expected = (weeklyHours * FatigueCalculator.NeutralUtilization).ToString("0.###");
        StringAssert.Contains("うち" + expected + "h", PrototypePresentation.RestHint(weeklyHours));
    }

    [Test]
    public void EveryTaskEffect_DescribesTheProfileSigns()
    {
        foreach (TaskType type in Enum.GetValues(typeof(TaskType)))
        {
            var profile = TaskProductProfiles.Get(type);
            string text = PrototypePresentation.Effect(type);
            Assert.AreEqual(profile.TechnicalDebtChangePerProgressHour < 0m, text.Contains("負債が減"));
            Assert.AreEqual(profile.TechnicalDebtChangePerProgressHour > 0m, text.Contains("負債が増"));
            Assert.AreEqual(profile.QualityChangePerProgressHour < 0m, text.Contains("品質が下"));
            Assert.AreEqual(profile.QualityChangePerProgressHour > 0m, text.Contains("品質が上"));
        }
        Assert.AreEqual("品質改善", PrototypePresentation.Kind(TaskType.Security));
        Assert.AreEqual("開発・運用", PrototypePresentation.Kind(TaskType.CustomerSupport));
    }
}
