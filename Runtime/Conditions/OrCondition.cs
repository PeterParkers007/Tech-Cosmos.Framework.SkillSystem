using System.Collections.Generic;
using System.Linq;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 逻辑或组合条件：任一子条件满足时返回 true。
    /// </summary>
    public class OrCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private List<Condition<T>> _conditions;

        public OrCondition(params Condition<T>[] conditions)
        {
            _conditions = conditions.Where(c => c != null).ToList();
        }

        public OrCondition(Condition<T> a, Condition<T> b)
        {
            _conditions = new List<Condition<T>>(2);
            if (a != null) _conditions.Add(a);
            if (b != null) _conditions.Add(b);
        }

        public OrCondition(Condition<T> a, Condition<T> b, Condition<T> c)
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
                if (conditions[i].IsEligible(skillContext, dataLayer))
                    return true;
            }
            return false;
        }

        // 转发成功回调到满足条件的子条件
        public override void OnSkillExecuted(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                var condition = _conditions[i];
                if (condition != null && condition.IsEligible(skillContext, dataLayer))
                    condition.OnSkillExecuted(skillContext, dataLayer);
            }
        }

        public override void OnReset()
        {
            for (int i = 0; i < _conditions.Count; i++)
                _conditions[i]?.OnReset();
        }

        // 转发失败回调到所有子条件
        public override void OnConditionFailed(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                _conditions[i]?.OnConditionFailed(skillContext, dataLayer);
            }
        }

        /// <summary>重新初始化子条件列表。</summary>
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

        /// <summary>清空子条件（用于对象池归还）。</summary>
        public void Clear()
        {
            _conditions.Clear();
        }
    }
}