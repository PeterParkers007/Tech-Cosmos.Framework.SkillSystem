using System.Collections;
namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能层基础接口：关联所属技能实例。
    /// </summary>
    public interface ISkillLayer<T> where T : class, IUnit<T>
    {
        /// <summary>所属技能。</summary>
        public ISkill<T> Skill { get; set; }
    }
}
