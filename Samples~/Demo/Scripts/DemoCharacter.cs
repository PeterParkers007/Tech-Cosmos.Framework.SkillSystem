using System;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Samples
{
    /// <summary>
    /// 演示用角色 Unit，提供攻击、生命与受击/治疗逻辑。
    /// </summary>
    [GenerateSkillDataSO(MenuName = "Tech-Cosmos/Skill/Demo Character")]
    public class DemoCharacter : UnitBase<DemoCharacter>
    {
        [SkillDataField(Category = "战斗", DisplayName = "攻击力")]
        public float Attack = 20f;

        [SkillDataField(Category = "战斗", DisplayName = "当前生命")]
        public float Health = 100f;

        /// <summary>返回演示场景支持的触发事件列表。</summary>
        public override string[] GetSupportedEvents()
            => new[] { "OnAttack", "OnHit", "OnTimelineHit" };

        /// <summary>扣除生命值并输出日志。</summary>
        public void ReceiveDamage(float amount)
        {
            Health = Mathf.Max(0f, Health - amount);
            Debug.Log($"[DemoCharacter] {name} 受到 {amount} 伤害，剩余 HP={Health:F0}");
        }

        /// <summary>恢复生命值并输出日志。</summary>
        public void Heal(float amount)
        {
            Health += amount;
            Debug.Log($"[DemoCharacter] {name} 恢复 {amount} HP，当前 HP={Health:F0}");
        }
    }
}
