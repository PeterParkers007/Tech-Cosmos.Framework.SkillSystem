using System;
using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class MechanismLayer<T> : IMechanismLayer<T> where T : class, IUnit<T>
    {
        private List<Action<SkillContext<T>>> _mechanisms = new(6);
        public ISkill<T> Skill { get; set; }

        public void Mechanism(SkillContext<T> skillContext)
        {
            for (int i = 0; i < _mechanisms.Count; i++) _mechanisms[i](skillContext);
        }

        public MechanismLayer(List<Action<SkillContext<T>>> actions = null)
        {
            if (actions != null)
            {
                foreach (Action<SkillContext<T>> action in actions)
                    AddActionMechanism(action);
            }
        }

        public void AddActionMechanism(Action<SkillContext<T>> action) => _mechanisms.Add(action);
        public void RemoveActionMechanism(Action<SkillContext<T>> action) => _mechanisms.Remove(action);
        public void ClearMechanisms() => _mechanisms.Clear();
        public void AddMechanisms(params Action<SkillContext<T>>[] actions)
        {
            // 一次添加多个，减少方法调用开销
            _mechanisms.AddRange(actions);
        }
    }
}
