// AND 条件（所有条件都要满足）
using System.Collections.Generic;
using System.Linq;
namespace TechCosmos.SkillSystem.Runtime
{
    public class AndCondition<T> : Condition<T> where T : IUnit<T>
    {
        private List<Condition<T>> _conditions;

        public AndCondition(params Condition<T>[] conditions)
        {
            _conditions = conditions.Where(c => c != null).ToList();
        }

        public override bool IsEligible(SkillContext<T> skillContext)
        {
            if (_conditions.Count == 0) return true;
            return _conditions.All(condition => condition.IsEligible(skillContext));
        }
    }
}
