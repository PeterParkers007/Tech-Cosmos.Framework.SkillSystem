using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    public struct SkillContext<T> where T : class, IUnit<T>
    {
        public T caster;
        public T target;
        public Vector3 targetPos;

        public SkillContext(T caster = null, T target = null, Vector3 targetPos = default)
        {
            this.caster = caster;
            this.target = target;
            this.targetPos = targetPos;
        }

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
}