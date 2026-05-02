namespace TechCosmos.SkillSystem.Runtime
{
    public static class SkillDataSOExtensions
    {
        /// <summary>
        /// 直接从 SkillDataSO 创建技能
        /// </summary>
        public static ISkill<T> CreateSkill<T>(this SkillDataSO<T> skillDataSO) where T : class, IUnit<T>
        {
            if (skillDataSO == null)
            {
                UnityEngine.Debug.LogError("SkillDataSO 为空");
                return null;
            }
            return SkillFactory<T>.CreateSkill(skillDataSO.GetSkillData());
        }
    }
}