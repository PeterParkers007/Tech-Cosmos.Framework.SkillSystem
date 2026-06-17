using System;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    [AutoGenerateCondition]
    [ConditionMenu("✨ 增益", DisplayName = "拥有 Buff", Priority = 6)]
    [RequiredData("BuffId", typeof(string), DefaultValue = "Buff", Description = "Buff 标识")]
    /// <summary>
    /// Buff 条件：检查单位是否拥有指定 Buff 且层数达标。
    /// </summary>
    public class HasBuffCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        /// <summary>默认 Buff 标识（数据层未配置时使用）。</summary>
        public string buffId = "Buff";
        /// <summary>所需最小层数。</summary>
        public int minStacks = 1;
        /// <summary>是否检查目标而非施法者。</summary>
        public bool checkTarget = true;

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {
            var id = dataLayer.GetValue<string>("BuffId", skillContext);
            if (string.IsNullOrEmpty(id)) id = buffId;

            var unit = checkTarget ? skillContext.target ?? skillContext.caster : skillContext.caster;
            if (!(unit is IBuffHost<T> host)) return false;

            var buff = host.BuffSystem.FindBuffByName(id);
            return buff != null && buff.CurrentStacks >= minStacks;
        }
    }
}
