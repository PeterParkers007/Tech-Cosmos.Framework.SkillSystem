namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// <see cref="SkillDataSO{T}"/> 的便捷扩展方法。
    /// </summary>
    public static class SkillDataSOExtensions
    {
        /// <summary>
        /// 直接从 SkillDataSO 创建技能（含资源层）。
        /// </summary>
        public static ISkill<T> CreateSkill<T>(this SkillDataSO<T> skillDataSO) where T : class, IUnit<T>
        {
            if (skillDataSO == null)
            {
                UnityEngine.Debug.LogError("SkillDataSO 为空");
                return null;
            }

            return SkillFactory<T>.CreateSkill(skillDataSO);
        }
    }
}
