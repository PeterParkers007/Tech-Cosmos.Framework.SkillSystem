using System;
using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class MechanismLayer<T> : IMechanismLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }

        public void Mechanism(SkillContext<T> skillContext) => ActionMechanism?.Invoke(skillContext);

        private Action<SkillContext<T>> ActionMechanism { get; set; }

        public MechanismLayer(List<Action<SkillContext<T>>> actions = null)
        {
            if (actions != null)
            {
                foreach (Action<SkillContext<T>> action in actions)
                    AddActionMechanism(action);
            }
        }

        public void AddActionMechanism(Action<SkillContext<T>> action) => ActionMechanism += action;
        public void RemoveActionMechanism(Action<SkillContext<T>> action) => ActionMechanism -= action;
        public void ClearMechanisms() => ActionMechanism = null;
    }
}
