// 泛型技能上下文
namespace TechCosmos.SkillSystem.Runtime
{
    public class SkillContext<T> where T : IUnit<T>
    {
        public T caster;
        public T target;

        public Vector3 targetPos;
    }
}
