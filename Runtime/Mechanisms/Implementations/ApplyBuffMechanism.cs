using System;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    [AutoGenerateMechanism]
    [MechanismMenu("✨ 增益", DisplayName = "施加 Buff", Priority = 10)]
    [RequiredData("BuffId", typeof(string), DefaultValue = "Buff", Description = "Buff 标识")]
    [RequiredData("BuffDuration", typeof(float), DefaultValue = "5", Description = "持续时间")]
    /// <summary>
    /// 施加 Buff 机制：向目标或施法者添加增益效果（支持运行时 SimpleBuff 或 BuffDataSO）。
    /// </summary>
    public class ApplyBuffMechanism<T> : Mechanism<T> where T : class, IUnit<T>
    {
        /// <summary>默认 Buff 标识。</summary>
        public string buffId = "Buff";
        /// <summary>可选 Buff 配置资产，设置后优先于 buffId。</summary>
        public BuffDataSO buffData;
        /// <summary>默认持续时间（秒）。</summary>
        public float duration = 5f;
        /// <summary>最大叠加层数。</summary>
        public int maxStacks = 1;
        /// <summary>是否施加到目标而非施法者。</summary>
        public bool applyToTarget = true;
        /// <summary>重复施加时的叠层策略。</summary>
        public ModifierStackPolicy stackPolicy = ModifierStackPolicy.Stack;
        /// <summary>附加状态标签。</summary>
        public string[] tags;
        /// <summary>属性修改器（仅 SimpleBuff 模式生效）。</summary>
        public StatModifier[] modifiers;

        public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
        {
            var unit = applyToTarget ? context.target ?? context.caster : context.caster;
            if (!(unit is IBuffHost<T> host)) return;

            if (buffData != null)
            {
                host.BuffSystem.AddBuff(new ConfigurableBuff<T>(unit, buffData, context.caster));
                return;
            }

            var id = dataLayer.GetValue<string>("BuffId", context);
            if (string.IsNullOrEmpty(id)) id = buffId;

            var buffDuration = dataLayer.GetValue<float>("BuffDuration", context);
            if (buffDuration <= 0f) buffDuration = duration;

            if (stackPolicy == ModifierStackPolicy.Ignore && host.BuffSystem.FindBuffByName(id) != null)
                return;

            var buff = new SimpleBuff<T>(
                unit,
                id,
                buffDuration,
                tags,
                modifiers,
                context.caster,
                BuffStackPolicyMapper.ToGbfPolicy(stackPolicy),
                maxStacks);

            host.BuffSystem.AddBuff(buff);
        }
    }
}
