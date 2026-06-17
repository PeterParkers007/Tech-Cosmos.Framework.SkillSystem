using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 运行时轻量 Buff：供技能机制按标识、时长与修改器快速施加，无需 ScriptableObject。
    /// </summary>
    public sealed class SimpleBuff<T> : BaseBuff<T> where T : class
    {
        private readonly string _buffName;
        private readonly BuffStackPolicy _stackPolicy;
        private readonly int _maxStacks;

        public SimpleBuff(
            T target,
            string buffName,
            float duration,
            string[] tags,
            StatModifier[] modifiers,
            T caster,
            BuffStackPolicy stackPolicy,
            int maxStacks)
            : base(target, duration, tags)
        {
            _buffName = buffName;
            _caster = caster;
            _stackPolicy = stackPolicy;
            _maxStacks = Math.Max(1, maxStacks);

            if (modifiers == null) return;

            for (int i = 0; i < modifiers.Length; i++)
            {
                var mod = modifiers[i];
                if (string.IsNullOrEmpty(mod.statKey)) continue;

                RegisterModifier(mod.statKey, (baseValue, ctx) =>
                {
                    switch (mod.operation)
                    {
                        case ModifierOperation.Add:
                            return baseValue + mod.value * CurrentStacks;
                        case ModifierOperation.Multiply:
                            return baseValue * mod.value;
                        case ModifierOperation.Override:
                            return mod.value;
                        default:
                            return baseValue;
                    }
                });
            }
        }

        public override string BuffName => _buffName;
        public override BuffStackPolicy StackPolicy => _stackPolicy;
        public override int MaxStacks => _maxStacks;
    }
}
