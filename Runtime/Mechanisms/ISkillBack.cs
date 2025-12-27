using System;
namespace TechCosmos.SkillSystem.Runtime
{
    public interface ISkillBack<T> where T : class, IUnit<T>
    {
        public Action<ISkill<T>> action { get; set; }
        public void SkillBack(ISkill<T> skill) => action?.Invoke(skill);
    }
}
