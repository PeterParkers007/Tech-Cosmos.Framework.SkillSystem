using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 可复用的复合条件资产，可在多个技能/Buff 中通过引用节点复用。
    /// </summary>
    [CreateAssetMenu(fileName = "CompositeCondition", menuName = "Tech-Cosmos/SkillSystem/Composite Condition")]
    public class CompositeConditionSO : ScriptableObject
    {
        [Tooltip("显示名称，用于编辑器与 Graph 标题。")]
        public string displayName;

        [SerializeReference]
        public ConditionTreeNodeBase conditionTreeRoot = new ConditionTreeAnd();

        [Header("Graph 编辑器布局")]
        public GraphEditorLayout graphLayout = new();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;
        }
#endif
    }
}
