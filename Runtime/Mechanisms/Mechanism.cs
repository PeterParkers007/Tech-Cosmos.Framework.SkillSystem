using System;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    /// <summary>
    /// 泛型机制基类：将非泛型执行入口转发到类型化 Execute。
    /// </summary>
    public abstract class Mechanism<T> : MechanismBase, ITypedMechanism where T : class, IUnit<T>
    {
        public override void ExecuteBase(object context, IDataLayerBase dataLayer)
        {
            if (context is SkillContext<T> typedContext && dataLayer is IDataLayer<T> typedDataLayer)
            {
                Execute(typedContext, typedDataLayer);
                return;
            }

            Debug.LogWarning(
                $"[Mechanism] 类型不匹配，跳过执行: {GetType().Name} 需要 SkillContext<{typeof(T).Name}> / IDataLayer<{typeof(T).Name}>，" +
                $"实际 context={context?.GetType().Name ?? "null"}, dataLayer={dataLayer?.GetType().Name ?? "null"}");
        }

        /// <summary>执行机制效果。</summary>
        public virtual void Execute(SkillContext<T> context, IDataLayer<T> dataLayer) { }
        /// <summary>技能回滚/卸载时的清理回调。</summary>
        public virtual void SkillBack(ISkill<T> skill) { }

        public System.Type GetUnitType() => typeof(T);

        public override string GetDisplayName()
        {
            // 移除泛型后缀，显示为 "DamageMechanism<T>"
            var name = GetType().Name;
            if (name.Contains("`"))
            {
                name = name.Substring(0, name.IndexOf('`'));
            }
            return $"{name}<{typeof(T).Name}>";
        }
    }
}