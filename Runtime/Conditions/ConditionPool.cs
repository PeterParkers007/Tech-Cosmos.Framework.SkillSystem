using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public static class ConditionPool<T> where T : class, IUnit<T>
    {
        private static readonly Stack<AndCondition<T>> _andPool = new();
        private static readonly Stack<OrCondition<T>> _orPool = new();
        private static readonly Stack<NotCondition<T>> _notPool = new();

        // ===== And =====
        public static Condition<T> RentAnd(params Condition<T>[] conditions)
        {
            if (_andPool.TryPop(out var condition))
            {
                condition.Reinitialize(conditions);
                return condition;
            }
            return new AndCondition<T>(conditions);
        }

        public static Condition<T> RentAnd(Condition<T> a, Condition<T> b)
        {
            if (_andPool.TryPop(out var condition))
            {
                condition.Reinitialize(a, b);
                return condition;
            }
            return new AndCondition<T>(a, b);
        }

        public static Condition<T> RentAnd(Condition<T> a, Condition<T> b, Condition<T> c)
        {
            if (_andPool.TryPop(out var condition))
            {
                condition.Reinitialize(a, b, c);
                return condition;
            }
            return new AndCondition<T>(a, b, c);
        }

        // ===== Or =====
        public static Condition<T> RentOr(params Condition<T>[] conditions)
        {
            if (_orPool.TryPop(out var condition))
            {
                condition.Reinitialize(conditions);
                return condition;
            }
            return new OrCondition<T>(conditions);
        }

        public static Condition<T> RentOr(Condition<T> a, Condition<T> b)
        {
            if (_orPool.TryPop(out var condition))
            {
                condition.Reinitialize(a, b);
                return condition;
            }
            return new OrCondition<T>(a, b);
        }

        public static Condition<T> RentOr(Condition<T> a, Condition<T> b, Condition<T> c)
        {
            if (_orPool.TryPop(out var condition))
            {
                condition.Reinitialize(a, b, c);
                return condition;
            }
            return new OrCondition<T>(a, b, c);
        }

        // ===== Not =====
        public static Condition<T> RentNot(Condition<T> condition)
        {
            if (_notPool.TryPop(out var notCondition))
            {
                notCondition.Reinitialize(condition);
                return notCondition;
            }
            return new NotCondition<T>(condition);
        }

        // ===== ¹é»¹ =====
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