namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 单次技能执行的元数据，提供时钟、随机源及网络角色等运行时服务。
    /// </summary>
    public struct SkillExecutionMeta
    {
        /// <summary>本次执行的唯一标识。</summary>
        public int executionId;
        /// <summary>当前逻辑帧序号。</summary>
        public int tick;
        /// <summary>网络角色，用于区分本地/服务端/客户端逻辑。</summary>
        public NetworkRole networkRole;
        /// <summary>时间时钟，为 null 时回退到全局默认。</summary>
        public ISkillClock clock;
        /// <summary>随机数提供者，为 null 时回退到全局默认。</summary>
        public IRandomProvider random;
        /// <summary>本次执行共享的黑板数据。</summary>
        public SkillBlackboard blackboard;
        /// <summary>触发本次执行的事件名称。</summary>
        public string triggerEvent;
        /// <summary>执行是否已被取消。</summary>
        public bool cancelled;

        /// <summary>解析可用时钟，优先使用实例级，否则使用全局服务。</summary>
        public ISkillClock ResolveClock() => clock ?? SkillSystemServices.Clock;

        /// <summary>解析可用随机源，优先使用实例级，否则使用全局服务。</summary>
        public IRandomProvider ResolveRandom() => random ?? SkillSystemServices.Random;
    }
}
