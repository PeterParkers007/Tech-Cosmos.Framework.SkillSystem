using System;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    [AutoGenerateCondition]
    [ConditionMenu("⏳ 冷却", DisplayName = "冷却时间", Priority = 0)]
    /// <summary>
    /// 冷却条件：技能成功执行后进入冷却，冷却结束前不可再次释放。
    /// </summary>
    public class CooldownCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        /// <summary>冷却时长（秒）。</summary>
        public float cooldown = 1f;
        private float _nextAvailableTime;

        public CooldownCondition() { }

        public CooldownCondition(float cooldown) => this.cooldown = cooldown;

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => ResolveClock(skillContext).Time >= _nextAvailableTime;

        public override void OnSkillExecuted(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => _nextAvailableTime = ResolveClock(skillContext).Time + cooldown;

        public override void OnReset() => _nextAvailableTime = 0f;

        /// <summary>立即开始冷却（不依赖技能执行回调）。</summary>
        public void StartCooldown(SkillContext<T> skillContext = default)
            => _nextAvailableTime = ResolveClock(skillContext).Time + cooldown;

        /// <summary>获取剩余冷却时间（秒）。</summary>
        public float GetRemainingCooldown(SkillContext<T> skillContext = default)
            => Mathf.Max(0f, _nextAvailableTime - ResolveClock(skillContext).Time);

        private static ISkillClock ResolveClock(SkillContext<T> context)
            => context.meta.ResolveClock();
    }
}
