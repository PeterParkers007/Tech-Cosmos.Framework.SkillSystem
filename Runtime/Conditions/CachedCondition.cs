namespace TechCosmos.SkillSystem.Runtime
{
    public class CachedCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        private Condition<T> _inner;
        private SkillContext<T> _lastContext;
        private bool _lastResult;
        private bool _hasCache;

        public CachedCondition(Condition<T> inner) => _inner = inner;

        public override bool IsEligible(SkillContext<T> ctx, IDataLayer<T> dataLayer)
        {
            if (_hasCache && ContextEquals(_lastContext, ctx))
                return _lastResult;

            _lastResult = _inner.IsEligible(ctx, dataLayer);
            _lastContext = ctx;
            _hasCache = true;
            return _lastResult;
        }

        public override void OnSkillExecuted(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => _inner?.OnSkillExecuted(skillContext, dataLayer);

        public override void OnConditionFailed(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => _inner?.OnConditionFailed(skillContext, dataLayer);

        public override void OnReset()
        {
            _hasCache = false;
            _lastContext = default;
            _inner?.OnReset();
        }

        private bool ContextEquals(in SkillContext<T> a, in SkillContext<T> b)
            => a.caster == b.caster &&
               a.target == b.target &&
               a.targetPos == b.targetPos;
    }
}