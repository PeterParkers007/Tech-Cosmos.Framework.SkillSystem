namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能执行中间件：在管线各阶段插入自定义逻辑。
    /// </summary>
    public interface ISkillMiddleware
    {
        /// <summary>执行顺序，数值越小越先执行。</summary>
        int Order { get; }

        /// <summary>执行前拦截，返回 false 可取消本次执行。</summary>
        bool OnBeforeExecute<T>(ISkill<T> skill, ref SkillContext<T> context) where T : class, IUnit<T>;

        /// <summary>条件检查失败时回调。</summary>
        void OnConditionFailed<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T>;

        /// <summary>执行完成后回调。</summary>
        void OnAfterExecute<T>(ISkill<T> skill, SkillContext<T> context, SkillExecutionResult result)
            where T : class, IUnit<T>;
    }

    /// <summary>
    /// 中间件基类：提供默认空实现，便于按需重写。
    /// </summary>
    public abstract class SkillMiddlewareBase : ISkillMiddleware
    {
        public virtual int Order => 0;

        public virtual bool OnBeforeExecute<T>(ISkill<T> skill, ref SkillContext<T> context) where T : class, IUnit<T>
            => true;

        public virtual void OnConditionFailed<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T> { }

        public virtual void OnAfterExecute<T>(ISkill<T> skill, SkillContext<T> context, SkillExecutionResult result)
            where T : class, IUnit<T> { }
    }
}
