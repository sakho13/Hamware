using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class PrototypeUITests
{
    private HamwareUI _ui;
    private GameSimulation Game => _ui.Session.Simulation;

    [UnitySetUp]
    public IEnumerator OpenPrototype()
    {
        yield return SceneManager.LoadSceneAsync("Hamware");
        yield return null;
        _ui = Object.FindAnyObjectByType<HamwareUI>();
        Assert.IsNotNull(_ui);
        Assert.IsNotNull(_ui.GetComponent<UIDocument>(), "UIDocumentをシーンに保持する");
    }

    [UnityTest]
    public IEnumerator Allocations_CanBeEditedRejectedCancelledAndReassigned()
    {
        var backend = Game.Tasks.First(t => t.Type == TaskType.BackendImplementation);
        var frontend = Game.Tasks.First(t => t.Type == TaskType.FrontendImplementation);
        var backendEmployee = Game.Employees.Single(e => e.TryGetSkill(SkillType.Backend, out var s) && s.Level == 4);
        var frontendEmployee = Game.Employees.Single(e => e.TryGetSkill(SkillType.Frontend, out _));

        Allocate(backendEmployee, backend, 20);
        Assert.AreEqual(20, Game.AllocatedHours);
        Allocate(backendEmployee, backend, 50);
        Assert.AreEqual(20, Game.AllocatedHours);
        StringAssert.Contains("残り時間", _ui.Root.Q<Label>("status").text);
        Click("cancel-" + backend.Id);
        Assert.AreEqual(80, Game.AvailableHours);
        Allocate(frontendEmployee, frontend, 30);
        Assert.AreEqual(30, Game.GetAllocatedHours(frontendEmployee, frontend));
        StringAssert.Contains(frontendEmployee.Name, _ui.Root.Q<Label>("allocation-title").text);
        yield return Capture("planning.png");
        yield return Capture("planning-1280x720.png", 1280, 720);
    }

    [UnityTest]
    public IEnumerator ConfirmationAndReport_BlockBackgroundControls()
    {
        Click("advance");
        Assert.AreEqual(1, Game.CurrentWeek);
        Assert.IsFalse(_ui.Root.Q<Button>("restart").enabledInHierarchy);
        Click("close-modal");
        Assert.AreEqual(1, Game.CurrentWeek);
        Assert.IsTrue(_ui.Root.Q<Button>("advance").enabledInHierarchy);

        Click("advance");
        Click("confirm-week");

        Assert.AreEqual(2, Game.CurrentWeek);
        Assert.AreEqual(1, Game.History.Count);
        Assert.IsNotNull(_ui.Root.Q("report-content"));
        Assert.IsFalse(_ui.Root.Q<Button>("advance").enabledInHierarchy);
        // 現在のパネル上の無効なボタンを操作する。削除済みの確認ボタンには送らない。
        Click("advance");
        Assert.AreEqual(2, Game.CurrentWeek);
        yield return Capture("week-report.png");
        Click("continue");
        Assert.IsTrue(_ui.Root.Q<Button>("advance").enabledInHierarchy);
        Assert.IsFalse(_ui.Session.IsShowingReport);
    }

    [UnityTest]
    public IEnumerator TwelveWeeks_KeepReportsConsistent_AndShowFatigueAndRecovery()
    {
        for (int week = 1; week <= 12; week++)
        {
            if (week != 7)
            {
                var task = week == 1 ? Game.Tasks.First(t => t.Type == TaskType.Testing) :
                    week == 2 ? Game.Tasks.First(t => t.Type == TaskType.Architecture) :
                    Game.Tasks.FirstOrDefault(t => !t.IsCompleted);
                if (task != null && !task.IsCompleted)
                    foreach (var employee in Game.Employees) Allocate(employee, task, 40);
            }
            AdvanceAndContinue();
            AssertReportMatchesGame();
            if (week == 6)
            {
                Assert.AreEqual(2, _ui.Root.Query(className: "sleepy").ToList().Count);
                yield return Capture("fatigue.png");
            }
            if (week == 7) Assert.IsTrue(Game.Employees.All(e => e.Fatigue == 30m));
        }
        Assert.AreEqual(13, Game.CurrentWeek);
        Assert.AreEqual(12, Game.History.Count);
        Assert.AreEqual(80, Game.AvailableHours);
    }

    [UnityTest]
    public IEnumerator CompletedBacklog_AllowsAnotherWeek_AndRestartRequiresConfirmation()
    {
        for (int week = 0; week < 60 && Game.Tasks.Any(t => !t.IsCompleted); week++)
        {
            var task = Game.Tasks.First(t => !t.IsCompleted);
            foreach (var employee in Game.Employees) Allocate(employee, task, 40);
            AdvanceAndContinue();
        }
        Assert.IsTrue(Game.Tasks.All(t => t.IsCompleted));
        Assert.IsTrue(_ui.Root.Query<Button>(className: "set-hours").ToList().All(b => !b.enabledInHierarchy));
        int previous = Game.CurrentWeek;
        AdvanceAndContinue();
        Assert.AreEqual(previous + 1, Game.CurrentWeek);
        Assert.AreEqual(0, Game.LastWeekResult.WorkedHours);
        Click("restart");
        Click("close-modal");
        Assert.AreEqual(previous + 1, Game.CurrentWeek);
        Click("restart");
        Click("confirm-restart");
        Assert.AreEqual(1, Game.CurrentWeek);
        Assert.IsEmpty(Game.History);
        yield return null;
    }

    private void AssertReportMatchesGame()
    {
        Assert.AreEqual(Game.CurrentWeek - 1, Game.LastWeekResult.Week);
        Assert.AreEqual(Game.Product.Quality, Game.LastWeekResult.Product.QualityAfter);
        Assert.AreEqual(Game.Product.TechnicalDebt, Game.LastWeekResult.Product.TechnicalDebtAfter);
        foreach (var employee in Game.Employees)
            Assert.AreEqual(employee.Fatigue, Game.LastWeekResult.Employees.Single(e => e.EmployeeId == employee.Id).FatigueAfter);
    }

    private void Allocate(Employee employee, DevelopmentTask task, int hours)
    {
        Click("employee-" + employee.Id);
        _ui.Root.Q<IntegerField>("hours-" + task.Id).value = hours;
        Click("apply-" + task.Id);
    }

    private void AdvanceAndContinue()
    {
        Click("advance");
        Click("confirm-week");
        Click("continue");
    }

    private void Click(string name)
    {
        var button = _ui.Root.Q<Button>(name);
        Assert.IsNotNull(button, name);
        Assert.IsNotNull(button.panel, "現在表示中のボタンだけにイベントを送る");
        using (var click = ClickEvent.GetPooled())
        {
            click.target = button;
            button.SendEvent(click);
        }
    }

    private IEnumerator Capture(string name, int width = 1600, int height = 1000)
    {
        var directory = System.Environment.GetEnvironmentVariable("HAMWARE_CAPTURE_DIR");
        if (string.IsNullOrEmpty(directory)) yield break;
        Directory.CreateDirectory(directory);
        var panel = _ui.GetComponent<UIDocument>().panelSettings;
        var target = new RenderTexture(width, height, 24);
        target.Create();
        var previousTarget = panel.targetTexture;
        var previous = RenderTexture.active;
        Texture2D texture = null;
        panel.targetTexture = target;
        try
        {
            for (int i = 0; i < 10; i++) yield return null;
            foreach (var metric in _ui.Root.Query(className: "metric").ToList())
                Assert.LessOrEqual(metric.Q(className: "small").worldBound.yMax, metric.worldBound.yMax);
            Assert.LessOrEqual(_ui.Root.Q(className: "footer").worldBound.yMax, _ui.Root.worldBound.yMax);
            RenderTexture.active = target;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(directory, name), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            panel.targetTexture = previousTarget;
            if (texture != null) Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
        }
        yield return null;
    }
}
