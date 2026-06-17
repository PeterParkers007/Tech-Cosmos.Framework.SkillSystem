using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能执行拥有者：提供施法控制器以管理读条与引导。
    /// </summary>
    public interface ISkillExecutionOwner<T> where T : class, IUnit<T>
    {
        /// <summary>施法状态控制器。</summary>
        SkillExecutionController<T> ExecutionController { get; }
    }
}
