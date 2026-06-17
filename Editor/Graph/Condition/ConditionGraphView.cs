#if UNITY_EDITOR
using System;
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
    public sealed class ConditionGraphView : TechGraphView
    {
        private CompositeConditionSO _target;
        private SerializedObject _serializedObject;

        public ConditionGraphView()
        {
            style.backgroundColor = new Color(0.11f, 0.13f, 0.16f);
        }

        public void Bind(CompositeConditionSO target)
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
            var rootProp = _serializedObject.FindProperty("conditionTreeRoot");
            var root = rootProp?.managedReferenceValue as ConditionTreeNodeBase;

            if (root == null)
            {
                var empty = new ConditionTreeGraphNode("root", "根 (空)", layout.GetPosition("root", new Vector2(80, 160)), _serializedObject, rootProp);
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
            ConditionTreeNodeBase node,
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
                case ConditionTreeAnd and:
                    BuildCompositeChildren(and.children, nodeProp, pathId, graphNode.OutputFlow, depth, layoutY: 120f);
                    break;
                case ConditionTreeOr orNode:
                    BuildCompositeChildren(orNode.children, nodeProp, pathId, graphNode.OutputFlow, depth, layoutY: 120f);
                    break;
                case ConditionTreeNot not:
                    var childProp = nodeProp?.FindPropertyRelative("child");
                    var child = not.child;
                    if (child != null)
                    {
                        var childPos = _target.graphLayout.GetPosition($"{pathId}/child", new Vector2(position.x + 280, position.y));
                        BuildNodeRecursive(child, childProp, $"{pathId}/child", childPos, graphNode.OutputFlow, depth + 1);
                    }
                    break;
            }
        }

        private void BuildCompositeChildren(
            List<ConditionTreeNodeBase> children,
            SerializedProperty parentProp,
            string parentPath,
            Port parentOutput,
            int depth,
            float layoutY)
        {
            if (children == null || children.Count == 0) return;
            var childrenProp = parentProp?.FindPropertyRelative("children");
            float y = layoutY * depth;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null) continue;
                var childPath = $"{parentPath}/c{i}";
                var childProp = childrenProp?.GetArrayElementAtIndex(i);
                var pos = _target.graphLayout.GetPosition(childPath, new Vector2(360 + depth * 40f, y + i * 140f));
                BuildNodeRecursive(child, childProp, childPath, pos, parentOutput, depth + 1);
            }
        }

        private ConditionTreeGraphNode CreateGraphNode(
            ConditionTreeNodeBase node,
            string pathId,
            Vector2 position,
            SerializedProperty nodeProp)
        {
            string title = node switch
            {
                ConditionTreeAnd => "AND",
                ConditionTreeOr => "OR",
                ConditionTreeNot => "NOT",
                ConditionTreeLeaf => "Leaf",
                ConditionTreeRef => "Ref",
                _ => "Node"
            };
            return new ConditionTreeGraphNode(pathId, title, position, _serializedObject, nodeProp);
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
            evt.menu.AppendAction("打开 IMGUI 编辑器", _ => ConditionTreeEditorWindow.OpenComposite(_target));
            evt.menu.AppendAction("刷新图表", _ => Reload());
        }
    }

    internal sealed class ConditionTreeGraphNode : TechGraphNode
    {
        public ConditionTreeGraphNode(string nodeId, string title, Vector2 position, SerializedObject so, SerializedProperty nodeProp)
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
                var node = nodeProp.managedReferenceValue as ConditionTreeNodeBase;
                if (node == null)
                {
                    EditorGUILayout.LabelField("空节点");
                }
                else
                {
                    EditorGUILayout.LabelField("摘要", ConditionTreeEditor.GetSummary(node), EditorStyles.wordWrappedMiniLabel);
                    switch (node)
                    {
                        case ConditionTreeLeaf:
                            var condProp = nodeProp.FindPropertyRelative("condition");
                            if (condProp != null)
                                EditorGUILayout.PropertyField(condProp, GUIContent.none, true);
                            break;
                        case ConditionTreeRef:
                            var presetProp = nodeProp.FindPropertyRelative("preset");
                            if (presetProp != null)
                                EditorGUILayout.PropertyField(presetProp, new GUIContent("预设"));
                            break;
                    }
                }
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 220 } };

            extensionContainer.Add(container);

            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    if (so?.targetObject is CompositeConditionSO preset)
                        ConditionTreeEditorWindow.OpenComposite(preset);
                    evt.StopPropagation();
                }
            });

            style.borderTopColor = new Color(0.35f, 0.75f, 0.55f);
            style.borderBottomColor = new Color(0.35f, 0.75f, 0.55f);
            style.borderLeftColor = new Color(0.35f, 0.75f, 0.55f);
            style.borderRightColor = new Color(0.35f, 0.75f, 0.55f);

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
#endif
