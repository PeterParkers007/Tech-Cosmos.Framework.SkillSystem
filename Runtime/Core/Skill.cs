using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

            // 设置反向引用
            baseLayer.Skill = this;
            infoLayer.Skill = this;
            conditionLayer.Skill = this;
            mechanismLayer.Skill = this;
            dataLayer.Skill = this;
            executeLayer.Skill = this;
        }
    }
}
