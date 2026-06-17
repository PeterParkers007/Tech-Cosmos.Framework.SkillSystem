using System;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    [AutoGenerateCondition]
    [ConditionMenu("🏷 标签", DisplayName = "拥有标签", Priority = 5)]
    /// <summary>
    /// 标签条件：检查单位是否拥有指定状态标签。
    /// </summary>
    public class HasTagCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        /// <summary>所需标签名。</summary>
        public string requiredTag = "Stun";
        /// <summary>是否检查目标而非施法者。</summary>
        public bool checkTarget = true;

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {
            var unit = checkTarget ? skillContext.target ?? skillContext.caster : skillContext.caster;
            return unit is IBuffHost host && host.Tags.HasTag(requiredTag);
        }
    }
}
