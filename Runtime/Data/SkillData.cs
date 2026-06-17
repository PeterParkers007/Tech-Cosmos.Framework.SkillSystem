using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 运行时技能数据容器，由 <see cref="SkillDataSO{T}"/> 或代码构建后交给 <see cref="SkillFactory{T}"/> 实例化。
    /// </summary>
    [Serializable]
    public class SkillData<T> where T : class, IUnit<T>
    {
        /// <summary>技能类型（主动/被动）。</summary>
        public SkillType SkillType;

        /// <summary>可触发本技能的事件名列表。</summary>
        public List<string> TriggerEvents = new List<string>();

        /// <summary>施法配置（读条、引导、优先级等）。</summary>
        public SkillProfile Profile = new();

        /// <summary>技能时间轴配置。</summary>
        public SkillTimelineData Timeline = new();

        /// <summary>运行时条件列表。</summary>
        public List<Condition<T>> Conditions = new();

        /// <summary>技能显示名称。</summary>
        public string SkillName;

        /// <summary>技能描述文本。</summary>
        public string SkillDescription;

        /// <summary>委托形式的机制（不可序列化）。</summary>
        public List<Action<SkillContext<T>>> FuncMechanisms = new();

        [SerializeReference]
        /// <summary>序列化机制列表。</summary>
        public List<MechanismBase> Mechanisms = new();

        /// <summary>自定义数据键值表（供 DataLayer 读取）。</summary>
        public Dictionary<string, object> Data = new();

        /// <summary>写入静态数据值。</summary>
        public void SetValue<TValue>(string key, TValue value) => Data[key] = value;

        /// <summary>写入公式委托，运行时按上下文求值。</summary>
        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula) => Data[key] = formula;

        /// <summary>尝试读取指定 key 的数据，类型不匹配时返回 false。</summary>
        public bool TryGetValue<TValue>(string key, out TValue value)
        {
            if (Data.TryGetValue(key, out var raw) && raw is TValue typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>读取指定 key 的数据，不存在时输出警告并返回 default。</summary>
        public TValue GetValue<TValue>(string key)
        {
            if (TryGetValue<TValue>(key, out var value)) return value;
            Debug.LogWarning($"[SkillData] 找不到数据键 '{key}'");
            return default;
        }

        /// <summary>添加委托机制。</summary>
        public void AddMechanism(Action<SkillContext<T>> mechanism) => FuncMechanisms.Add(mechanism);

        /// <summary>添加序列化机制。</summary>
        public void AddMechanism(Mechanism<T> mechanism) => Mechanisms.Add(mechanism);

        /// <summary>批量添加委托机制。</summary>
        public void AddMechanism(params Action<SkillContext<T>>[] mechanisms) => FuncMechanisms.AddRange(mechanisms);

        /// <summary>批量添加序列化机制。</summary>
        public void AddMechanism(params Mechanism<T>[] mechanisms) => Mechanisms.AddRange(mechanisms);

        /// <summary>移除指定委托机制。</summary>
        public void RemoveMechanism(Action<SkillContext<T>> mechanism) => FuncMechanisms.Remove(mechanism);

        /// <summary>添加单个条件。</summary>
        public void AddCondition(Condition<T> condition) => Conditions.Add(condition);

        /// <summary>批量添加条件。</summary>
        public void AddCondition(Condition<T>[] conditions) => Conditions.AddRange(conditions);

        /// <summary>移除指定条件。</summary>
        public void RemoveCondition(Condition<T> condition) => Conditions.Remove(condition);

        /// <summary>清空所有机制（委托与序列化）。</summary>
        public void ClearMechanism()
        {
            FuncMechanisms.Clear();
            Mechanisms.Clear();
        }

        /// <summary>清空所有条件。</summary>
        public void ClearCondition() => Conditions.Clear();
    }
}
