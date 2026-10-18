using System;
using System.Collections.Generic;

/// <summary>
/// 社員を表すドメインモデル。
/// 名前と1週間あたりの利用可能工数を持ち、
/// 週内の残り利用可能工数を自身で管理する。
/// また、基本スキル(種類とレベル)の集合と疲労を自身で管理する。
/// 職種はEmployeeの固定属性としては持たない
/// (将来的な採用時のスキル分布テンプレート/表示ラベルとして別概念で扱う想定)。
/// </summary>
public class Employee
{
    private readonly Dictionary<SkillType, Skill> _skills = new Dictionary<SkillType, Skill>();

    /// <summary>
    /// 社員名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 1週間に割り当てられる勤務工数の上限(時間)
    /// </summary>
    public int WeeklyAvailableHours { get; }

    /// <summary>
    /// 今週まだ割り当てられていない残り利用可能工数(時間)。
    /// コンストラクタでWeeklyAvailableHoursへ初期化され、
    /// 割当時に内部で消費され、週末のCompleteWeekでWeeklyAvailableHoursへ戻る。
    /// </summary>
    public int AvailableHours { get; private set; }

    /// <summary>
    /// 疲労値(0〜100)。週末に実勤務工数から更新され、翌週の生産性へ影響する。
    /// </summary>
    public decimal Fatigue { get; private set; }

    /// <summary>
    /// 保有している基本スキル一覧。
    /// スキルを持たない場合は空のコレクションになる。
    /// </summary>
    public IReadOnlyCollection<Skill> Skills => _skills.Values;

    /// <summary>
    /// 社員を生成する。
    /// </summary>
    /// <param name="name">社員名</param>
    /// <param name="weeklyAvailableHours">1週間あたりの利用可能工数(時間)</param>
    /// <param name="initialSkills">
    /// 初期スキル分布。省略した場合(null)はスキルなしの状態で生成される。
    /// 同一SkillTypeが複数含まれる場合は後勝ち(upsert)で反映される。
    /// </param>
    /// <param name="initialFatigue">初期疲労値(0〜100)。省略時は0。</param>
    /// <exception cref="ArgumentOutOfRangeException">weeklyAvailableHoursが0以下、またはinitialFatigueが範囲外の場合</exception>
    public Employee(string name, int weeklyAvailableHours, IEnumerable<Skill> initialSkills = null, decimal initialFatigue = 0m)
    {
        if (weeklyAvailableHours <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weeklyAvailableHours),
                "WeeklyAvailableHoursは1以上である必要があります。");
        }

        if (initialFatigue < FatigueCalculator.MinFatigue || initialFatigue > FatigueCalculator.MaxFatigue)
        {
            throw new ArgumentOutOfRangeException(nameof(initialFatigue), "初期疲労は0以上100以下である必要があります。");
        }

        Name = name;
        WeeklyAvailableHours = weeklyAvailableHours;
        AvailableHours = weeklyAvailableHours;
        Fatigue = initialFatigue;

        if (initialSkills != null)
        {
            foreach (var skill in initialSkills)
            {
                AddSkill(skill);
            }
        }
    }

    /// <summary>
    /// 残り利用可能工数からhours分を消費する。
    /// 0以下の工数、または残り利用可能工数を超える消費は拒否しfalseを返す。
    /// 成功した場合のみAvailableHoursを減少させる。
    /// </summary>
    internal bool TryConsumeHours(int hours)
    {
        if (hours <= 0 || hours > AvailableHours)
        {
            return false;
        }

        AvailableHours -= hours;
        return true;
    }

    /// <summary>
    /// 残り利用可能工数をWeeklyAvailableHoursへリセットする。
    /// 疲労更新後にCompleteWeekからのみ呼び出す。
    /// </summary>
    private void ResetWeeklyHours()
    {
        AvailableHours = WeeklyAvailableHours;
    }

    /// <summary>
    /// 今週の消費勤務時間から疲労を更新した後、翌週の利用可能工数を復元する。
    /// 全タスクの進捗反映後にGameSimulationから一度だけ呼び出す。
    /// </summary>
    internal void CompleteWeek()
    {
        int workedHours = WeeklyAvailableHours - AvailableHours;
        Fatigue = FatigueCalculator.CalculateNextFatigue(Fatigue, workedHours, WeeklyAvailableHours);
        ResetWeeklyHours();
    }

    /// <summary>
    /// 基本スキルを追加する。
    /// 同一SkillTypeのスキルが既に存在する場合はレベルを上書き更新する(upsert)。
    /// 将来的なスキル成長(レベルアップ)は、このメソッドで新しいレベルのSkillを
    /// 再登録することで表現する想定(成長処理自体は本メソッドの対象外)。
    /// skillがnullの場合は何もしない。
    /// </summary>
    public void AddSkill(Skill skill)
    {
        if (skill == null)
        {
            return;
        }

        _skills[skill.Type] = skill;
    }

    /// <summary>
    /// 指定した基本スキルを取得する。未習得・未定義種別の場合はfalseを返し、skillはnullになる。
    /// </summary>
    public bool TryGetSkill(SkillType type, out Skill skill)
    {
        return _skills.TryGetValue(type, out skill);
    }
}
