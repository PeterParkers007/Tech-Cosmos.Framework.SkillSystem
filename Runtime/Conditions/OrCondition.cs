// OR 条件（任意条件满足）
using System.Collections.Generic;
using System.Linq;
namespace TechCosmos.SkillSystem.Runtime
{
    public class OrCondition<T> : Condition<T> where T : IUnit<T>
    {
        private List<Condition<T>> _conditions;

        public OrCondition(params Condition<T>[] conditions)
        {
            _conditions = conditions.Where(c => c != null).ToList();
        }

        public override bool IsEligible(SkillContext<T> skillContext)
        {
            if (_conditions.Count == 0) return true;
            return _conditions.Any(condition => condition.IsEligible(skillContext));
        }
    }
}
