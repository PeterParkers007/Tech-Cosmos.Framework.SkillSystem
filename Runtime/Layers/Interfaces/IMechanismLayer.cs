using System;
namespace TechCosmos.SkillSystem.Runtime
{
    public interface IMechanismLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        public void Mechanism(SkillContext<T> skillContext);
        public void AddActionMechanism(Action<SkillContext<T>> action);
        public void RemoveActionMechanism(Action<SkillContext<T>> action);
        public void ClearMechanisms();
    }
}
