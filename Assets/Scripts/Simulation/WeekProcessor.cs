using System;
using System.Collections.Generic;

/// <summary>
/// 社員ごとの実勤務工数をスキル・疲労・負債で補正し、進捗と新規完了によるプロダクトの変化を反映する。
/// ゲーム状態は保持せず、GameSimulationから週次処理のたびに呼び出される。
/// </summary>
public class WeekProcessor
{
    private readonly ProductivityCalculator _productivityCalculator = new ProductivityCalculator();

    /// <summary>
    /// 当週の進捗はすべて開始時の負債で計算する。全タスク処理後に完了効果を合算し、一度だけ状態を更新する。
    /// 割当辞書や社員の勤務時間・疲労は変更しない。完了を超えた進捗はタスク側で破棄する。
    /// </summary>
    /// <exception cref="ArgumentNullException">allocationsまたはproductがnullの場合</exception>
    public void Process(IReadOnlyDictionary<DevelopmentTask, Dictionary<Employee, int>> allocations, ProductState product)
    {
        if (allocations == null)
        {
            throw new ArgumentNullException(nameof(allocations));
        }

        if (product == null)
        {
            throw new ArgumentNullException(nameof(product));
        }

        decimal debtMultiplier = ProductHealthCalculator.CalculateProductivityMultiplier(product.TechnicalDebt);
        decimal technicalDebtChange = 0m;
        decimal qualityChange = 0m;
        foreach (var allocation in allocations)
        {
            var task = allocation.Key;
            if (task.IsCompleted)
            {
                continue;
            }

            var profile = TaskSkillProfiles.Get(task.Type);
            var productProfile = TaskProductProfiles.Get(task.Type);
            decimal productMultiplier = productProfile.IsDebtSensitive ? debtMultiplier : 1m;
            decimal progress = 0m;
            foreach (var employeeHours in allocation.Value)
            {
                progress += _productivityCalculator.CalculateProgress(employeeHours.Key, profile, employeeHours.Value)
                    * productMultiplier;
            }

            task.ApplyProgress(progress);
            if (task.IsCompleted)
            {
                technicalDebtChange += task.RequiredProgressHours * productProfile.TechnicalDebtChangePerProgressHour;
                qualityChange += task.RequiredProgressHours * productProfile.QualityChangePerProgressHour;
            }
        }

        product.ApplyChanges(technicalDebtChange, qualityChange);
    }
}
