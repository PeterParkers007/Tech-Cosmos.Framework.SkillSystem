using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 执行层：技能执行入口，转发至管线并广播生命周期事件。
    /// </summary>
    public class ExecuteLayer<T> : IExecuteLayer<T> where T : class, IUnit<T>
    {
        public ISkill<T> Skill { get; set; }

        /// <summary>本技能开始执行时触发。</summary>
        public event Action<SkillContext<T>> Executing;
        /// <summary>本技能执行成功时触发。</summary>
        public event Action<SkillContext<T>> Executed;
        /// <summary>本技能执行失败时触发。</summary>
        public event Action<SkillContext<T>> Failed;

        /// <summary>任意技能开始执行时触发（全局）。</summary>
        public static event Action<SkillContext<T>> OnAnySkillExecuting;
        /// <summary>任意技能执行成功时触发（全局）。</summary>
        public static event Action<SkillContext<T>> OnAnySkillExecuted;
        /// <summary>任意技能执行失败时触发（全局）。</summary>
        public static event Action<SkillContext<T>> OnAnySkillFailed;

        public void Execute(SkillContext<T> skillContext)
        {
            skillContext = skillContext.skill != null ? skillContext : skillContext.WithSkill(Skill);
            SkillExecutionPipeline.Execute(Skill, skillContext);
        }

        internal void InvokeExecuting(SkillContext<T> context) => Executing?.Invoke(context);

        internal void InvokeExecuted(SkillContext<T> context) => Executed?.Invoke(context);

        internal void InvokeFailed(SkillContext<T> context) => Failed?.Invoke(context);

        internal static void RaiseGlobalExecuting(SkillContext<T> context) => OnAnySkillExecuting?.Invoke(context);

        internal static void RaiseGlobalExecuted(SkillContext<T> context) => OnAnySkillExecuted?.Invoke(context);

        internal static void RaiseGlobalFailed(SkillContext<T> context) => OnAnySkillFailed?.Invoke(context);
    }
}
