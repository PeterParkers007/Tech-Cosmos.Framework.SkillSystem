using System;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    [AutoGenerateMechanism]
    [MechanismMenu("✨ 增益", DisplayName = "移除 Buff", Priority = 11)]
    [RequiredData("BuffId", typeof(string), DefaultValue = "Buff", Description = "Buff 标识")]
    /// <summary>
    /// 移除 Buff 机制：从目标或施法者移除指定增益。
    /// </summary>
    public class RemoveBuffMechanism<T> : Mechanism<T> where T : class, IUnit<T>
    {
        /// <summary>要移除的 Buff 标识。</summary>
        public string buffId = "Buff";
        /// <summary>是否从目标而非施法者移除。</summary>
        public bool removeFromTarget = true;
        /// <summary>按标签驱散（设置后忽略 buffId）。</summary>
        public string[] dispelTags;

        public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
        {
            var unit = removeFromTarget ? context.target ?? context.caster : context.caster;
            if (!(unit is IBuffHost<T> host)) return;

            if (dispelTags != null && dispelTags.Length > 0)
            {
                host.BuffSystem.DispelByTags(dispelTags);
                return;
            }

            var id = dataLayer.GetValue<string>("BuffId", context);
            if (string.IsNullOrEmpty(id)) id = buffId;

            host.BuffSystem.RemoveBuffsByName(id);
        }
    }
}
