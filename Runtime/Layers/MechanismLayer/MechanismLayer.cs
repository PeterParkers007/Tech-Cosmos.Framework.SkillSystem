using System;
using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class MechanismLayer<T> : IMechanismLayer<T> where T : class, IUnit<T>
    {
        private List<Mechanism<T>> _mechanisms = new List<Mechanism<T>>();
        private List<Action<SkillContext<T>>> _funcMechanisms = new List<Action<SkillContext<T>>>(6);
        public ISkill<T> Skill { get; set; }

        public void Mechanism(SkillContext<T> skillContext)
        {
            // 优化：局部变量 + for循环
            var funcMechanisms = _funcMechanisms;
            int funcCount = funcMechanisms.Count;

            for (int i = 0; i < funcCount; i++)
                funcMechanisms[i](skillContext);

            var mechanisms = _mechanisms;
            int mechanismsCount = mechanisms.Count;

            for (int i = 0; i < mechanismsCount; i++)
                mechanisms[i].Execute(skillContext);
        }

        public MechanismLayer(List<Mechanism<T>> mechanisms = null,List<Action<SkillContext<T>>> actions = null)
        {
            if (mechanisms != null) AddMechanism(mechanisms.ToArray());
            if (actions != null) AddMechanism(actions.ToArray());
        }

        public void AddMechanism(Action<SkillContext<T>> action) => _funcMechanisms.Add(action);
        public void AddMechanism(Mechanism<T> mechanism) => _mechanisms.Add(mechanism);
        public void AddMechanism(params Action<SkillContext<T>>[] actions) => _funcMechanisms.AddRange(actions);
        public void AddMechanism(params Mechanism<T>[] mechanisms) => _mechanisms.AddRange(mechanisms);
        public void RemoveActionMechanism(Action<SkillContext<T>> action) => _funcMechanisms.Remove(action);
        public void ClearMechanisms() => _funcMechanisms.Clear();
    }
}