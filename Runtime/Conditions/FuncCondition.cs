using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 将任意判断委托包装为可序列化的条件节点。
    /// </summary>
    public class FuncCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private Func<SkillContext<T>, bool> _func;

        /// <summary>使用判断委托创建函数条件。</summary>
        public FuncCondition(Func<SkillContext<T>, bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        /// <inheritdoc />
        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => _func(skillContext);
    }
}
