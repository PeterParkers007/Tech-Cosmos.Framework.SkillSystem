using System;
using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    public class SkillData<T> where T : IUnit<T>
    {
        //基础层
        public SkillType SkillType;
        public string TriggerEvent = string.Empty;

        //条件层
        public List<Condition<T>> Conditions = new();

        //信息层
        public string SkillName;
        public string SkillDescription;

        //机制层
        public List<Action<SkillContext<T>>> Mechanisms = new();

        public void AddMechanism(Action<SkillContext<T>> mechanism) => Mechanisms.Add(mechanism);
        public void AddMechanisms(Action<SkillContext<T>>[] mechanisms) => Mechanisms.AddRange(mechanisms);
        public void RemoveMechanism(Action<SkillContext<T>> mechanism) => Mechanisms.Remove(mechanism);
        public void AddCondition(Condition<T> condition) => Conditions.Add(condition);
        public void AddConditions(Condition<T>[] conditions) => Conditions.AddRange(conditions);
        public void RemoveCondition(Condition<T> condition) => Conditions.Remove(condition);
        public void ClearMechanisms() => Mechanisms.Clear();
        public void ClearConditions() => Conditions.Clear();
    }
}
