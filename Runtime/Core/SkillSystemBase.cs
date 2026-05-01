using System;
using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 非泛型上下文基类，仅用于类型匹配的标记
    /// </summary>
    public class SkillContextBase
    {
        public object Caster { get; set; }
        public object Target { get; set; }
        public Vector3 TargetPos { get; set; }
    }

    /// <summary>
    /// 非泛型数据层基接口
    /// </summary>
    public interface IDataLayerBase { }
    /// <summary>
    /// 泛型机制标记接口，用于 Editor 识别
    /// </summary>
    public interface ITypedMechanism
    {
        System.Type GetUnitType();
    }
    /// <summary>
    /// 条件非泛型基类，用于序列化和 Inspector 多态选择
    /// </summary>
    [Serializable]
    public abstract class ConditionBase
    {
        public abstract bool IsEligible(object context, IDataLayerBase dataLayer);
    }
    /// <summary>
    /// 非泛型机制基类，使用 object 参数以支持结构体的模式匹配
    /// </summary>
    [Serializable]
    public abstract class MechanismBase
    {
        /// <summary>
        /// 非泛型执行入口，context 为 object 以支持结构体模式匹配
        /// </summary>
        public abstract void ExecuteBase(object context, IDataLayerBase dataLayer);

        /// <summary>
        /// 获取机制的类型名称（用于 Editor 显示）
        /// </summary>
        public virtual string GetDisplayName() => GetType().Name;
    }
}