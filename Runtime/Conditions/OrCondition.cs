// OR 条件（任意条件满足）
using System.Collections.Generic;
using System.Linq;
namespace TechCosmos.SkillSystem.Runtime
{
    public class OrCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private List<Condition<T>> _conditions;

        public OrCondition(params Condition<T>[] conditions)
        {
            _conditions = conditions.Where(c => c != null).ToList();
        }

        public override bool IsEligible(SkillContext<T> skillContext)
        {
            if (_conditions.Count == 0) return true;

            // 优化：for循环代替LINQ
            var conditions = _conditions;
            int count = conditions.Count;

            for (int i = 0; i < count; i++)
            {
                if (conditions[i].IsEligible(skillContext))
                    return true;
            }
            return false;
        }
        public void Reinitialize(params Condition<T>[] conditions)
        {
            _conditions.Clear();
            _conditions.AddRange(conditions.Where(c => c != null));
        }

        public void Clear()
        {
            _conditions.Clear();
        }
    }
}
