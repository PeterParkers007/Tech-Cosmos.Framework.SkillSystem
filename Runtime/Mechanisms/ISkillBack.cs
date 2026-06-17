using System;
namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能回滚接口：机制可在技能卸载时执行清理逻辑。
    /// </summary>
    public interface ISkillBack<T> where T : class, IUnit<T>
    {
        /// <summary>回滚委托。</summary>
        public Action<ISkill<T>> action { get; set; }
        /// <summary>触发回滚。</summary>
        public void SkillBack(ISkill<T> skill) => action?.Invoke(skill);
    }
}
