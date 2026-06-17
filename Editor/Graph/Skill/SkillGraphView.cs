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
    public sealed class SkillGraphView : TechGraphView
    {
        private SkillDataSO _target;
        private SerializedObject _serializedObject;

        public SkillGraphView()
        {
            style.backgroundColor = new Color(0.12f, 0.12f, 0.13f);
        }

        public void Bind(SkillDataSO target)
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

            var trigger = new SkillTriggerGraphNode(layout.GetPosition("trigger", new Vector2(40, 200)), _serializedObject);
            var condition = new SkillConditionGraphNode(layout.GetPosition("condition", new Vector2(320, 200)), _serializedObject);
            AddElement(trigger);
            AddElement(condition);
            Connect(trigger.OutputFlow, condition.InputFlow);

            var useMechTree = _serializedObject.FindProperty("useMechanismTree")?.boolValue ?? false;

            if (useMechTree)
            {
                var mechTree = new SkillMechanismTreeGraphNode(layout.GetPosition("mechanism_tree", new Vector2(620, 200)), _serializedObject);
                AddElement(mechTree);
                Connect(condition.OutputFlow, mechTree.InputFlow);
            }
            else
            {
                var mechProp = _serializedObject.FindProperty("Mechanisms");
                float mechY = 80f;
                for (int i = 0; i < mechProp.arraySize; i++)
                {
                    var nodeId = $"mechanism_{i}";
                    var node = new SkillMechanismGraphNode(
                        nodeId,
                        layout.GetPosition(nodeId, new Vector2(620, mechY)),
                        _serializedObject,
                        i);
                    AddElement(node);
                    Connect(condition.OutputFlow, node.InputFlow);
                    mechY += 180f;
                }
            }

            var timeline = new SkillTimelineGraphNode(layout.GetPosition("timeline", new Vector2(320, 420)), _serializedObject);
            AddElement(timeline);
            Connect(trigger.OutputFlow, timeline.InputFlow);

            PersistNodePositions(graphElements.OfType<TechGraphNode>(), layout);
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

            evt.menu.AppendAction("添加机制节点 (平铺模式)", _ => AddMechanismNode(evt.localMousePosition));
            evt.menu.AppendAction("切换为机制树模式", _ =>
            {
                ToggleMechanismTreeMode(true);
                Reload();
            });
            evt.menu.AppendAction("切换为平铺机制列表", _ =>
            {
                ToggleMechanismTreeMode(false);
                Reload();
            });
            evt.menu.AppendAction("打开条件树编辑器", _ => ConditionTreeEditorWindow.OpenSkill(_target));
            evt.menu.AppendAction("刷新图表", _ => Reload());
        }

        private void ToggleMechanismTreeMode(bool useTree)
        {
            if (_serializedObject == null) return;
            _serializedObject.Update();
            var useTreeProp = _serializedObject.FindProperty("useMechanismTree");
            useTreeProp.boolValue = useTree;
            if (useTree)
            {
                var rootProp = _serializedObject.FindProperty("mechanismTreeRoot");
                if (rootProp.managedReferenceValue == null)
                    rootProp.managedReferenceValue = new MechanismTreeSequence();
            }
            _serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_target);
        }

        private void AddMechanismNode(Vector2 mousePosition)
        {
            if (_serializedObject == null) return;
            _serializedObject.Update();

            var useTree = _serializedObject.FindProperty("useMechanismTree")?.boolValue ?? false;
            if (useTree)
            {
                MechanismTreeEditorWindow.OpenSkill(_target);
                return;
            }

            var mechProp = _serializedObject.FindProperty("Mechanisms");
            mechProp.InsertArrayElementAtIndex(mechProp.arraySize);
            var element = mechProp.GetArrayElementAtIndex(mechProp.arraySize - 1);
            element.managedReferenceValue = null;
            _serializedObject.ApplyModifiedProperties();

            var index = mechProp.arraySize - 1;
            var nodeId = $"mechanism_{index}";
            _target.graphLayout.SetPosition(nodeId, contentViewContainer.WorldToLocal(mousePosition));
            EditorUtility.SetDirty(_target);
            Reload();
        }
    }

    internal sealed class SkillTriggerGraphNode : TechGraphNode
    {
        public SkillTriggerGraphNode(Vector2 position, SerializedObject so) : base("trigger", "触发器", position)
        {
            InputFlow = null;
            OutputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, "Flow");
            outputContainer.Add(OutputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("TriggerEvents"), new GUIContent("触发事件"));
                EditorGUILayout.PropertyField(so.FindProperty("SkillType"), new GUIContent("技能类型"));
                EditorGUILayout.PropertyField(so.FindProperty("SkillName"), new GUIContent("技能名称"));
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8 } };
            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();
        }
    }

    internal sealed class SkillConditionGraphNode : TechGraphNode
    {
        public SkillConditionGraphNode(Vector2 position, SerializedObject so) : base("condition", "条件门", position)
        {
            InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
            OutputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, "Pass");
            inputContainer.Add(InputFlow);
            outputContainer.Add(OutputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                ConditionTreeEditor.DrawCompact(so, so.targetObject as SkillDataSO);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 260 } };
            extensionContainer.Add(container);

            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    if (so?.targetObject is SkillDataSO skill)
                        ConditionTreeEditorWindow.OpenSkill(skill);
                    evt.StopPropagation();
                }
            });

            RefreshExpandedState();
            RefreshPorts();
        }
    }

    internal sealed class SkillMechanismTreeGraphNode : TechGraphNode
    {
        public SkillMechanismTreeGraphNode(Vector2 position, SerializedObject so) : base("mechanism_tree", "机制树", position)
        {
            InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
            OutputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, "Out");
            inputContainer.Add(InputFlow);
            outputContainer.Add(OutputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                var target = so.targetObject as SkillDataSO;
                EditorGUILayout.LabelField("摘要", MechanismTreeEditor.GetSummary(target?.mechanismTreeRoot), EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(4);
                if (GUILayout.Button("打开机制树编辑器", GUILayout.Height(22)))
                    MechanismTreeEditorWindow.OpenSkill(target);
                EditorGUILayout.LabelField("双击节点也可打开编辑器", EditorStyles.miniLabel);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 280 } };
            extensionContainer.Add(container);

            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    if (so?.targetObject is SkillDataSO skill)
                        MechanismTreeEditorWindow.OpenSkill(skill);
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

    internal sealed class SkillMechanismGraphNode : TechGraphNode
    {
        public int MechanismIndex { get; }

        public SkillMechanismGraphNode(string nodeId, Vector2 position, SerializedObject so, int index)
            : base(nodeId, $"机制 #{index + 1}", position)
        {
            MechanismIndex = index;
            InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
            OutputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, "Out");
            inputContainer.Add(InputFlow);
            outputContainer.Add(OutputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                var mechProp = so.FindProperty("Mechanisms");
                if (index < 0 || index >= mechProp.arraySize) return;
                EditorGUILayout.PropertyField(mechProp.GetArrayElementAtIndex(index), GUIContent.none, true);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 280 } };
            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();
        }
    }

    internal sealed class SkillTimelineGraphNode : TechGraphNode
    {
        public SkillTimelineGraphNode(Vector2 position, SerializedObject so) : base("timeline", "时间轴", position)
        {
            InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
            inputContainer.Add(InputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("Timeline"), new GUIContent("Timeline"), true);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 260 } };
            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
#endif
