using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>属性修改运算方式。</summary>
    public enum ModifierOperation
    {
        /// <summary>加法。</summary>
        Add,
        /// <summary>乘法。</summary>
        Multiply,
        /// <summary>覆盖（按优先级取最高）。</summary>
        Override
    }

    /// <summary>Buff 重复施加时的叠层策略。</summary>
    public enum ModifierStackPolicy
    {
        /// <summary>叠加层数。</summary>
        Stack,
        /// <summary>刷新持续时间。</summary>
        RefreshDuration,
        /// <summary>替换旧实例。</summary>
        Replace,
        /// <summary>忽略新施加。</summary>
        Ignore
    }

    /// <summary>单条属性修改器。</summary>
    [Serializable]
    public class StatModifier
    {
        /// <summary>属性键名。</summary>
        public string statKey;
        /// <summary>修改运算方式。</summary>
        public ModifierOperation operation = ModifierOperation.Add;
        /// <summary>修改值。</summary>
        public float value;
        /// <summary>Override 时的优先级。</summary>
        public int priority;
    }

    /// <summary>
    /// Buff 宿主基础接口：提供标签容器。
    /// </summary>
    public interface IBuffHost
    {
        /// <summary>状态标签容器。</summary>
        TagContainer Tags { get; }
    }

    /// <summary>
    /// Buff 宿主泛型接口：提供 GBF Buff 系统。
    /// </summary>
    public interface IBuffHost<T> : IBuffHost where T : class
    {
        /// <summary>Buff 管理系统。</summary>
        BuffSystem<T> BuffSystem { get; }
    }
}
