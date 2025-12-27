// CachedCondition.cs
using TechCosmos.SkillSystem.Runtime;
namespace TechCosmos.SkillSystem.Runtime
{
    public class CachedCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private Condition<T> _inner;
        private SkillContext<T> _lastContext;
        private bool _lastResult;
        private bool _hasCache;

        public CachedCondition(Condition<T> inner) => _inner = inner;

        public override bool IsEligible(SkillContext<T> ctx)
        {
            if (_hasCache && ContextEquals(_lastContext,ctx))
                return _lastResult;

            _lastResult = _inner.IsEligible(ctx);
            _lastContext = ctx;
            _hasCache = true;
            return _lastResult;
        }

        private bool ContextEquals(in SkillContext<T> a, in SkillContext<T> b)
            => a.caster == b.caster &&      // 用 == 而不是 ReferenceEquals
                a.target == b.target &&      // 尊重类型自己的相等性定义
                a.targetPos == b.targetPos;
    }
}
