using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能执行上下文，携带施法者、目标及执行元数据。
    /// </summary>
    public struct SkillContext<T> where T : class, IUnit<T>
    {
        /// <summary>施法者。</summary>
        public T caster;
        /// <summary>技能目标单位。</summary>
        public T target;
        /// <summary>目标世界坐标（无单位目标时使用）。</summary>
        public Vector3 targetPos;
        /// <summary>本次执行的元数据（时钟、随机源、黑板等）。</summary>
        public SkillExecutionMeta meta;
        /// <summary>当前正在执行的技能实例。</summary>
        public ISkill<T> skill;

        /// <summary>创建技能上下文。</summary>
        public SkillContext(T caster = null, T target = null, Vector3 targetPos = default)
        {
            this.caster = caster;
            this.target = target;
            this.targetPos = targetPos;
            meta = default;
            skill = null;
        }

        /// <summary>隐式转换为非泛型上下文，用于跨类型传递。</summary>
        public static implicit operator SkillContextBase(SkillContext<T> ctx)
        {
            return new SkillContextBase
            {
                Caster = ctx.caster,
                Target = ctx.target,
                TargetPos = ctx.targetPos
            };
        }
    }

    /// <summary>
    /// <see cref="SkillContext{T}"/> 的链式扩展方法。
    /// </summary>
    public static class SkillContextExtensions
    {
        /// <summary>附加当前执行的技能实例。</summary>
        public static SkillContext<T> WithSkill<T>(this SkillContext<T> ctx, ISkill<T> executingSkill)
            where T : class, IUnit<T>
        {
            ctx.skill = executingSkill;
            return ctx;
        }

        /// <summary>附加执行元数据。</summary>
        public static SkillContext<T> WithMeta<T>(this SkillContext<T> ctx, SkillExecutionMeta executionMeta)
            where T : class, IUnit<T>
        {
            ctx.meta = executionMeta;
            return ctx;
        }
    }
}
