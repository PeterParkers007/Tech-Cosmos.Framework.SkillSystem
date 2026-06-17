using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 逻辑非条件：对子条件结果取反。
    /// </summary>
    public class NotCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private Condition<T> _condition;

        public NotCondition(Condition<T> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        /// <summary>重新绑定被取反的子条件。</summary>
        public void Reinitialize(Condition<T> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        /// <summary>清空子条件引用（用于对象池归还）。</summary>
        public void Clear()
        {
            _condition = null;
        }

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => _condition != null && !_condition.IsEligible(skillContext, dataLayer);

        public override void OnReset()
            => _condition?.OnReset();
        public override void OnSkillExecuted(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {
            _condition?.OnSkillExecuted(skillContext, dataLayer);
        }

        public override void OnConditionFailed(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {
            _condition?.OnConditionFailed(skillContext, dataLayer);
        }
    }
}