using System;

namespace TechCosmos.SkillSystem.Runtime
{
    public class ExecuteLayer<T> : IExecuteLayer<T> where T : class, IUnit<T>
    {
        public ISkill<T> Skill { get; set; }

        // ===== 全局事件 =====

        /// <summary>技能执行前（条件检查之前）</summary>
        public static event Action<SkillContext<T>> OnAnySkillExecuting;

        /// <summary>技能执行成功（条件通过、机制执行完毕）</summary>
        public static event Action<SkillContext<T>> OnAnySkillExecuted;

        /// <summary>技能执行失败（条件未通过）</summary>
        public static event Action<SkillContext<T>> OnAnySkillFailed;

        /// <summary>
        /// 执行技能：先检查条件，通过后执行机制
        /// </summary>
        public void Execute(SkillContext<T> skillContext)
        {
            OnAnySkillExecuting?.Invoke(skillContext);

            if (Skill.ConditionLayer.CheckCondition(skillContext))
            {
                Skill.MechanismLayer.Mechanism(skillContext);
                NotifyConditionsOnSuccess(skillContext);
                OnAnySkillExecuted?.Invoke(skillContext);
            }
            else
            {
                NotifyConditionsOnFailure(skillContext);
                OnAnySkillFailed?.Invoke(skillContext);
            }
        }

        #region 私有方法

        private void NotifyConditionsOnSuccess(SkillContext<T> skillContext)
        {
            if (Skill.ConditionLayer is ConditionLayer<T> conditionLayer)
            {
                var conditions = conditionLayer.Conditions;
                if (conditions == null) return;

                for (int i = 0; i < conditions.Count; i++)
                {
                    try
                    {
                        conditions[i]?.OnSkillExecuted(skillContext, Skill.DataLayer);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError(
                            $"[ExecuteLayer] 条件回调异常 [{conditions[i]?.GetType().Name}]: {ex.Message}");
                    }
                }
            }
        }

        private void NotifyConditionsOnFailure(SkillContext<T> skillContext)
        {
            if (Skill.ConditionLayer is ConditionLayer<T> conditionLayer)
            {
                var conditions = conditionLayer.Conditions;
                if (conditions == null) return;

                for (int i = 0; i < conditions.Count; i++)
                {
                    try
                    {
                        conditions[i]?.OnConditionFailed(skillContext, Skill.DataLayer);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError(
                            $"[ExecuteLayer] 条件回调异常 [{conditions[i]?.GetType().Name}]: {ex.Message}");
                    }
                }
            }
        }

        #endregion
    }
}