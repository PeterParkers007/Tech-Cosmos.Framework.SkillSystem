using System;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Samples
{
    /// <summary>
    /// 演示用伤害机制，从数值层读取 Damage 并对目标造成伤害。
    /// </summary>
    [Serializable]
    [AutoGenerateMechanism(typeof(DemoCharacter))]
    [MechanismMenu("Demo/伤害", DisplayName = "演示伤害", Priority = 0)]
    [RequiredData("Damage", typeof(float), DefaultValue = "10", Description = "伤害值")]
    public class DemoDamageMechanism<T> : Mechanism<T> where T : class, IUnit<T>
    {
        public float baseDamage = 10f;

        /// <summary>执行伤害逻辑，优先使用数值层 Damage，否则使用 baseDamage。</summary>
        public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
        {
            var damage = dataLayer.GetValue<float>("Damage", context);
            if (damage <= 0f) damage = baseDamage;

            var target = context.target;
            if (target is DemoCharacter demo)
            {
                demo.ReceiveDamage(damage);
                return;
            }

            Debug.Log($"[DemoDamageMechanism] 对目标造成 {damage} 点伤害");
        }
    }
}
