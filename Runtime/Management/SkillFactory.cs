using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能实例工厂：将 <see cref="SkillData{T}"/> 或 <see cref="SkillDataSO{T}"/> 组装为完整的 <see cref="ISkill{T}"/>。
    /// </summary>
    public static class SkillFactory<T> where T : class, IUnit<T>
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
        }

        public static ISkill<T> CreateSkill(SkillData<T> data)
        {
            Initialize();

            IBaseLayer<T> baseLayer = data.SkillType == SkillType.Passive
                ? new PassiveBaseLayer<T>(data.TriggerEvents)
                : new ActiveBaseLayer<T>(data.TriggerEvents);

            IConditionLayer<T> conditionLayer = new ConditionLayer<T>(data.Conditions);
            IInformationLayer<T> infoLayer = new InformationLayer<T>(data.SkillName, data.SkillDescription);
            IMechanismLayer<T> mechanismLayer = new MechanismLayer<T>(data.Mechanisms, data.FuncMechanisms);
            IDataLayer<T> dataLayer = new DataLayer<T>(data.Data);
            IExecuteLayer<T> executeLayer = new ExecuteLayer<T>();

            return new Skill<T>(
                baseLayer,
                infoLayer,
                conditionLayer,
                mechanismLayer,
                dataLayer,
                executeLayer,
                data.Profile,
                data.Timeline);
        }

        public static ISkill<T> CreateSkill(SkillDataSO<T> skillDataSO)
        {
            if (skillDataSO == null)
            {
                Debug.LogError("SkillDataSO 为空，无法创建技能");
                return null;
            }

            var skill = CreateSkill(skillDataSO.GetSkillData());
            var resources = skillDataSO.GetResourceDictionary();
            if (resources.Count > 0)
                skill = skill.WithResources(resources);

            return skill;
        }
    }
}
