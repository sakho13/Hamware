using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>UI Toolkitによる試作画面。計算はSimulation、画面の進行制御はPrototypeSessionが担当する。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class HamwareUI : MonoBehaviour
{
    public PrototypeSession Session { get; private set; }
    public VisualElement Root { get; private set; }
    private PanelSettings _panel;
    private Font _font;
    private GameObject _camera;
    private int _selected;
    private string _message = "社員を選び、タスクへ時間を割り当てましょう。残した時間は休養になります。";
    private Vector2 _scroll;
    private GameSimulation Game => Session.Simulation;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        Session = new PrototypeSession();
        _panel = ScriptableObject.CreateInstance<PanelSettings>();
        _panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("HamwareTheme");
        _panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        _panel.referenceResolution = new Vector2Int(1600, 1000);
        _panel.match = 0.5f;
        var document = GetComponent<UIDocument>();
        document.panelSettings = _panel;
        Root = document.rootVisualElement;
        Root.styleSheets.Add(Resources.Load<StyleSheet>("Hamware"));
        // OSにインストールされた日本語フォントを使用。フォントファイルは再配布しない。
        _font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Hiragino Sans" }, 18);
        Root.style.unityFontDefinition = FontDefinition.FromFont(_font);
        if (Camera.main == null)
        {
            _camera = new GameObject("Hamware Camera");
            var camera = _camera.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.96f, 0.95f, 0.91f);
            camera.cullingMask = 0;
        }
        Render();
    }

    private void OnDisable()
    {
        Root?.Clear();
        if (_panel != null) Destroy(_panel);
        if (_font != null) Destroy(_font);
        if (_camera != null) Destroy(_camera);
    }

    private static Label Text(string value, string css = null)
    {
        var label = new Label(value);
        if (css != null) label.AddToClassList(css);
        return label;
    }

    private static VisualElement Box(string css)
    {
        var element = new VisualElement();
        element.AddToClassList(css);
        return element;
    }

    private static Button ActionButton(string value, string name, Action action, string css = null)
    {
        var button = new Button { text = value, name = name };
        if (css != null) button.AddToClassList(css);
        button.RegisterCallback<ClickEvent>(evt =>
        {
            if (button.enabledInHierarchy) action();
            evt.StopPropagation();
        });
        button.RegisterCallback<NavigationSubmitEvent>(evt =>
        {
            if (button.enabledInHierarchy) action();
            evt.StopPropagation();
        });
        return button;
    }

    private void Render()
    {
        Root.Clear();
        var page = Box("page");
        Root.Add(page);
        var header = Box("header");
        var brand = Box("brand");
        brand.Add(Text("HAMWARE", "logo"));
        brand.Add(Text("小さなチームの、今週のものづくり。", "muted"));
        header.Add(brand);
        header.Add(Text($"WEEK {Game.CurrentWeek:00}", "week"));
        header.Add(ActionButton("最初から", "restart", ConfirmRestart, "quiet"));
        page.Add(header);

        var metrics = Box("metrics");
        Metric(metrics, "今週の割当", $"{Game.AllocatedHours} / {Game.TotalWeeklyHours} h", "実際に働く時間");
        Metric(metrics, "残りの時間", $"{Game.AvailableHours} h", "未割当の時間で疲労を回復");
        Metric(metrics, "技術的負債", N(Game.Product.TechnicalDebt), "低いほど、機能開発が進む");
        Metric(metrics, "品質", N(Game.Product.Quality), "テスト・改善で育てる");
        page.Add(metrics);

        var workspace = Box("workspace");
        var team = new ScrollView { name = "team" };
        team.AddToClassList("team");
        team.Add(Text("今週のチーム", "section-title"));
        team.Add(Text("担当者を選択", "muted"));
        for (int i = 0; i < Game.Employees.Count; i++) AddEmployee(team, i);
        team.Add(Text(PrototypePresentation.RestHint(Game.Employees[_selected].WeeklyAvailableHours), "tip"));
        workspace.Add(team);
        var backlog = Box("backlog");
        var title = Text($"{Game.Employees[_selected].Name}の工数を配分", "section-title");
        title.name = "allocation-title";
        backlog.Add(title);
        backlog.Add(Text("入力値はこのタスクへ割り当てる合計時間です。0hで取消。", "muted"));
        var scroll = new ScrollView { name = "tasks" };
        scroll.AddToClassList("tasks");
        for (int i = 0; i < Game.Tasks.Count; i++) AddTask(scroll, i);
        backlog.Add(scroll);
        scroll.schedule.Execute(() => scroll.scrollOffset = _scroll);
        workspace.Add(backlog);
        page.Add(workspace);
        var footer = Box("footer");
        var status = Text(_message, "status");
        status.name = "status";
        footer.Add(status);
        var history = ActionButton("前週の結果", "history", () => ShowReport(false), "quiet");
        history.SetEnabled(Game.LastWeekResult != null);
        footer.Add(history);
        footer.Add(ActionButton("1週間進める →", "advance", ConfirmWeek, "primary"));
        page.Add(footer);
        if (Session.IsShowingReport) ShowReport(true);
    }

    private static void Metric(VisualElement parent, string title, string value, string hint)
    {
        var metric = Box("metric");
        metric.Add(Text(title, "muted"));
        metric.Add(Text(value, "metric-value"));
        metric.Add(Text(hint, "small"));
        parent.Add(metric);
    }

    private void AddEmployee(VisualElement team, int index)
    {
        var employee = Game.Employees[index];
        var card = ActionButton("", "employee-" + employee.Id, () => { _selected = index; Render(); }, "employee-card");
        if (index == _selected) card.AddToClassList("selected");
        var top = Box("employee-top");
        top.Add(Hamster(employee.Fatigue, index));
        var info = Box("employee-info");
        info.Add(Text(employee.Name, "employee-name"));
        info.Add(Text($"残り {employee.AvailableHours} / {employee.WeeklyAvailableHours} h", "employee-hours"));
        info.Add(Text(PrototypePresentation.FatigueMessage(employee.Fatigue), "muted"));
        top.Add(info);
        card.Add(top);
        card.Add(Text($"疲労  {N(employee.Fatigue)} / 100", "small"));
        card.Add(new ProgressBar { value = (float)employee.Fatigue, title = "" });
        card.Add(Text(string.Join("   ", employee.Skills.Select(s => $"{s.Type} {s.Level}")), "skills"));
        team.Add(card);
    }

    private static VisualElement Hamster(decimal fatigue, int variant)
    {
        var hamster = Box("hamster");
        if (variant == 1) hamster.AddToClassList("gray");
        if (fatigue >= PrototypePresentation.SleepyFaceThreshold) hamster.AddToClassList("sleepy");
        foreach (string css in new[] { "ear-left", "ear-right", "face", "cheek-left", "cheek-right", "eye-left", "eye-right" })
            hamster.Add(Box(css));
        hamster.Add(Text("ω", "mouth"));
        hamster.pickingMode = PickingMode.Ignore;
        return hamster;
    }

    private void AddTask(ScrollView parent, int index)
    {
        var task = Game.Tasks[index];
        var employee = Game.Employees[_selected];
        var row = Box("task-card");
        if (task.IsCompleted) row.AddToClassList("completed");
        var heading = Box("task-heading");
        heading.Add(Text(task.Name, "task-title"));
        heading.Add(Text(task.IsCompleted ? "完了" : PrototypePresentation.Kind(task.Type), "badge"));
        row.Add(heading);
        row.Add(new ProgressBar
        {
            value = (float)(task.CompletedProgressHours / task.RequiredProgressHours * 100m),
            title = $"進捗 {N(task.CompletedProgressHours)} / {task.RequiredProgressHours} h相当"
        });
        var edit = Box("task-edit");
        edit.Add(Text(string.Join("  /  ", Game.Employees.Select(e => $"{e.Name} {Game.GetAllocatedHours(e, task)}h")), "assignments"));
        var input = new IntegerField { name = "hours-" + task.Id, value = Game.GetAllocatedHours(employee, task), isDelayed = false };
        input.AddToClassList("hours-input");
        input.tooltip = "選択した社員から割り当てる合計時間（0で取消）";
        edit.Add(input);
        edit.Add(Text("h", "unit"));
        edit.Add(ActionButton("設定", "apply-" + task.Id, () => SetHours(task, input.value), "set-hours"));
        edit.Add(ActionButton("取消", "cancel-" + task.Id, () => SetHours(task, 0), "quiet"));
        edit.SetEnabled(!task.IsCompleted);
        row.Add(edit);
        row.Add(Text(task.IsCompleted ? "完了済みのため割り当てできません。" : PrototypePresentation.Effect(task.Type), "small"));
        parent.Add(row);
    }

    private void SetHours(DevelopmentTask task, int hours)
    {
        _scroll = Root.Q<ScrollView>("tasks").scrollOffset;
        if (Session.TrySetHours(Game.Employees[_selected], task, hours, out var failure))
            _message = hours == 0 ? $"「{task.Name}」の割当を取り消しました。" : $"「{task.Name}」へ {hours}h を設定しました。週送り前は変更できます。";
        else _message = FailureText(failure);
        Render();
    }

    private VisualElement Modal(string title)
    {
        Root.Q("overlay")?.RemoveFromHierarchy();
        Root.Q(className: "page").SetEnabled(false);
        var overlay = Box("overlay");
        overlay.name = "overlay";
        var card = Box("modal");
        card.Add(Text(title, "modal-title"));
        overlay.Add(card);
        Root.Add(overlay);
        overlay.schedule.Execute(() => overlay.Q<Button>()?.Focus());
        return card;
    }

    private void CloseModal()
    {
        Root.Q("overlay")?.RemoveFromHierarchy();
        Root.Q(className: "page").SetEnabled(true);
        Root.Q<Button>("advance")?.Focus();
    }

    private void ConfirmWeek()
    {
        if (Session.IsShowingReport) return;
        var card = Modal($"Week {Game.CurrentWeek} を確定しますか？");
        card.Add(Text($"割当 {Game.AllocatedHours}h ／ 未割当 {Game.AvailableHours}h\n未割当の時間は休養になります。確定後、この週の割当は変更できません。", "modal-copy"));
        card.Add(ActionButton("この計画で1週間進める", "confirm-week", () =>
        {
            if (Session.TryAdvanceWeek())
            {
                _message = "新しい週です。前週の結果を参考に、今週の工数を配分しましょう。";
                Render();
            }
        }, "primary"));
        card.Add(ActionButton("計画に戻る", "close-modal", CloseModal, "quiet"));
    }

    private void ConfirmRestart()
    {
        var card = Modal("最初の週からやり直しますか？");
        card.Add(Text("社員・タスク・週次結果を初期状態に戻します。現在のプレイは保存されません。", "modal-copy"));
        card.Add(ActionButton("最初から始める", "confirm-restart", () =>
        {
            Session.Restart(); _selected = 0; _scroll = Vector2.zero;
            _message = "新しいチームでスタート。まずは社員を選びましょう。";
            Render();
        }, "primary"));
        card.Add(ActionButton("続ける", "close-modal", () =>
        {
            if (Session.IsShowingReport) ShowReport(true);
            else CloseModal();
        }, "quiet"));
    }

    private void ShowReport(bool continueRequired)
    {
        var report = Game.LastWeekResult;
        if (report == null) return;
        var card = Modal($"Week {report.Week} のふりかえり");
        card.AddToClassList("report");
        var scroll = new ScrollView { name = "report-content" };
        scroll.Add(Text($"実勤務 {report.WorkedHours}h    →    反映した進捗 {N(report.AppliedProgress)}h相当", "report-summary"));
        var p = report.Product;
        scroll.Add(Text($"技術的負債  {N(p.TechnicalDebtBefore)} → {N(p.TechnicalDebtAfter)}    ／    品質  {N(p.QualityBefore)} → {N(p.QualityAfter)}", "report-metrics"));
        scroll.Add(Text($"完了効果の合計：負債 {Signed(p.RequestedTechnicalDebtChange)}、品質 {Signed(p.RequestedQualityChange)}\n上下限適用後の変化：負債 {Signed(p.AppliedTechnicalDebtChange)}、品質 {Signed(p.AppliedQualityChange)}", "small"));
        scroll.Add(Text("チームの様子", "section-title"));
        foreach (var employee in report.Employees)
            scroll.Add(Text($"{employee.Name}  ·  実勤務 {employee.WorkedHours}h  ·  疲労 {N(employee.FatigueBefore)} → {N(employee.FatigueAfter)}", "report-line"));
        scroll.Add(Text("進んだ仕事", "section-title"));
        if (report.Tasks.Count == 0) scroll.Add(Text("今週は休養。仕事の進捗・品質・負債は変わりません。", "report-line"));
        foreach (var task in report.Tasks)
        {
            scroll.Add(Text($"{(task.CompletedThisWeek ? "[完了] " : "")}{task.Name}  ·  {N(task.ProgressBefore)} → {N(task.ProgressAfter)} / {task.RequiredProgress} h相当", "report-line"));
            foreach (var c in task.Contributions)
                scroll.Add(Text($"  {c.EmployeeName}：{c.WorkedHours}h × スキル{N(c.SkillMultiplier)} × 疲労{N(c.FatigueMultiplier)} × 負債{N(c.DebtMultiplier)} = {N(c.CalculatedProgress)}h相当", "small"));
            if (task.DiscardedProgress > 0)
                scroll.Add(Text($"  完了上限による余り {N(task.DiscardedProgress)}h相当（勤務時間は返却されません）", "small"));
            if (task.CompletedThisWeek)
                scroll.Add(Text($"  完了効果：負債 {Signed(task.TechnicalDebtChange)} ／ 品質 {Signed(task.QualityChange)}", "small"));
        }
        card.Add(scroll);
        card.Add(ActionButton(continueRequired ? $"Week {Game.CurrentWeek} の計画へ →" : "計画に戻る", "continue", () =>
        {
            if (continueRequired) Session.ContinuePlanning();
            Render();
        }, "primary"));
    }

    private static string N(decimal value) => value.ToString("0.###");
    private static string Signed(decimal value) => value.ToString("+0.###;-0.###;0");
    private static string FailureText(PlanningFailure failure)
    {
        if (failure.IsReportOpen) return "週報を確認し、次週の計画へ進んでください。";
        switch (failure.AllocationReason)
        {
            case AllocationFailure.InvalidHours: return "時間は0以上の整数で入力してください。";
            case AllocationFailure.InsufficientHours: return "残り時間を超えています。他の割当を減らすか、入力時間を減らしてください。";
            case AllocationFailure.CompletedTask: return "完了済みのタスクには割り当てできません。";
            default: return "この社員またはタスクには割り当てできません。計画を確認してください。";
        }
    }
}
