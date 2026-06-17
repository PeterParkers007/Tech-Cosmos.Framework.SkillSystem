// ============================================================
// �ļ���BuffModifyContext.cs
// ·����TechCosmos.SkillSystem.Runtime/BuffModifyContext.cs
// ============================================================
namespace TechCosmos.SkillSystem.Runtime
{
    public class BuffModifyContext<T> where T : class
    {
        public T target;
        public T caster;
    }
}