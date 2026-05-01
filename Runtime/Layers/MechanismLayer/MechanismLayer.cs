using System;
using System.Collections.Generic;
using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    public class MechanismLayer<T> : IMechanismLayer<T> where T : class, IUnit<T>
    {
        [SerializeReference]
        private List<MechanismBase> _mechanisms = new();
        private List<Action<SkillContext<T>>> _funcMechanisms = new List<Action<SkillContext<T>>>(6);
        public ISkill<T> Skill { get; set; }

        public void Mechanism(SkillContext<T> skillContext)
        {
            // 执行函数式机制
            var funcMechanisms = _funcMechanisms;
            int funcCount = funcMechanisms.Count;
            for (int i = 0; i < funcCount; i++)
                funcMechanisms[i]?.Invoke(skillContext);

            // 执行对象机制 - 直接传递 SkillContext<T>（会自动装箱为 object）
            var mechanisms = _mechanisms;
            int mechanismsCount = mechanisms.Count;
            for (int i = 0; i < mechanismsCount; i++)
            {
                mechanisms[i]?.ExecuteBase(skillContext, Skill.DataLayer);
            }
        }

        public MechanismLayer(List<MechanismBase> mechanisms = null, List<Action<SkillContext<T>>> actions = null)
        {
            if (mechanisms != null) _mechanisms.AddRange(mechanisms);
            if (actions != null) _funcMechanisms.AddRange(actions);
        }

        public void AddMechanism(Action<SkillContext<T>> action) => _funcMechanisms.Add(action);
        public void AddMechanism(Mechanism<T> mechanism) => _mechanisms.Add(mechanism);
        public void AddMechanism(params Action<SkillContext<T>>[] actions) => _funcMechanisms.AddRange(actions);
        public void AddMechanism(params Mechanism<T>[] mechanisms) => _mechanisms.AddRange(mechanisms);
        public void RemoveActionMechanism(Action<SkillContext<T>> action) => _funcMechanisms.Remove(action);
        public void ClearMechanisms() => _funcMechanisms.Clear();
    }
}