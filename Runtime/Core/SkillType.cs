namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能类型，区分主动与被动技能。
    /// </summary>
    public enum SkillType
    {
        /// <summary>主动技能，由玩家或 AI 主动触发。</summary>
        Active,
        /// <summary>被动技能，注册后自动生效或监听事件触发。</summary>
        Passive
    }
}
