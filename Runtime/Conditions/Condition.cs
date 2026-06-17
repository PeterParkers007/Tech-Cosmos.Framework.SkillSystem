using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 泛型条件基类
    /// </summary>
    [Serializable]
    public abstract class Condition<T> : ConditionBase where T : class, IUnit<T>
    {
        #region 非泛型接口实现（转发到泛型方法）

        public override bool IsEligible(object context, IDataLayerBase dataLayer)
        {
            if (context is SkillContext<T> ctx && dataLayer is IDataLayer<T> dl)
            {
                return IsEligible(ctx, dl);
            }
            return false;
        }

        public override void OnSkillExecuted(object context, IDataLayerBase dataLayer)
        {
            if (context is SkillContext<T> ctx && dataLayer is IDataLayer<T> dl)
            {
                OnSkillExecuted(ctx, dl);
            }
        }

        public override void OnConditionFailed(object context, IDataLayerBase dataLayer)
        {
            if (context is SkillContext<T> ctx && dataLayer is IDataLayer<T> dl)
            {
                OnConditionFailed(ctx, dl);
            }
        }

        #endregion

        #region 泛型接口（子类重写这些方法）

        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        public abstract bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer);

        /// <summary>
        /// 技能成功执行后的回调
        /// 子类可重写以实现冷却、计数等功能
        /// </summary>
        public virtual void OnSkillExecuted(SkillContext<T> skillContext, IDataLayer<T> dataLayer) { }

        /// <summary>
        /// 条件检查失败时的回调
        /// </summary>
        public virtual void OnConditionFailed(SkillContext<T> skillContext, IDataLayer<T> dataLayer) { }

        /// <summary>
        /// 技能被移除或重置时的回调
        /// </summary>
        public override void OnReset() { }

        #endregion

        #region 运算符重载

        /// <summary>逻辑与组合（使用对象池）。</summary>
        public static Condition<T> operator &(Condition<T> left, Condition<T> right)
            => ConditionPool<T>.RentAnd(left, right);

        /// <summary>逻辑或组合（使用对象池）。</summary>
        public static Condition<T> operator |(Condition<T> left, Condition<T> right)
            => ConditionPool<T>.RentOr(left, right);

        /// <summary>逻辑非（使用对象池）。</summary>
        public static Condition<T> operator !(Condition<T> condition)
            => ConditionPool<T>.RentNot(condition);

        #endregion
    }
}