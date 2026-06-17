using System;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Samples
{
    /// <summary>
    /// 演示用治疗机制，对施法者恢复固定生命值。
    /// </summary>
    [Serializable]
    [AutoGenerateMechanism(typeof(DemoCharacter))]
    [MechanismMenu("Demo/治疗", DisplayName = "演示治疗", Priority = 1)]
    public class DemoHealMechanism<T> : Mechanism<T> where T : class, IUnit<T>
    {
        public float healAmount = 5f;

        /// <summary>对施法者执行治疗。</summary>
        public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
        {
            if (context.caster is DemoCharacter demo)
                demo.Heal(healAmount);
        }
    }
}
