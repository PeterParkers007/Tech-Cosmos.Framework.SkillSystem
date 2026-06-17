using System;
namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 机制层接口：执行技能效果逻辑。
    /// </summary>
    public interface IMechanismLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        /// <summary>依次执行所有机制。</summary>
        public void Mechanism(SkillContext<T> skillContext);
        /// <summary>添加委托机制。</summary>
        public void AddMechanism(Action<SkillContext<T>> action);
        /// <summary>移除委托机制。</summary>
        public void RemoveActionMechanism(Action<SkillContext<T>> action);
        /// <summary>清空所有机制。</summary>
        public void ClearMechanisms();
    }
}
