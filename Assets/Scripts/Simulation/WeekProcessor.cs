using System;
using System.Collections.Generic;

/// <summary>
/// 社員ごとの実勤務工数をスキル・疲労・負債で補正し、進捗と新規完了によるプロダクトの変化を反映する。
/// ゲーム状態は保持せず、GameSimulationから週次処理のたびに呼び出される。
/// </summary>
internal sealed class WeekProcessor
{
    private readonly ProductivityCalculator _productivityCalculator = new ProductivityCalculator();

    /// <summary>
    /// 全割当の検証・計算が成功してから進捗と完了効果を反映する。計算中は状態を変更しない。
    /// 当週の進捗はすべて開始時の負債で計算する。完了効果は合算して一度だけ状態へ反映する。
    /// 割当辞書や社員の勤務時間・疲労は変更しない。完了を超えた進捗はタスク側で破棄する。
    /// </summary>
    /// <exception cref="ArgumentNullException">allocationsまたはproductがnullの場合</exception>
    /// <exception cref="ArgumentException">割当内のタスクまたは社員別の割当がnullの場合</exception>
    /// <exception cref="ArgumentOutOfRangeException">実勤務工数が負の場合</exception>
    internal void Process(IReadOnlyDictionary<DevelopmentTask, Dictionary<Employee, int>> allocations, ProductState product)
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
        var results = new List<(DevelopmentTask Task, decimal Progress)>();
        decimal technicalDebtChange = 0m;
        decimal qualityChange = 0m;

        // 1. 入力の検証と計算を全件済ませる。後続の割当が不正でも先行タスクは未変更のまま。
        foreach (var allocation in allocations)
        {
            var task = allocation.Key;
            if (task == null || allocation.Value == null)
            {
                throw new ArgumentException("タスクと社員別の割当はnullにできません。", nameof(allocations));
            }

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

            results.Add((task, progress));
            if (progress >= task.RequiredProgressHours - task.CompletedProgressHours)
            {
                technicalDebtChange += task.RequiredProgressHours * productProfile.TechnicalDebtChangePerProgressHour;
                qualityChange += task.RequiredProgressHours * productProfile.QualityChangePerProgressHour;
            }
        }

        // 2. 検証済みの計算結果だけを反映する。完了効果の計算も上で済ませ、部分更新を防ぐ。
        foreach (var result in results)
        {
            result.Task.ApplyProgress(result.Progress);
        }

        product.ApplyChanges(technicalDebtChange, qualityChange);
    }
}
