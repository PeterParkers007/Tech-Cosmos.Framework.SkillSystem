using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    /// <summary>引用外部复合条件资产的树节点。</summary>
    public class ConditionTreeRef : ConditionTreeNodeBase
    {
        public CompositeConditionSO preset;

        public override IEnumerable<ConditionBase> EnumerateLeafConditions()
        {
            if (preset?.conditionTreeRoot == null) yield break;
            foreach (var leaf in preset.conditionTreeRoot.EnumerateLeafConditions())
                yield return leaf;
        }
    }
}
