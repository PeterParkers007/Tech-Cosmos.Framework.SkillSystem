using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 机制执行层：依次执行委托机制与序列化机制，并支持 SkillBack 回滚。
    /// </summary>
    public class MechanismLayer<T> : IMechanismLayer<T> where T : class, IUnit<T>
    {
        [SerializeReference]
        private List<MechanismBase> _mechanisms = new();
        private List<Action<SkillContext<T>>> _funcMechanisms = new List<Action<SkillContext<T>>>(6);
        public ISkill<T> Skill { get; set; }

        /// <summary>机制执行出错时的处理策略。</summary>
        public MechanismErrorPolicy ErrorPolicy { get; set; } = MechanismErrorPolicy.ContinueOnError;

        public void Mechanism(SkillContext<T> skillContext)
            => Mechanism(skillContext, ErrorPolicy);

        public void Mechanism(SkillContext<T> skillContext, MechanismErrorPolicy policy)
        {
            SkillProfilerMarkers.Mechanism.Begin();
            try
            {
                var funcMechanisms = _funcMechanisms;
                for (int i = 0; i < funcMechanisms.Count; i++)
                {
                    try { funcMechanisms[i]?.Invoke(skillContext); }
                    catch (Exception ex)
                    {
                        LogMechanismError("FuncMechanism", ex);
                        if (policy == MechanismErrorPolicy.FailFast) break;
                    }
                }

                var mechanisms = _mechanisms;
                for (int i = 0; i < mechanisms.Count; i++)
                {
                    try { mechanisms[i]?.ExecuteBase(skillContext, Skill.DataLayer); }
                    catch (Exception ex)
                    {
                        LogMechanismError(mechanisms[i]?.GetType().Name ?? "Mechanism", ex);
                        if (policy == MechanismErrorPolicy.FailFast) break;
                    }
                }
            }
            finally
            {
                SkillProfilerMarkers.Mechanism.End();
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

        public void ClearMechanisms()
        {
            _funcMechanisms.Clear();
            _mechanisms.Clear();
        }

        /// <summary>对所有支持回滚的机制执行 SkillBack。</summary>
        public void InvokeSkillBack(ISkill<T> skill)
        {
            var mechanisms = _mechanisms;
            for (int i = 0; i < mechanisms.Count; i++)
            {
                if (mechanisms[i] is Mechanism<T> typedMechanism)
                {
                    try { typedMechanism.SkillBack(skill); }
                    catch (Exception ex) { LogMechanismError(typedMechanism.GetType().Name, ex); }
                }
            }
        }

        private static void LogMechanismError(string name, Exception ex)
            => Debug.LogError($"[MechanismLayer] 机制执行异常 [{name}]: {ex.Message}");
    }
}
