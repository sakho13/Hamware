using System.Collections.Generic;

/// <summary>
/// 社員ごとの実勤務工数を基本スキルで補正し、タスク単位で合算して進捗へ反映する。
/// ゲーム状態は保持せず、GameSimulationから週次処理のたびに呼び出される。
/// </summary>
public class WeekProcessor
{
    private readonly ProductivityCalculator _productivityCalculator = new ProductivityCalculator();

    /// <summary>
    /// 割当(タスク→社員→実勤務工数)を読み取り、週送り時点のスキルで進捗を更新する。
    /// 割当辞書や社員の勤務時間は変更しない。完了を超えた進捗はタスク側で破棄する。
    /// </summary>
    public void Process(IReadOnlyDictionary<DevelopmentTask, Dictionary<Employee, int>> allocations)
    {
        foreach (var allocation in allocations)
        {
            var profile = TaskSkillProfiles.Get(allocation.Key.Type);
            decimal progress = 0m;
            foreach (var employeeHours in allocation.Value)
            {
                progress += _productivityCalculator.CalculateProgress(employeeHours.Key, profile, employeeHours.Value);
            }

            allocation.Key.ApplyProgress(progress);
        }
    }
}
