// ============================================================
// �ļ���ConfigurableBuff.cs
// ·����TechCosmos.SkillSystem.Runtime/ConfigurableBuff.cs
// ============================================================
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    public class ConfigurableBuff<T> : BaseBuff<T> where T : class
    {
        private BuffDataSO _data;

        public ConfigurableBuff(T target, BuffDataSO data, T caster = null) : base(target, data.duration, data.tags)
        {
            _caster = caster;
            _data = data;
            BuildFromConfig();
        }

        private void BuildFromConfig()
        {
            foreach (var mod in _data.modifiers)
                RegisterModifier(mod.modifyType, mod.BuildModifier<T>());

            foreach (var actionCfg in _data.actions)
            {
                var capturedEffects = actionCfg.effects;
                RegisterAction(actionCfg.actionName, (actionName, caster, target, value, damageType) =>
                {
                    var context = new BuffContext<T>
                    {
                        source = target ?? this.target,
                        deltaTime = Time.deltaTime,
                        currentStacks = CurrentStacks
                    };

                    for (int i = 0; i < capturedEffects.Count; i++)
                        capturedEffects[i]?.ExecuteBase(this.target, context);
                });
            }

            foreach (var executer in _data.effectExecuters)
                AddEffectExecuter(executer);
        }

        public override string BuffName => _data.buffName;
        public override BuffStackPolicy StackPolicy => _data.stackPolicy;
        public override int MaxStacks => _data.maxStacks;
    }
}