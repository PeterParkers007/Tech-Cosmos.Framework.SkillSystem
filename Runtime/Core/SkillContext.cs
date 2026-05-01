using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能上下文结构体（保持值类型，高性能）
    /// </summary>
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
    }
}