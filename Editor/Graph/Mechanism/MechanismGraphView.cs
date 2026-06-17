#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using TechCosmos.SkillSystem.Runtime;
using TechCosmos.SkillSystem.Editor;

namespace TechCosmos.SkillSystem.Editor.Graph
{
    public sealed class MechanismGraphView : TechGraphView
    {
        private CompositeMechanismSO _target;
        private SerializedObject _serializedObject;

        public MechanismGraphView()
        {
            style.backgroundColor = new Color(0.14f, 0.11f, 0.16f);
        }

        public void Bind(CompositeMechanismSO target)
        {
            _target = target;
            _serializedObject?.Dispose();
            _serializedObject = target != null ? new SerializedObject(target) : null;
            Reload();
        }

        public void Reload()
        {
            DeleteElements(graphElements.ToList());
            if (_target == null || _serializedObject == null) return;

            _serializedObject.Update();
            var layout = _target.graphLayout ??= new GraphEditorLayout();
            var rootProp = _serializedObject.FindProperty("mechanismTreeRoot");
            var root = rootProp?.managedReferenceValue as MechanismTreeNodeBase;

            if (root == null)
            {
                var empty = new MechanismTreeGraphNode("root", "根 (空)", layout.GetPosition("root", new Vector2(80, 160)), _serializedObject, rootProp);
                AddElement(empty);
            }
            else
            {
                BuildNodeRecursive(root, rootProp, "root", layout.GetPosition("root", new Vector2(80, 160)), null, 0);
            }

            PersistNodePositions(graphElements.OfType<TechGraphNode>(), layout);
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void BuildNodeRecursive(
            MechanismTreeNodeBase node,
            SerializedProperty nodeProp,
            string pathId,
            Vector2 position,
            Port parentOutput,
            int depth)
        {
            var graphNode = CreateGraphNode(node, pathId, position, nodeProp);
            AddElement(graphNode);

            if (parentOutput != null && graphNode.InputFlow != null)
                Connect(parentOutput, graphNode.InputFlow);

            switch (node)
            {
                case MechanismTreeSequence sequence:
                    BuildCompositeChildren(sequence.children, nodeProp, pathId, graphNode.OutputFlow, depth);
                    break;
                case MechanismTreeParallel parallel:
                    BuildCompositeChildren(parallel.children, nodeProp, pathId, graphNode.OutputFlow, depth);
                    break;
            }
        }

        private void BuildCompositeChildren(
            List<MechanismTreeNodeBase> children,
            SerializedProperty parentProp,
            string parentPath,
            Port parentOutput,
            int depth)
        {
            if (children == null || children.Count == 0) return;
            var childrenProp = parentProp?.FindPropertyRelative("children");
            float y = 120f * depth;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null) continue;
                var childPath = $"{parentPath}/c{i}";
                var childProp = childrenProp?.GetArrayElementAtIndex(i);
                var pos = _target.graphLayout.GetPosition(childPath, new Vector2(360 + depth * 40f, y + i * 150f));
                BuildNodeRecursive(child, childProp, childPath, pos, parentOutput, depth + 1);
            }
        }

        private MechanismTreeGraphNode CreateGraphNode(
            MechanismTreeNodeBase node,
            string pathId,
            Vector2 position,
            SerializedProperty nodeProp)
        {
            string title = node switch
            {
                MechanismTreeSequence => "Sequence",
                MechanismTreeParallel => "Parallel",
                MechanismTreeLeaf => "Leaf",
                MechanismTreeRef => "Ref",
                _ => "Node"
            };
            return new MechanismTreeGraphNode(pathId, title, position, _serializedObject, nodeProp);
        }

        public void SaveLayout()
        {
            if (_target == null) return;
            _target.graphLayout ??= new GraphEditorLayout();
            PersistNodePositions(graphElements.OfType<TechGraphNode>(), _target.graphLayout);
            EditorUtility.SetDirty(_target);
        }

        protected override void BuildGraphContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_target == null) return;
            evt.menu.AppendAction("打开 IMGUI 编辑器", _ => MechanismTreeEditorWindow.OpenComposite(_target));
            evt.menu.AppendAction("刷新图表", _ => Reload());
        }
    }

    internal sealed class MechanismTreeGraphNode : TechGraphNode
    {
        public MechanismTreeGraphNode(string nodeId, string title, Vector2 position, SerializedObject so, SerializedProperty nodeProp)
            : base(nodeId, title, position)
        {
            if (nodeId != "root")
            {
                InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
                inputContainer.Add(InputFlow);
            }

            OutputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, "Out");
            outputContainer.Add(OutputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null || nodeProp == null) return;
                so.Update();
                var node = nodeProp.managedReferenceValue as MechanismTreeNodeBase;
                if (node == null)
                {
                    EditorGUILayout.LabelField("空节点");
                }
                else
                {
                    EditorGUILayout.LabelField("摘要", MechanismTreeEditor.GetSummary(node), EditorStyles.wordWrappedMiniLabel);
                    switch (node)
                    {
                        case MechanismTreeLeaf:
                            var mechProp = nodeProp.FindPropertyRelative("mechanism");
                            if (mechProp != null)
                                EditorGUILayout.PropertyField(mechProp, GUIContent.none, true);
                            break;
                        case MechanismTreeRef:
                            var presetProp = nodeProp.FindPropertyRelative("preset");
                            if (presetProp != null)
                                EditorGUILayout.PropertyField(presetProp, new GUIContent("预设"));
                            break;
                    }
                }
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 240 } };

            extensionContainer.Add(container);

            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    if (so?.targetObject is CompositeMechanismSO preset)
                        MechanismTreeEditorWindow.OpenComposite(preset);
                    evt.StopPropagation();
                }
            });

            style.borderTopColor = new Color(0.75f, 0.45f, 0.35f);
            style.borderBottomColor = new Color(0.75f, 0.45f, 0.35f);
            style.borderLeftColor = new Color(0.75f, 0.45f, 0.35f);
            style.borderRightColor = new Color(0.75f, 0.45f, 0.35f);

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
#endif
