namespace TechCosmos.SkillSystem.Runtime
{
    public static class SkillManager<T> where T : IUnit<T>
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
            IMechanismLayer<T> mechanismLayer = new MechanismLayer<T>(data.Mechanisms);
            IDataLayer<T> dataLayer = new DataLayer<T>();
            IExecuteLayer<T> executeLayer = new ExecuteLayer<T>();

            return new Skill<T>(baseLayer, infoLayer, conditionLayer, mechanismLayer, dataLayer, executeLayer);
        }
    }
}
