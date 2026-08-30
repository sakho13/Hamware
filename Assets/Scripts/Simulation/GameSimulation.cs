using UnityEngine;

public class GameSimulation
{
    /// <summary>
    /// 現在の週 ゲームの基本単位
    /// </summary>
    public int CurrentWeek { get; private set; } = 1;

    /// <summary>
    /// 1週間に使える工数
    /// </summary>
    public int AvailableHours { get; private set; } = 40;

    public void AdvanceWeek() {
        this.CurrentWeek++;
        this.AvailableHours = 40; // マジックナンバー 将来修正
    }
}
