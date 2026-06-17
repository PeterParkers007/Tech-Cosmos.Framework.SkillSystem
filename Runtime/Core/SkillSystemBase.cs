using System;
using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 非泛型上下文基类，仅用于类型匹配的标记。
    /// </summary>
    public class SkillContextBase
    {
        /// <summary>施法者对象。</summary>
        public object Caster { get; set; }
        /// <summary>目标对象。</summary>
        public object Target { get; set; }
        /// <summary>目标世界坐标。</summary>
        public Vector3 TargetPos { get; set; }
    }

    /// <summary>
    /// 非泛型数据层基接口。
    /// </summary>
    public interface IDataLayerBase { }
    /// <summary>
    /// 泛型机制标记接口，用于 Editor 识别。
    /// </summary>
    public interface ITypedMechanism
    {
        /// <summary>返回机制绑定的单位类型。</summary>
        System.Type GetUnitType();
    }
    /// <summary>
    /// 条件非泛型基类，用于序列化和 Inspector 多态选择。
    /// </summary>
    [Serializable]
    public abstract class ConditionBase
    {
        /// <summary>
        /// 检查条件是否满足。
        /// </summary>
        public abstract bool IsEligible(object context, IDataLayerBase dataLayer);

        /// <summary>
        /// 技能成功执行后的回调（由 ExecuteLayer 调用）。
        /// 用于冷却计时、状态更新等后置处理。
        /// </summary>
        public virtual void OnSkillExecuted(object context, IDataLayerBase dataLayer) { }

        /// <summary>
        /// 条件检查失败时的回调。
        /// </summary>
        public virtual void OnConditionFailed(object context, IDataLayerBase dataLayer) { }

        /// <summary>
        /// 技能被移除或重置时的回调。
        /// </summary>
        public virtual void OnReset() { }
    }
    /// <summary>
    /// 非泛型机制基类，使用 object 参数以支持结构体的模式匹配。
    /// </summary>
    [Serializable]
    public abstract class MechanismBase
    {
        /// <summary>
        /// 非泛型执行入口，context 为 object 以支持结构体模式匹配。
        /// </summary>
        public abstract void ExecuteBase(object context, IDataLayerBase dataLayer);

        /// <summary>
        /// 获取机制的显示名称（用于 Editor 展示）。
        /// </summary>
        public virtual string GetDisplayName() => GetType().Name;
    }
}