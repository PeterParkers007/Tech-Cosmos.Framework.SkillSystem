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

        // 池化支持：重新初始化
        public void Reinitialize(Condition<T> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        // 池化支持：清理
        public void Clear()
        {
            _condition = null; // 或设置为默认值
        }

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => !_condition.IsEligible(skillContext, dataLayer);
    }
}