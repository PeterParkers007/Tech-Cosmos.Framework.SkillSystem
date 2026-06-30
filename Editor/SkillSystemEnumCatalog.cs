#if UNITY_EDITOR
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Editor
{
    public enum SkillSystemEnumKind
    {
        TriggerEvent,
        BuffModifyType,
        BuffTag
    }

    public sealed class SkillSystemEnumDefinition
    {
        public SkillSystemEnumKind Kind { get; private set; }
        public string EnumTypeName { get; private set; }
        public string FilePath { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public string[] DefaultValues { get; private set; }
        public bool HasExtensions { get; private set; }

        public SkillSystemEnumDefinition(
            SkillSystemEnumKind kind,
            string enumTypeName,
            string filePath,
            string displayName,
            string description,
            string[] defaultValues,
            bool hasExtensions)
        {
            Kind = kind;
            EnumTypeName = enumTypeName;
            FilePath = filePath;
            DisplayName = displayName;
            Description = description;
            DefaultValues = defaultValues;
            HasExtensions = hasExtensions;
        }
    }

    /// <summary>
    /// 技能 / Buff 系统共用枚举的路径与默认值定义。
    /// </summary>
    public static class SkillSystemEnumCatalog
    {
        public const string TriggerEventPath = "Assets/Generated/TriggerEventType.cs";
        public const string BuffModifyTypePath = "Assets/Generated/BuffModifyType.cs";
        public const string BuffTagPath = "Assets/Generated/BuffTag.cs";

        private static readonly Dictionary<SkillSystemEnumKind, SkillSystemEnumDefinition> Definitions =
            new Dictionary<SkillSystemEnumKind, SkillSystemEnumDefinition>
            {
                {
                    SkillSystemEnumKind.TriggerEvent,
                    new SkillSystemEnumDefinition(
                        SkillSystemEnumKind.TriggerEvent,
                        "TriggerEventType",
                        TriggerEventPath,
                        "TriggerEvent（技能 / Buff 事件）",
                        "技能 TriggerEvents 与 Buff actionName 共用的事件枚举。",
                        new[] { "OnAttack", "OnDamaged", "OnHeal", "OnDeath", "OnKill", "OnHealed" },
                        true)
                },
                {
                    SkillSystemEnumKind.BuffModifyType,
                    new SkillSystemEnumDefinition(
                        SkillSystemEnumKind.BuffModifyType,
                        "BuffModifyType",
                        BuffModifyTypePath,
                        "Buff Modify Type",
                        "Buff 属性修改键（Attack、MoveSpeed 等）。",
                        new[]
                        {
                            "MoveSpeed", "AttackSpeed", "Attack", "IncomingDamage",
                            "IncomingHeal", "MaxHealth", "Armor"
                        },
                        false)
                },
                {
                    SkillSystemEnumKind.BuffTag,
                    new SkillSystemEnumDefinition(
                        SkillSystemEnumKind.BuffTag,
                        "BuffTag",
                        BuffTagPath,
                        "Buff Tag",
                        "Buff 分类标签。",
                        new[]
                        {
                            "Damage", "Heal", "CrowdControl", "Buff", "Debuff", "Physical", "Magic"
                        },
                        false)
                }
            };

        public static SkillSystemEnumDefinition Get(SkillSystemEnumKind kind) => Definitions[kind];

        public static SkillSystemEnumKind[] AllKinds { get; } =
        {
            SkillSystemEnumKind.TriggerEvent,
            SkillSystemEnumKind.BuffModifyType,
            SkillSystemEnumKind.BuffTag
        };
    }
}
#endif
