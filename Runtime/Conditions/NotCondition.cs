using System;

namespace TechCosmos.SkillSystem.Runtime
{
    public class NotCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private Condition<T> _condition;

        public NotCondition(Condition<T> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public void Reinitialize(Condition<T> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public void Clear()
        {
            _condition = null;
        }

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => !_condition.IsEligible(skillContext, dataLayer);

        // 转发回调到内部条件
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