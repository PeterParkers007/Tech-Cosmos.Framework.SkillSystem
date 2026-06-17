namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能单位核心接口，定义事件触发与技能管理能力。
    /// </summary>
    public interface IUnit<T> where T : class, IUnit<T>
    {
        /// <summary>返回该单位支持的事件名称列表。</summary>
        string[] GetSupportedEvents();
        /// <summary>使用完整上下文触发指定事件。</summary>
        void TriggerEvent(string eventName, SkillContext<T> context);
        /// <summary>向单位注册技能。</summary>
        void AddSkill(ISkill<T> skill);
        /// <summary>从单位移除技能。</summary>
        void RemoveSkill(ISkill<T> skill);
    }
}
