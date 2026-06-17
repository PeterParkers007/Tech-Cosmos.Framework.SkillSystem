using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 可复用的复合机制资产，可在多个技能中通过引用节点复用。
    /// </summary>
    [CreateAssetMenu(fileName = "CompositeMechanism", menuName = "Tech-Cosmos/SkillSystem/Composite Mechanism")]
    public class CompositeMechanismSO : ScriptableObject
    {
        [Tooltip("显示名称，用于编辑器与 Graph 标题。")]
        public string displayName;

        [SerializeReference]
        public MechanismTreeNodeBase mechanismTreeRoot = new MechanismTreeSequence();

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
