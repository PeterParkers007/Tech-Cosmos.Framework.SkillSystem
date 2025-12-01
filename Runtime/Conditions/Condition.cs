namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class Condition<T> where T : class, IUnit<T>
    {
        public abstract bool IsEligible(SkillContext<T> skillContext);

        public static Condition<T> operator &(Condition<T> left, Condition<T> right)
            => ConditionPool<T>.RentAnd(left, right);

        public static Condition<T> operator |(Condition<T> left, Condition<T> right)
            => ConditionPool<T>.RentOr(left, right);

        public static Condition<T> operator !(Condition<T> condition)
            => ConditionPool<T>.RentNot(condition);
    }
}