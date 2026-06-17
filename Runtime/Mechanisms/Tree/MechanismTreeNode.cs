using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    /// <summary>机制树节点基类。</summary>
    public abstract class MechanismTreeNodeBase
    {
        public abstract IEnumerable<MechanismBase> EnumerateLeafMechanisms();
    }

    [Serializable]
    /// <summary>机制树叶子，持有单个机制实例。</summary>
    public class MechanismTreeLeaf : MechanismTreeNodeBase
    {
        [UnityEngine.SerializeReference]
        public MechanismBase mechanism;

        public override IEnumerable<MechanismBase> EnumerateLeafMechanisms()
        {
            if (mechanism != null) yield return mechanism;
        }
    }

    [Serializable]
    /// <summary>顺序执行子节点的机制树节点。</summary>
    public class MechanismTreeSequence : MechanismTreeNodeBase
    {
        [UnityEngine.SerializeReference]
        public List<MechanismTreeNodeBase> children = new();

        public override IEnumerable<MechanismBase> EnumerateLeafMechanisms()
        {
            if (children == null) yield break;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null) continue;
                foreach (var leaf in children[i].EnumerateLeafMechanisms())
                    yield return leaf;
            }
        }
    }

    [Serializable]
    /// <summary>并行执行子节点的机制树节点（单线程依次触发，语义为同批次）。</summary>
    public class MechanismTreeParallel : MechanismTreeNodeBase
    {
        [UnityEngine.SerializeReference]
        public List<MechanismTreeNodeBase> children = new();

        public override IEnumerable<MechanismBase> EnumerateLeafMechanisms()
        {
            if (children == null) yield break;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null) continue;
                foreach (var leaf in children[i].EnumerateLeafMechanisms())
                    yield return leaf;
            }
        }
    }

    [Serializable]
    /// <summary>引用外部复合机制资产的树节点。</summary>
    public class MechanismTreeRef : MechanismTreeNodeBase
    {
        public CompositeMechanismSO preset;

        public override IEnumerable<MechanismBase> EnumerateLeafMechanisms()
        {
            if (preset?.mechanismTreeRoot == null) yield break;
            foreach (var leaf in preset.mechanismTreeRoot.EnumerateLeafMechanisms())
                yield return leaf;
        }
    }
}
