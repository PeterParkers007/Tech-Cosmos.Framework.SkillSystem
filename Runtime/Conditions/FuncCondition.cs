// 函数条件（包装 Func<SkillContext<T>, bool>）
using System;
namespace TechCosmos.SkillSystem.Runtime
{
    public class FuncCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private Func<SkillContext<T>, bool> _func;

        public FuncCondition(Func<SkillContext<T>, bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => _func(skillContext);
    }
}
