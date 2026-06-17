namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>技能执行结果。</summary>
    public enum SkillExecutionResult
    {
        /// <summary>执行成功。</summary>
        Success,
        /// <summary>条件未满足。</summary>
        ConditionFailed,
        /// <summary>被中间件或外部取消。</summary>
        Cancelled,
        /// <summary>正在施法中。</summary>
        Casting,
        /// <summary>正在引导中。</summary>
        Channeling,
        /// <summary>被阻塞（如优先级不足）。</summary>
        Blocked,
        /// <summary>执行过程抛出异常。</summary>
        Error
    }

    /// <summary>机制执行出错时的处理策略。</summary>
    public enum MechanismErrorPolicy
    {
        /// <summary>记录错误并继续执行后续机制。</summary>
        ContinueOnError,
        /// <summary>遇到错误立即停止。</summary>
        FailFast
    }
}
