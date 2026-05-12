namespace TechCosmos.SkillSystem.Runtime
{
    public class Skill<T> : ISkill<T> where T : class, IUnit<T>
    {
        public IBaseLayer<T> BaseLayer { get; }
        public IConditionLayer<T> ConditionLayer { get; }
        public IInformationLayer<T> InformationLayer { get; }
        public IMechanismLayer<T> MechanismLayer { get; }
        public IDataLayer<T> DataLayer { get; }
        public IExecuteLayer<T> ExecuteLayer { get; }

        public Skill(
            IBaseLayer<T> baseLayer,
            IInformationLayer<T> infoLayer,
            IConditionLayer<T> conditionLayer,
            IMechanismLayer<T> mechanismLayer,
            IDataLayer<T> dataLayer,
            IExecuteLayer<T> executeLayer)
        {
            BaseLayer = baseLayer;
            InformationLayer = infoLayer;
            ConditionLayer = conditionLayer;
            MechanismLayer = mechanismLayer;
            DataLayer = dataLayer;
            ExecuteLayer = executeLayer;

            baseLayer.Skill = this;
            infoLayer.Skill = this;
            conditionLayer.Skill = this;
            mechanismLayer.Skill = this;
            dataLayer.Skill = this;
            executeLayer.Skill = this;
        }

        /// <summary>重置技能所有状态（冷却、缓存等）</summary>
        public void Reset()
        {
            if (ConditionLayer is ConditionLayer<T> cl && cl.Conditions != null)
            {
                foreach (var condition in cl.Conditions)
                    condition?.OnReset();
            }
        }
    }
}