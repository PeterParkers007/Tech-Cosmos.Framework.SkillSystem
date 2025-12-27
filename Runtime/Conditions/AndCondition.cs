// AND 条件（所有条件都要满足）
using System.Collections.Generic;
using System.Linq;
namespace TechCosmos.SkillSystem.Runtime
{
    public class AndCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private List<Condition<T>> _conditions;

        public AndCondition(params Condition<T>[] conditions)
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
                if (!conditions[i].IsEligible(skillContext))
                    return false;
            }
            return true;
        }
        // 新增：池化支持
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
