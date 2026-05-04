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

        public AndCondition(Condition<T> a, Condition<T> b)
        {
            _conditions = new List<Condition<T>>(2);
            if (a != null) _conditions.Add(a);
            if (b != null) _conditions.Add(b);
        }

        public AndCondition(Condition<T> a, Condition<T> b, Condition<T> c)
        {
            _conditions = new List<Condition<T>>(3);
            if (a != null) _conditions.Add(a);
            if (b != null) _conditions.Add(b);
            if (c != null) _conditions.Add(c);
        }

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {
            if (_conditions.Count == 0) return true;

            var conditions = _conditions;
            int count = conditions.Count;

            for (int i = 0; i < count; i++)
            {
                if (!conditions[i].IsEligible(skillContext, dataLayer))
                    return false;
            }
            return true;
        }

        // 池化支持：重新初始化
        public void Reinitialize(params Condition<T>[] conditions)
        {
            _conditions.Clear();
            _conditions.AddRange(conditions.Where(c => c != null));
        }

        public void Reinitialize(Condition<T> a, Condition<T> b)
        {
            _conditions.Clear();
            if (a != null) _conditions.Add(a);
            if (b != null) _conditions.Add(b);
        }

        public void Reinitialize(Condition<T> a, Condition<T> b, Condition<T> c)
        {
            _conditions.Clear();
            if (a != null) _conditions.Add(a);
            if (b != null) _conditions.Add(b);
            if (c != null) _conditions.Add(c);
        }

        public void Clear()
        {
            _conditions.Clear();
        }
    }
}