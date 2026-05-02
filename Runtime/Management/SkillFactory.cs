using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    public static class SkillFactory<T> where T : class, IUnit<T>
    {
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
        }

        public static ISkill<T> CreateSkill(SkillData<T> data)
        {
            IBaseLayer<T> baseLayer = data.SkillType == SkillType.Passive ?
                new PassiveBaseLayer<T>(data.TriggerEvent) : new ActiveBaseLayer<T>(data.TriggerEvent);

            IConditionLayer<T> conditionLayer = new ConditionLayer<T>(data.Conditions);
            IInformationLayer<T> infoLayer = new InformationLayer<T>(data.SkillName, data.SkillDescription);
            IMechanismLayer<T> mechanismLayer = new MechanismLayer<T>(data.Mechanisms,data.FuncMechanisms);
            IDataLayer<T> dataLayer = new DataLayer<T>(data.Data);
            IExecuteLayer<T> executeLayer = new ExecuteLayer<T>();

            return new Skill<T>(baseLayer, infoLayer, conditionLayer, mechanismLayer, dataLayer, executeLayer);
        }
        /// <summary>
        /// 从 SkillDataSO 创建技能
        /// </summary>
        public static ISkill<T> CreateSkill(SkillDataSO<T> skillDataSO)
        {
            if (skillDataSO == null)
            {
                Debug.LogError("SkillDataSO 为空，无法创建技能");
                return null;
            }
            return CreateSkill(skillDataSO.GetSkillData());
        }
    }
}
