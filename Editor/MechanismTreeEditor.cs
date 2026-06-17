#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>机制树编辑辅助工具。</summary>
    public static class MechanismTreeEditorUtility
    {
        public static IEnumerable<MechanismBase> EnumerateAllMechanisms(SkillDataSO skillDataSO)
        {
            if (skillDataSO == null) yield break;

            if (skillDataSO.useMechanismTree && skillDataSO.mechanismTreeRoot != null)
            {
                foreach (var leaf in skillDataSO.mechanismTreeRoot.EnumerateLeafMechanisms())
                    yield return leaf;
                yield break;
            }

            if (skillDataSO.Mechanisms == null) yield break;
            foreach (var mechanism in skillDataSO.Mechanisms)
            {
                if (mechanism != null) yield return mechanism;
            }
        }
    }

    /// <summary>机制树 Inspector 绘制器，支持 Sequence / Parallel / Ref 组合编辑。</summary>
    public static class MechanismTreeEditor
    {
        private static readonly Dictionary<int, bool> Foldouts = new();

        public static void Draw(SerializedObject serializedObject, SkillDataSO target)
        {
            var useTreeProp = serializedObject.FindProperty("useMechanismTree");
            var rootProp = serializedObject.FindProperty("mechanismTreeRoot");
            var legacyProp = serializedObject.FindProperty("Mechanisms");

            EditorGUILayout.PropertyField(useTreeProp, new GUIContent("使用机制树"));

            if (useTreeProp.boolValue)
            {
                EditorGUILayout.HelpBox("机制树支持 Sequence / Parallel / Ref 组合。根节点为空时不执行任何机制。", MessageType.Info);
                DrawToolbar(rootProp);
                EditorGUILayout.Space(4);
                DrawNodeField(rootProp, "根节点", 0);
            }
            else
            {
                EditorGUILayout.PropertyField(legacyProp, new GUIContent("机制列表 (顺序执行)"), true);
            }
        }

        public static void DrawRoot(SerializedObject serializedObject, SerializedProperty rootProp)
        {
            DrawToolbar(rootProp);
            EditorGUILayout.Space(4);
            DrawNodeField(rootProp, "根节点", 0);
        }

        public static string GetSummary(MechanismTreeNodeBase root)
        {
            if (root == null) return "空 (无机制)";
            return SummarizeNode(root);
        }

        private static string SummarizeNode(MechanismTreeNodeBase node)
        {
            switch (node)
            {
                case MechanismTreeLeaf leaf:
                    if (leaf.mechanism == null) return "Leaf (空)";
                    return $"Leaf ({leaf.mechanism.GetDisplayName()})";
                case MechanismTreeSequence sequence:
                    return $"Sequence ({sequence.children?.Count ?? 0})";
                case MechanismTreeParallel parallel:
                    return $"Parallel ({parallel.children?.Count ?? 0})";
                case MechanismTreeRef reference:
                    var name = reference.preset != null ? reference.preset.displayName : "None";
                    return $"Ref ({name})";
                default:
                    return node.GetType().Name;
            }
        }

        private static void DrawToolbar(SerializedProperty rootProp)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("设为 Sequence", GUILayout.Height(22)))
                SetNode(rootProp, new MechanismTreeSequence());
            if (GUILayout.Button("设为 Parallel", GUILayout.Height(22)))
                SetNode(rootProp, new MechanismTreeParallel());
            if (GUILayout.Button("设为 Leaf", GUILayout.Height(22)))
                SetNode(rootProp, new MechanismTreeLeaf());
            if (GUILayout.Button("设为 Ref", GUILayout.Height(22)))
                SetNode(rootProp, new MechanismTreeRef());
            if (GUILayout.Button("清空", GUILayout.Height(22)))
            {
                rootProp.managedReferenceValue = null;
                rootProp.serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void SetNode(SerializedProperty nodeProp, MechanismTreeNodeBase node)
        {
            nodeProp.managedReferenceValue = node;
            nodeProp.serializedObject.ApplyModifiedProperties();
        }

        private static void DrawNodeField(SerializedProperty nodeProp, string label, int depth)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel = depth;

            if (nodeProp == null)
            {
                EditorGUILayout.LabelField(label, "None");
                EditorGUILayout.EndVertical();
                return;
            }

            var node = nodeProp.managedReferenceValue as MechanismTreeNodeBase;
            int foldKey = nodeProp.propertyPath.GetHashCode();
            if (!Foldouts.ContainsKey(foldKey)) Foldouts[foldKey] = true;

            string typeLabel = node switch
            {
                MechanismTreeSequence => "Sequence",
                MechanismTreeParallel => "Parallel",
                MechanismTreeLeaf => "Leaf",
                MechanismTreeRef => "Ref",
                null => "None",
                _ => node.GetType().Name
            };

            Foldouts[foldKey] = EditorGUILayout.Foldout(Foldouts[foldKey], $"{label} [{typeLabel}]", true);

            if (Foldouts[foldKey])
            {
                DrawNodeActions(nodeProp);

                switch (node)
                {
                    case MechanismTreeLeaf leaf:
                        DrawLeaf(nodeProp, leaf);
                        break;
                    case MechanismTreeSequence sequence:
                        DrawChildren(nodeProp, sequence.children, depth + 1);
                        break;
                    case MechanismTreeParallel parallel:
                        DrawChildren(nodeProp, parallel.children, depth + 1);
                        break;
                    case MechanismTreeRef reference:
                        DrawRef(nodeProp, reference);
                        break;
                }
            }

            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();
        }

        private static void DrawNodeActions(SerializedProperty nodeProp)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("切换类型", GUILayout.Width(80)))
                ShowNodeTypeMenu(nodeProp);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("删除", GUILayout.Width(50)))
                nodeProp.managedReferenceValue = null;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private static void ShowNodeTypeMenu(SerializedProperty nodeProp)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Sequence"), false, () => SetNode(nodeProp, new MechanismTreeSequence()));
            menu.AddItem(new GUIContent("Parallel"), false, () => SetNode(nodeProp, new MechanismTreeParallel()));
            menu.AddItem(new GUIContent("Leaf"), false, () => SetNode(nodeProp, new MechanismTreeLeaf()));
            menu.AddItem(new GUIContent("Ref"), false, () => SetNode(nodeProp, new MechanismTreeRef()));
            menu.ShowAsContext();
        }

        private static void DrawLeaf(SerializedProperty nodeProp, MechanismTreeLeaf leaf)
        {
            if (leaf == null) return;
            var mechanismProp = nodeProp.FindPropertyRelative("mechanism");
            if (mechanismProp != null)
                EditorGUILayout.PropertyField(mechanismProp, new GUIContent("机制"), true);
        }

        private static void DrawRef(SerializedProperty nodeProp, MechanismTreeRef reference)
        {
            if (reference == null) return;
            var presetProp = nodeProp.FindPropertyRelative("preset");
            if (presetProp != null)
                EditorGUILayout.PropertyField(presetProp, new GUIContent("复合机制资产"));
        }

        private static void DrawChildren(SerializedProperty nodeProp, List<MechanismTreeNodeBase> children, int depth)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"子节点 ({children?.Count ?? 0})", EditorStyles.miniLabel);
            if (GUILayout.Button("+ Seq", GUILayout.Width(50)))
                AddChild(nodeProp, new MechanismTreeSequence());
            if (GUILayout.Button("+ Par", GUILayout.Width(45)))
                AddChild(nodeProp, new MechanismTreeParallel());
            if (GUILayout.Button("+ Leaf", GUILayout.Width(50)))
                AddChild(nodeProp, new MechanismTreeLeaf());
            if (GUILayout.Button("+ Ref", GUILayout.Width(45)))
                AddChild(nodeProp, new MechanismTreeRef());
            EditorGUILayout.EndHorizontal();

            var childrenProp = nodeProp.FindPropertyRelative("children");
            if (childrenProp == null) return;

            for (int i = 0; i < childrenProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Child {i}", GUILayout.Width(60));
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    childrenProp.DeleteArrayElementAtIndex(i);
                    nodeProp.serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                var childElement = childrenProp.GetArrayElementAtIndex(i);
                DrawNodeField(childElement, "节点", depth);
            }
        }

        private static void AddChild(SerializedProperty nodeProp, MechanismTreeNodeBase childNode)
        {
            var childrenProp = nodeProp.FindPropertyRelative("children");
            if (childrenProp == null) return;

            int index = childrenProp.arraySize;
            childrenProp.InsertArrayElementAtIndex(index);
            childrenProp.GetArrayElementAtIndex(index).managedReferenceValue = childNode;
            nodeProp.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
