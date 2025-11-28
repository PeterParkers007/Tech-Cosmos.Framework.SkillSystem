// NOT 条件（取反）
using System;
namespace TechCosmos.SkillSystem.Runtime
{
    public class NotCondition<T> : Condition<T> where T : IUnit<T>
    {
        private Condition<T> _condition;

        public NotCondition(Condition<T> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public override bool IsEligible(SkillContext<T> skillContext)
            => !_condition.IsEligible(skillContext);
    }
}
