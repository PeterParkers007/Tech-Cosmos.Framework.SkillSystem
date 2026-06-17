#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor.Graph
{
    public sealed class BuffGraphView : TechGraphView
    {
        private BuffDataSO _target;
        private SerializedObject _serializedObject;

        public BuffGraphView()
        {
            style.backgroundColor = new Color(0.11f, 0.13f, 0.12f);
        }

        public void Bind(BuffDataSO target)
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

            var root = new BuffRootGraphNode(layout.GetPosition("root", new Vector2(60, 220)), _serializedObject);
            AddElement(root);

            var modProp = _serializedObject.FindProperty("modifiers");
            float y = 60f;
            for (int i = 0; i < modProp.arraySize; i++)
            {
                var nodeId = $"modifier_{i}";
                var node = new BuffModifierGraphNode(nodeId, layout.GetPosition(nodeId, new Vector2(420, y)), _serializedObject, i);
                AddElement(node);
                Connect(root.OutputFlow, node.InputFlow);
                y += 150f;
            }

            var execProp = _serializedObject.FindProperty("effectExecuters");
            y = 60f;
            for (int i = 0; i < execProp.arraySize; i++)
            {
                var nodeId = $"executer_{i}";
                var node = new BuffExecuterGraphNode(nodeId, layout.GetPosition(nodeId, new Vector2(780, y)), _serializedObject, i);
                AddElement(node);
                Connect(root.OutputFlow, node.InputFlow);
                y += 170f;
            }

            var actionProp = _serializedObject.FindProperty("actions");
            y = 280f;
            for (int i = 0; i < actionProp.arraySize; i++)
            {
                var nodeId = $"action_{i}";
                var node = new BuffActionGraphNode(nodeId, layout.GetPosition(nodeId, new Vector2(420, y)), _serializedObject, i);
                AddElement(node);
                Connect(root.OutputFlow, node.InputFlow);
                y += 140f;
            }

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

            evt.menu.AppendAction("添加属性修改器", _ => AddArrayItem("modifiers", evt.localMousePosition, "modifier"));
            evt.menu.AppendAction("添加效果执行器", _ => AddArrayItem("effectExecuters", evt.localMousePosition, "executer"));
            evt.menu.AppendAction("添加 Action 响应", _ => AddArrayItem("actions", evt.localMousePosition, "action"));
            evt.menu.AppendAction("刷新图表", _ => Reload());
        }

        private void AddArrayItem(string propertyName, Vector2 mousePosition, string idPrefix)
        {
            if (_serializedObject == null) return;
            _serializedObject.Update();
            var prop = _serializedObject.FindProperty(propertyName);
            prop.InsertArrayElementAtIndex(prop.arraySize);
            var index = prop.arraySize - 1;
            _serializedObject.ApplyModifiedProperties();
            _target.graphLayout.SetPosition($"{idPrefix}_{index}", contentViewContainer.WorldToLocal(mousePosition));
            EditorUtility.SetDirty(_target);
            Reload();
        }
    }

    internal sealed class BuffRootGraphNode : TechGraphNode
    {
        public BuffRootGraphNode(Vector2 position, SerializedObject so) : base("root", "Buff 根节点", position)
        {
            OutputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, "Effects");
            outputContainer.Add(OutputFlow);

            style.borderTopColor = new Color(0.35f, 0.75f, 0.55f);
            style.borderBottomColor = new Color(0.35f, 0.75f, 0.55f);
            style.borderLeftColor = new Color(0.35f, 0.75f, 0.55f);
            style.borderRightColor = new Color(0.35f, 0.75f, 0.55f);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("buffName"), new GUIContent("名称"));
                EditorGUILayout.PropertyField(so.FindProperty("duration"), new GUIContent("时长"));
                EditorGUILayout.PropertyField(so.FindProperty("stackPolicy"), new GUIContent("叠层策略"));
                EditorGUILayout.PropertyField(so.FindProperty("maxStacks"), new GUIContent("最大层数"));
                EditorGUILayout.PropertyField(so.FindProperty("tags"), new GUIContent("标签"), true);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 240 } };
            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();
        }
    }

    internal sealed class BuffModifierGraphNode : TechGraphNode
    {
        public BuffModifierGraphNode(string nodeId, Vector2 position, SerializedObject so, int index)
            : base(nodeId, $"修改器 #{index + 1}", position)
        {
            InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
            inputContainer.Add(InputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                var prop = so.FindProperty("modifiers");
                if (index < 0 || index >= prop.arraySize) return;
                EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(index), GUIContent.none, true);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 260 } };
            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();
        }
    }

    internal sealed class BuffExecuterGraphNode : TechGraphNode
    {
        public BuffExecuterGraphNode(string nodeId, Vector2 position, SerializedObject so, int index)
            : base(nodeId, $"执行器 #{index + 1}", position)
        {
            InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
            inputContainer.Add(InputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                var prop = so.FindProperty("effectExecuters");
                if (index < 0 || index >= prop.arraySize) return;
                EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(index), GUIContent.none, true);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 280 } };
            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();
        }
    }

    internal sealed class BuffActionGraphNode : TechGraphNode
    {
        public BuffActionGraphNode(string nodeId, Vector2 position, SerializedObject so, int index)
            : base(nodeId, $"Action #{index + 1}", position)
        {
            InputFlow = CreateFlowPort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "In");
            inputContainer.Add(InputFlow);

            var container = new IMGUIContainer(() =>
            {
                if (so == null) return;
                so.Update();
                var prop = so.FindProperty("actions");
                if (index < 0 || index >= prop.arraySize) return;
                EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(index), GUIContent.none, true);
                so.ApplyModifiedProperties();
            }) { style = { paddingLeft = 8, paddingRight = 8, paddingBottom = 8, minWidth = 260 } };
            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
#endif
