using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public static class ConditionPool<T> where T : class, IUnit<T>
    {
        private static readonly Stack<AndCondition<T>> _andPool = new();
        private static readonly Stack<OrCondition<T>> _orPool = new();
        private static readonly Stack<NotCondition<T>> _notPool = new();

        public static Condition<T> RentAnd(params Condition<T>[] conditions)  // 改这里！
        {
            if (_andPool.TryPop(out var condition))
            {
                condition.Reinitialize(conditions);
                return condition;
            }
            return new AndCondition<T>(conditions);
        }

        public static Condition<T> RentOr(params Condition<T>[] conditions)  // 这个对了
        {
            if (_orPool.TryPop(out var condition))
            {
                condition.Reinitialize(conditions);
                return condition;
            }
            return new OrCondition<T>(conditions);
        }

        public static Condition<T> RentNot(Condition<T> condition)  // 这个对了
        {
            if (_notPool.TryPop(out var notCondition))
            {
                notCondition.Reinitialize(condition);
                return notCondition;
            }
            return new NotCondition<T>(condition);
        }

        public static void Return(AndCondition<T> condition)
        {
            condition.Clear();
            _andPool.Push(condition);
        }

        public static void Return(OrCondition<T> condition)
        {
            condition.Clear();
            _orPool.Push(condition);
        }

        public static void Return(NotCondition<T> condition)
        {
            condition.Clear();
            _notPool.Push(condition);
        }
    }
}