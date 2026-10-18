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
    [UnityTest]
    public IEnumerator Screen_EditingConfirmationReportAndRestartUseRealControls()
    {
        yield return SceneManager.LoadSceneAsync("Hamware");
        yield return null;
        var ui = Object.FindAnyObjectByType<HamwareUI>();
        Assert.IsNotNull(ui);
        var game = ui.Session.Simulation;
        ui.Root.Q<IntegerField>("hours-0").value = 20;
        Click(ui, "apply-0");
        Assert.AreEqual(20, game.AllocatedHours);
        ui.Root.Q<IntegerField>("hours-0").value = 50;
        Click(ui, "apply-0");
        Assert.AreEqual(20, game.AllocatedHours);
        StringAssert.Contains("残り時間", ui.Root.Q<Label>("status").text);
        Click(ui, "cancel-0");
        Assert.AreEqual(80, game.AvailableHours);
        Click(ui, "employee-1");
        StringAssert.Contains("ハム川", ui.Root.Q<Label>("allocation-title").text);
        ui.Root.Q<IntegerField>("hours-1").value = 30;
        Click(ui, "apply-1");
        yield return Capture(ui, "planning.png");
        yield return Capture(ui, "planning-1280x720.png", 1280, 720);
        Click(ui, "advance");
        Assert.AreEqual(1, game.CurrentWeek, "確定する前は週を進めない");
        Assert.IsFalse(ui.Root.Q<Button>("apply-0").enabledInHierarchy, "確認中は背面の計画を編集させない");
        var confirm = ui.Root.Q<Button>("confirm-week");
        Click(confirm); Click(confirm);
        Assert.AreEqual(2, game.CurrentWeek);
        Assert.AreEqual(1, game.History.Count);
        Assert.IsNotNull(ui.Root.Q("report-content"));
        yield return Capture(ui, "week-report.png");
        Click(ui, "continue");
        Assert.IsFalse(ui.Session.IsShowingReport);
        for (int i = 0; i < 11; i++)
        {
            // 品質・負債の改善、連続稼働、休養を画面の操作で通す。
            if (i != 6)
                for (int employeeIndex = 0; employeeIndex < game.Employees.Count; employeeIndex++)
                {
                    int taskIndex = i == 0 ? 4 : i == 1 ? 5 :
                        game.Tasks.ToList().FindIndex(t => !t.IsCompleted);
                    if (taskIndex >= 0 && !game.Tasks[taskIndex].IsCompleted)
                    {
                        Click(ui, "employee-" + employeeIndex);
                        ui.Root.Q<IntegerField>("hours-" + taskIndex).value = 40;
                        Click(ui, "apply-" + taskIndex);
                    }
                }
            Click(ui, "advance"); Click(ui, "confirm-week"); Click(ui, "continue");
            Assert.AreEqual(game.CurrentWeek - 1, game.LastWeekResult.Week);
            Assert.AreEqual(game.Product.Quality, game.LastWeekResult.Product.QualityAfter);
            Assert.AreEqual(game.Product.TechnicalDebt, game.LastWeekResult.Product.TechnicalDebtAfter);
            foreach (var employee in game.Employees)
                Assert.AreEqual(employee.Fatigue, game.LastWeekResult.Employees.Single(e => e.EmployeeId == employee.Id).FatigueAfter);
            if (i == 5)
            {
                Assert.AreEqual(2, ui.Root.Query(className: "sleepy").ToList().Count);
                yield return Capture(ui, "fatigue.png");
            }
            if (i == 6) Assert.IsTrue(game.Employees.All(e => e.Fatigue == 30m), "休養で60から30へ回復");
        }
        Assert.AreEqual(13, game.CurrentWeek);
        Assert.AreEqual(12, game.History.Count);
        Assert.AreEqual(80, game.AvailableHours);
        // 固定タスクがなくなった後も、画面から次の週へ進める。
        for (int i = 0; i < 40 && game.Tasks.Any(t => !t.IsCompleted); i++)
        {
            int taskIndex = game.Tasks.ToList().FindIndex(t => !t.IsCompleted);
            for (int employeeIndex = 0; employeeIndex < game.Employees.Count; employeeIndex++)
            {
                Click(ui, "employee-" + employeeIndex);
                ui.Root.Q<IntegerField>("hours-" + taskIndex).value = 40;
                Click(ui, "apply-" + taskIndex);
            }
            Click(ui, "advance"); Click(ui, "confirm-week"); Click(ui, "continue");
        }
        Assert.IsTrue(game.Tasks.All(t => t.IsCompleted));
        Assert.IsFalse(ui.Root.Q<Button>("apply-0").enabledInHierarchy);
        int finalWeek = game.CurrentWeek;
        Click(ui, "advance"); Click(ui, "confirm-week"); Click(ui, "continue");
        Assert.AreEqual(finalWeek + 1, game.CurrentWeek);
        Assert.AreEqual(0, game.LastWeekResult.WorkedHours);
        Click(ui, "restart"); Click(ui, "close-modal");
        Assert.AreEqual(finalWeek + 1, game.CurrentWeek);
        Click(ui, "restart"); Click(ui, "confirm-restart");
        Assert.AreEqual(1, ui.Session.Simulation.CurrentWeek);
        Assert.AreEqual(0, ui.Session.Simulation.History.Count);
    }

    private static void Click(HamwareUI ui, string name) => Click(ui.Root.Q<Button>(name));
    private static void Click(Button button)
    {
        Assert.IsNotNull(button);
        using (var click = ClickEvent.GetPooled())
        {
            click.target = button;
            button.SendEvent(click);
        }
    }

    private static IEnumerator Capture(HamwareUI ui, string name, int width = 1600, int height = 1000)
    {
        var directory = System.Environment.GetEnvironmentVariable("HAMWARE_CAPTURE_DIR");
        if (string.IsNullOrEmpty(directory)) yield break;
        Directory.CreateDirectory(directory);
        var panel = ui.GetComponent<UIDocument>().panelSettings;
        var target = new RenderTexture(width, height, 24);
        target.Create();
        var previousTarget = panel.targetTexture;
        var previous = RenderTexture.active;
        Texture2D texture = null;
        panel.targetTexture = target;
        try
        {
            for (int i = 0; i < 10; i++) yield return null;
            foreach (var metric in ui.Root.Query(className: "metric").ToList())
                Assert.LessOrEqual(metric.Q(className: "small").worldBound.yMax, metric.worldBound.yMax,
                    "指標の説明文をカード内に収める");
            Assert.LessOrEqual(ui.Root.Q(className: "footer").worldBound.yMax, ui.Root.worldBound.yMax,
                "週送りの操作を画面内に収める");
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
