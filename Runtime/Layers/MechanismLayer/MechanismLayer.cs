using System;
using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class MechanismLayer<T> : IMechanismLayer<T> where T : class, IUnit<T>
    {
        private List<Action<SkillContext<T>>> _mechanisms = new List<Action<SkillContext<T>>>(6);
        public ISkill<T> Skill { get; set; }

        public void Mechanism(SkillContext<T> skillContext)
        {
            // 优化：局部变量 + for循环
            var mechanisms = _mechanisms;
            int count = mechanisms.Count;

            for (int i = 0; i < count; i++)
                mechanisms[i](skillContext);
        }

        public MechanismLayer(List<Action<SkillContext<T>>> actions = null)
        {
            if (actions != null)
                _mechanisms = new List<Action<SkillContext<T>>>(actions);
        }

        public void AddActionMechanism(Action<SkillContext<T>> action) => _mechanisms.Add(action);
        public void RemoveActionMechanism(Action<SkillContext<T>> action) => _mechanisms.Remove(action);
        public void ClearMechanisms() => _mechanisms.Clear();

        // 新增：批量添加优化
        public void AddMechanisms(params Action<SkillContext<T>>[] actions)
        {
            _mechanisms.AddRange(actions);
        }
    }
}