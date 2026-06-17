using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    public class BuffEffectExecuterBase
    {
        [SerializeReference]
        public ExecutionModeBase executionMode;

        [SerializeReference]
        public List<BuffEffectBase> effects = new();

        public void Apply(object target, BuffContextBase context)
        {
            if (target == null || executionMode == null) return;
            if (!executionMode.IsEligible()) return;

            executionMode.target = target;
            executionMode.context = context;

            for (int i = 0; i < effects.Count; i++)
                effects[i]?.ExecuteBase(target, context);

            executionMode.MarkExecuted();
        }
    }
}