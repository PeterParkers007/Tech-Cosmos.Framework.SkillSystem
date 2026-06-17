#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// 条件树编辑辅助工具，用于枚举技能中所有叶子条件。
    /// </summary>
    public static class ConditionTreeEditorUtility
    {
        /// <summary>枚举 SkillDataSO 中所有叶子条件（含条件树与旧列表）。</summary>
        public static IEnumerable<ConditionBase> EnumerateAllConditions(SkillDataSO skillDataSO)
        {
            if (skillDataSO == null) yield break;

            if (skillDataSO.useConditionTree && skillDataSO.conditionTreeRoot != null)
            {
                foreach (var leaf in skillDataSO.conditionTreeRoot.EnumerateLeafConditions())
                    yield return leaf;
                yield break;
            }

            if (skillDataSO.Conditions == null) yield break;
            foreach (var condition in skillDataSO.Conditions)
            {
                if (condition != null) yield return condition;
            }
        }
    }

    /// <summary>
    /// 条件树的 Inspector 绘制器，支持 AND / OR / NOT / Ref 组合编辑。
    /// </summary>
    public static class ConditionTreeEditor
    {
        private static readonly Dictionary<int, bool> Foldouts = new();

        /// <summary>在技能编辑器中绘制条件树或旧版条件列表。</summary>
        public static void Draw(SerializedObject serializedObject, SkillDataSO target)
        {
            var useTreeProp = serializedObject.FindProperty("useConditionTree");
            var rootProp = serializedObject.FindProperty("conditionTreeRoot");
            var legacyProp = serializedObject.FindProperty("Conditions");

            EditorGUILayout.PropertyField(useTreeProp, new GUIContent("使用条件树"));

            if (useTreeProp.boolValue)
            {
                EditorGUILayout.HelpBox("条件树支持 AND / OR / NOT / Ref 组合。根节点为空时将视为无条件通过。", MessageType.Info);
                DrawRoot(serializedObject, rootProp, showUseTreeToggle: false);
            }
            else
            {
                EditorGUILayout.PropertyField(legacyProp, new GUIContent("条件列表 (AND)"), true);
            }
        }

        /// <summary>绘制条件树根（可用于技能 SO 或复合条件资产）。</summary>
        public static void DrawRoot(SerializedObject serializedObject, SerializedProperty rootProp, bool showUseTreeToggle = false)
        {
            DrawToolbar(rootProp);
            EditorGUILayout.Space(4);
            DrawNodeField(rootProp, "根节点", 0);
        }

        /// <summary>紧凑摘要视图，用于 Graph 节点内预览。</summary>
        public static void DrawCompact(SerializedObject serializedObject, SkillDataSO target)
        {
            serializedObject.Update();
            var useTreeProp = serializedObject.FindProperty("useConditionTree");
            if (!useTreeProp.boolValue)
            {
                EditorGUILayout.LabelField("模式", "平铺列表 (AND)");
                return;
            }

            var root = target?.conditionTreeRoot;
            EditorGUILayout.LabelField("摘要", GetSummary(root), EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开条件树编辑器", GUILayout.Height(22)))
                ConditionTreeEditorWindow.OpenSkill(target);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("双击节点也可打开编辑器", EditorStyles.miniLabel);
        }

        public static string GetSummary(ConditionTreeNodeBase root)
        {
            if (root == null) return "空 (无条件限制)";
            return SummarizeNode(root);
        }

        private static string SummarizeNode(ConditionTreeNodeBase node)
        {
            switch (node)
            {
                case ConditionTreeLeaf leaf:
                    if (leaf.condition == null) return "Leaf (空)";
                    return $"Leaf ({leaf.condition.GetType().Name})";
                case ConditionTreeAnd and:
                    return $"AND ({and.children?.Count ?? 0})";
                case ConditionTreeOr or:
                    return $"OR ({or.children?.Count ?? 0})";
                case ConditionTreeNot:
                    return "NOT";
                case ConditionTreeRef reference:
                    var name = reference.preset != null ? reference.preset.displayName : "None";
                    return $"Ref ({name})";
                default:
                    return node.GetType().Name;
            }
        }

        private static void DrawToolbar(SerializedProperty rootProp)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("设为 AND", GUILayout.Height(22)))
                SetNode(rootProp, new ConditionTreeAnd());
            if (GUILayout.Button("设为 OR", GUILayout.Height(22)))
                SetNode(rootProp, new ConditionTreeOr());
            if (GUILayout.Button("设为 NOT", GUILayout.Height(22)))
                SetNode(rootProp, new ConditionTreeNot());
            if (GUILayout.Button("设为 Leaf", GUILayout.Height(22)))
                SetNode(rootProp, new ConditionTreeLeaf());
            if (GUILayout.Button("设为 Ref", GUILayout.Height(22)))
                SetNode(rootProp, new ConditionTreeRef());
            if (GUILayout.Button("清空", GUILayout.Height(22)))
            {
                rootProp.managedReferenceValue = null;
                rootProp.serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void SetNode(SerializedProperty nodeProp, ConditionTreeNodeBase node)
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

            var node = nodeProp.managedReferenceValue as ConditionTreeNodeBase;
            int foldKey = nodeProp.propertyPath.GetHashCode();
            if (!Foldouts.ContainsKey(foldKey)) Foldouts[foldKey] = true;

            string typeLabel = node switch
            {
                ConditionTreeAnd => "AND",
                ConditionTreeOr => "OR",
                ConditionTreeNot => "NOT",
                ConditionTreeLeaf => "Leaf",
                ConditionTreeRef => "Ref",
                null => "None",
                _ => node.GetType().Name
            };

            Foldouts[foldKey] = EditorGUILayout.Foldout(Foldouts[foldKey], $"{label} [{typeLabel}]", true);

            if (Foldouts[foldKey])
            {
                DrawNodeActions(nodeProp);

                switch (node)
                {
                    case ConditionTreeLeaf leaf:
                        DrawLeaf(nodeProp, leaf);
                        break;
                    case ConditionTreeAnd and:
                        DrawChildren(nodeProp, and.children, depth + 1);
                        break;
                    case ConditionTreeOr or:
                        DrawChildren(nodeProp, or.children, depth + 1);
                        break;
                    case ConditionTreeNot not:
                        DrawNotChild(nodeProp, not, depth + 1);
                        break;
                    case ConditionTreeRef reference:
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
            menu.AddItem(new GUIContent("AND"), false, () => SetNode(nodeProp, new ConditionTreeAnd()));
            menu.AddItem(new GUIContent("OR"), false, () => SetNode(nodeProp, new ConditionTreeOr()));
            menu.AddItem(new GUIContent("NOT"), false, () => SetNode(nodeProp, new ConditionTreeNot()));
            menu.AddItem(new GUIContent("Leaf"), false, () => SetNode(nodeProp, new ConditionTreeLeaf()));
            menu.AddItem(new GUIContent("Ref"), false, () => SetNode(nodeProp, new ConditionTreeRef()));
            menu.ShowAsContext();
        }

        private static void DrawLeaf(SerializedProperty nodeProp, ConditionTreeLeaf leaf)
        {
            if (leaf == null) return;
            var conditionProp = nodeProp.FindPropertyRelative("condition");
            if (conditionProp != null)
                EditorGUILayout.PropertyField(conditionProp, new GUIContent("条件"), true);
        }

        private static void DrawRef(SerializedProperty nodeProp, ConditionTreeRef reference)
        {
            if (reference == null) return;
            var presetProp = nodeProp.FindPropertyRelative("preset");
            if (presetProp != null)
                EditorGUILayout.PropertyField(presetProp, new GUIContent("复合条件资产"));
        }

        private static void DrawNotChild(SerializedProperty nodeProp, ConditionTreeNot not, int depth)
        {
            EnsureNotChild(not, nodeProp);
            var childProp = nodeProp.FindPropertyRelative("child");
            DrawNodeField(childProp, "子节点", depth);
        }

        private static void DrawChildren(SerializedProperty nodeProp, List<ConditionTreeNodeBase> children, int depth)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"子节点 ({children?.Count ?? 0})", EditorStyles.miniLabel);
            if (GUILayout.Button("+ AND", GUILayout.Width(60)))
                AddChild(nodeProp, new ConditionTreeAnd());
            if (GUILayout.Button("+ OR", GUILayout.Width(50)))
                AddChild(nodeProp, new ConditionTreeOr());
            if (GUILayout.Button("+ NOT", GUILayout.Width(55)))
                AddChild(nodeProp, new ConditionTreeNot());
            if (GUILayout.Button("+ Leaf", GUILayout.Width(55)))
                AddChild(nodeProp, new ConditionTreeLeaf());
            if (GUILayout.Button("+ Ref", GUILayout.Width(50)))
                AddChild(nodeProp, new ConditionTreeRef());
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

        private static void AddChild(SerializedProperty nodeProp, ConditionTreeNodeBase childNode)
        {
            var childrenProp = nodeProp.FindPropertyRelative("children");
            if (childrenProp == null) return;

            int index = childrenProp.arraySize;
            childrenProp.InsertArrayElementAtIndex(index);
            childrenProp.GetArrayElementAtIndex(index).managedReferenceValue = childNode;
            nodeProp.serializedObject.ApplyModifiedProperties();
        }

        private static void EnsureNotChild(ConditionTreeNot not, SerializedProperty nodeProp)
        {
            if (not.child != null) return;
            not.child = new ConditionTreeLeaf();
            nodeProp.managedReferenceValue = not;
            nodeProp.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
