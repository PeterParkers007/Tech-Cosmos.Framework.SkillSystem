using System;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    public abstract class Mechanism<T> : MechanismBase, ITypedMechanism where T : class, IUnit<T>
    {
        public override void ExecuteBase(object context, IDataLayerBase dataLayer)
        {
            if (context is SkillContext<T> typedContext && dataLayer is IDataLayer<T> typedDataLayer)
            {
                Execute(typedContext, typedDataLayer);
            }
        }

        public virtual void Execute(SkillContext<T> context, IDataLayer<T> dataLayer) { }
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