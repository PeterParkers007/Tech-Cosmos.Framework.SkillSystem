namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class Condition<T> where T : class, IUnit<T>
    {
        public abstract bool IsEligible(SkillContext<T> skillContext);

        // 运算符重载
        public static Condition<T> operator &(Condition<T> left, Condition<T> right)
            => new AndCondition<T>(left, right);

        public static Condition<T> operator |(Condition<T> left, Condition<T> right)
            => new OrCondition<T>(left, right);

        public static Condition<T> operator !(Condition<T> condition)
            => new NotCondition<T>(condition);
    }
}
