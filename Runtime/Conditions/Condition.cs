namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class Condition<T> where T : IUnit<T>
    {
        public abstract bool IsEligible(SkillContext<T> skillContext);

        // ‘ÀÀ„∑˚÷ÿ‘ÿ
        public static Condition<T> operator &(Condition<T> left, Condition<T> right)
            => new AndCondition<T>(left, right);

        public static Condition<T> operator |(Condition<T> left, Condition<T> right)
            => new OrCondition<T>(left, right);

        public static Condition<T> operator !(Condition<T> condition)
            => new NotCondition<T>(condition);
    }
}
