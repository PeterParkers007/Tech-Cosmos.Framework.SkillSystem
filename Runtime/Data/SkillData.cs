using System;
using System.Collections.Generic;
using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    public class SkillData<T> where T : class, IUnit<T>
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
        public List<Action<SkillContext<T>>> FuncMechanisms = new();
        [SerializeReference]
        public List<MechanismBase> Mechanisms = new();
        //数值层
        public Dictionary<string,object> Data = new();

        public void SetValue<TValue>(string key, TValue value) => Data[key] = value;
        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula)
            => Data[key] = formula;
        public TValue GetValue<TValue>(string key) => (TValue)Data[key];
        public void AddMechanism(Action<SkillContext<T>> mechanism) => FuncMechanisms.Add(mechanism);
        public void AddMechanism(Mechanism<T> mechanism) => Mechanisms.Add(mechanism);
        public void AddMechanism(params Action<SkillContext<T>>[] mechanisms) => FuncMechanisms.AddRange(mechanisms);
        public void AddMechanism(params Mechanism<T>[] mechanisms) => Mechanisms.AddRange(mechanisms);
        public void RemoveMechanism(Action<SkillContext<T>> mechanism) => FuncMechanisms.Remove(mechanism);
        public void AddCondition(Condition<T> condition) => Conditions.Add(condition);
        public void AddCondition(Condition<T>[] conditions) => Conditions.AddRange(conditions);
        public void RemoveCondition(Condition<T> condition) => Conditions.Remove(condition);
        public void ClearMechanism() => FuncMechanisms.Clear();
        public void ClearCondition() => Conditions.Clear();
    }
}
