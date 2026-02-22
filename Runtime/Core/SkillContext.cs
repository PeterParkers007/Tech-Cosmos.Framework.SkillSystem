using UnityEngine;
// 泛型技能上下文
namespace TechCosmos.SkillSystem.Runtime
{
    public struct SkillContext<T> where T : class,IUnit<T>
    {
        public T caster;
        public T target;

        public Vector3 targetPos;
        public SkillContext(T caster = null, T target = null, Vector3 targetPos = default(Vector3))
        {
            this.caster = caster;
            this.target = target;
            this.targetPos = targetPos;
        }
    }
}
