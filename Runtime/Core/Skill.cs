namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能运行时实体，聚合各功能层并持有配置与时间轴数据。
    /// </summary>
    public class Skill<T> : ISkill<T> where T : class, IUnit<T>
    {
        /// <summary>基础层，负责触发与激活逻辑。</summary>
        public IBaseLayer<T> BaseLayer { get; }
        /// <summary>条件层，管理释放前置条件。</summary>
        public IConditionLayer<T> ConditionLayer { get; }
        /// <summary>信息层，提供技能名称等元数据。</summary>
        public IInformationLayer<T> InformationLayer { get; }
        /// <summary>机制层，执行具体游戏效果。</summary>
        public IMechanismLayer<T> MechanismLayer { get; }
        /// <summary>数据层，存储技能配置与序列化数据。</summary>
        public IDataLayer<T> DataLayer { get; }
        /// <summary>执行层，协调条件检查与机制调用。</summary>
        public IExecuteLayer<T> ExecuteLayer { get; }
        /// <summary>技能运行配置（优先级、施法时间等）。</summary>
        public SkillProfile Profile { get; }
        /// <summary>技能时间轴数据。</summary>
        public SkillTimelineData Timeline { get; }

        /// <summary>
        /// 构造技能实例，并将自身反向注入各层。
        /// </summary>
        public Skill(
            IBaseLayer<T> baseLayer,
            IInformationLayer<T> infoLayer,
            IConditionLayer<T> conditionLayer,
            IMechanismLayer<T> mechanismLayer,
            IDataLayer<T> dataLayer,
            IExecuteLayer<T> executeLayer,
            SkillProfile profile = null,
            SkillTimelineData timeline = null)
        {
            BaseLayer = baseLayer;
            InformationLayer = infoLayer;
            ConditionLayer = conditionLayer;
            MechanismLayer = mechanismLayer;
            DataLayer = dataLayer;
            ExecuteLayer = executeLayer;
            Profile = profile ?? new SkillProfile();
            Timeline = timeline ?? new SkillTimelineData();

            baseLayer.Skill = this;
            infoLayer.Skill = this;
            conditionLayer.Skill = this;
            mechanismLayer.Skill = this;
            dataLayer.Skill = this;
            executeLayer.Skill = this;

            if (baseLayer is BaseLayer<T> concreteBase)
                concreteBase.SetExecutionPriority(Profile.executionPriority);
        }

        /// <summary>重置条件层状态（如冷却计时）。</summary>
        public void Reset()
        {
            if (ConditionLayer is ConditionLayer<T> cl && cl.Conditions != null)
            {
                foreach (var condition in cl.Conditions)
                    condition?.OnReset();
            }
        }

        /// <summary>技能移除时清理资源并触发回收回调。</summary>
        public void OnRemove()
        {
            if (MechanismLayer is MechanismLayer<T> mechanismLayer)
                mechanismLayer.InvokeSkillBack(this);

            Reset();
            ResourceLayerExtension.RemoveResourceLayer(this);
        }
    }
}
