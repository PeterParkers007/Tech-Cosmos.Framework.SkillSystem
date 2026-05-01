using System;

using TechCosmos.SkillSystem.Runtime;

/// <summary>
/// 泛型条件基类修改
/// </summary>
[Serializable]
public abstract class Condition<T> : ConditionBase where T : class, IUnit<T>
{
    public override bool IsEligible(object context, IDataLayerBase dataLayer)
    {
        if (context is SkillContext<T> ctx && dataLayer is IDataLayer<T> dl)
        {
            return IsEligible(ctx, dl);
        }
        return false;
    }

    public abstract bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer);

    public static Condition<T> operator &(Condition<T> left, Condition<T> right)
        => ConditionPool<T>.RentAnd(left, right);

    public static Condition<T> operator |(Condition<T> left, Condition<T> right)
        => ConditionPool<T>.RentOr(left, right);

    public static Condition<T> operator !(Condition<T> condition)
        => ConditionPool<T>.RentNot(condition);
}