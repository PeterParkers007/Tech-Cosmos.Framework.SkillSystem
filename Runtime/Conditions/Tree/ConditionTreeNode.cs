using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    /// <summary>条件树节点基类，用于编辑器序列化与叶子枚举。</summary>
    public abstract class ConditionTreeNodeBase
    {
        /// <summary>枚举树中所有叶子条件。</summary>
        public abstract IEnumerable<ConditionBase> EnumerateLeafConditions();
    }

    [Serializable]
    /// <summary>条件树叶子节点，持有单个条件实例。</summary>
    public class ConditionTreeLeaf : ConditionTreeNodeBase
    {
        [UnityEngine.SerializeReference]
        public ConditionBase condition;

        public override IEnumerable<ConditionBase> EnumerateLeafConditions()
        {
            if (condition != null) yield return condition;
        }
    }

    [Serializable]
    /// <summary>条件树 AND 节点。</summary>
    public class ConditionTreeAnd : ConditionTreeNodeBase
    {
        [UnityEngine.SerializeReference]
        public List<ConditionTreeNodeBase> children = new();

        public override IEnumerable<ConditionBase> EnumerateLeafConditions()
        {
            if (children == null) yield break;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null) continue;
                foreach (var leaf in children[i].EnumerateLeafConditions())
                    yield return leaf;
            }
        }
    }

    [Serializable]
    /// <summary>条件树 OR 节点。</summary>
    public class ConditionTreeOr : ConditionTreeNodeBase
    {
        [UnityEngine.SerializeReference]
        public List<ConditionTreeNodeBase> children = new();

        public override IEnumerable<ConditionBase> EnumerateLeafConditions()
        {
            if (children == null) yield break;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null) continue;
                foreach (var leaf in children[i].EnumerateLeafConditions())
                    yield return leaf;
            }
        }
    }

    [Serializable]
    /// <summary>条件树 NOT 节点。</summary>
    public class ConditionTreeNot : ConditionTreeNodeBase
    {
        [UnityEngine.SerializeReference]
        public ConditionTreeNodeBase child;

        public override IEnumerable<ConditionBase> EnumerateLeafConditions()
        {
            if (child != null)
            {
                foreach (var leaf in child.EnumerateLeafConditions())
                    yield return leaf;
            }
        }
    }
}
