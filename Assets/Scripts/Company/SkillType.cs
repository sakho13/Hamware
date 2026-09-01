/// <summary>
/// 社員が保有する基本スキルの種類。
/// 個別技術(React, PostgreSQL, AWS等)ではなく、抽象度の高い汎用能力を表す。
/// 個別技術・具体的経験は将来的に別概念(SpecificSkill等)として扱う想定で、本列挙には含めない。
/// </summary>
public enum SkillType
{
    Frontend,
    Backend,
    Database,
    Infrastructure,
    Testing,
    Security,
    Architecture,
    Planning,
    Management,
    Communication,
    Sales,
    CustomerSupport
}
