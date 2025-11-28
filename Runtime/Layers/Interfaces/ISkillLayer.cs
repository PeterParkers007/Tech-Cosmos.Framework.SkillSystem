using System.Collections;
namespace TechCosmos.SkillSystem.Runtime
{
    public interface ISkillLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
    }
}
