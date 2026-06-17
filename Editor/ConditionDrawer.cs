#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// 条件（ConditionBase）的 Inspector 属性绘制器，支持选择与切换条件类型。
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionBase), true)]
    public class ConditionDrawer : PropertyDrawer
    {
        private Dictionary<string, List<Type>> typeCache = new();

        /// <summary>绘制条件字段的 Inspector UI。</summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Type ownerType = GetOwnerType(property);

            if (property.managedReferenceValue == null)
            {
                // 绘制标签
                position = EditorGUI.PrefixLabel(position, label);

                // 显示添加按钮
                if (GUI.Button(position, "Select Condition"))
                {
                    ShowAddMenu(property, ownerType);
                }
            }
            else
            {
                var typeName = property.managedReferenceValue is ConditionBase cond
                    ? cond.GetType().Name
                    : property.managedReferenceValue.GetType().Name;

                // 美化类型名
                typeName = ObjectNames.NicifyVariableName(typeName);

                // 折叠区域
                property.isExpanded = EditorGUI.Foldout(
                    new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                    property.isExpanded,
                    $"{label.text}: {typeName}",
                    true
                );

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    // 操作按钮行
                    var buttonRect = new Rect(
                        position.x + EditorGUI.indentLevel * 15,
                        position.y + EditorGUIUtility.singleLineHeight + 2,
                        position.width - EditorGUI.indentLevel * 15,
                        EditorGUIUtility.singleLineHeight
                    );

                    float thirdWidth = buttonRect.width / 2;

                    // 切换按钮
                    if (GUI.Button(new Rect(buttonRect.x, buttonRect.y, thirdWidth - 2, buttonRect.height), "Switch"))
                    {
                        ShowAddMenu(property, ownerType);
                    }

                    // 删除按钮
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                    if (GUI.Button(new Rect(buttonRect.x + thirdWidth, buttonRect.y, thirdWidth - 2, buttonRect.height), "Remove"))
                    {
                        property.managedReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    GUI.backgroundColor = oldColor;

                    // 绘制属性
                    var propRect = new Rect(
                        position.x + EditorGUI.indentLevel * 15,
                        position.y + EditorGUIUtility.singleLineHeight * 2 + 4,
                        position.width - EditorGUI.indentLevel * 15,
                        position.height - EditorGUIUtility.singleLineHeight * 2 - 4
                    );

                    EditorGUI.PropertyField(propRect, property, GUIContent.none, true);

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        /// <summary>根据展开状态计算条件字段的绘制高度。</summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight * 2 + 4;
        }

        private void ShowAddMenu(SerializedProperty property, Type ownerType)
        {
            var menu = new GenericMenu();

            // 添加 None 选项
            menu.AddItem(new GUIContent("None"), false, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            // 获取所有可用的条件类型
            var availableTypes = GetAvailableConditionTypes(ownerType);

            if (availableTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有可用的条件（请先生成或创建具体类型）"));
            }
            else
            {
                // 按分类分组
                var grouped = availableTypes
                    .GroupBy(t => GetMenuCategory(t))
                    .OrderBy(g => g.Key);

                foreach (var group in grouped)
                {
                    foreach (var type in group)
                    {
                        var displayName = GetFriendlyDisplayName(type);
                        var path = $"{group.Key}/{displayName}";

                        if (IsTypeInstantiable(type))
                        {
                            menu.AddItem(new GUIContent(path), false, () =>
                            {
                                try
                                {
                                    property.managedReferenceValue = Activator.CreateInstance(type);
                                    property.serializedObject.ApplyModifiedProperties();
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"创建条件失败: {e.Message}");
                                }
                            });
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent($"{path} (不可用)"));
                        }
                    }
                }
            }

            menu.ShowAsContext();
        }

        private List<Type> GetAvailableConditionTypes(Type ownerType)
        {
            string cacheKey = ownerType?.FullName ?? "unknown";

            if (typeCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var types = new List<Type>();
            HashSet<Type> seenTypes = new HashSet<Type>();

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try { return a.GetExportedTypes(); }
                    catch { return Type.EmptyTypes; }
                });

            foreach (var type in allTypes)
            {
                // 跳过抽象类、接口
                if (!type.IsClass || type.IsAbstract || !type.IsSerializable)
                    continue;

                // 跳过所有泛型类型（Unity 的 SerializeReference 不支持）
                if (type.IsGenericType)
                    continue;

                // 检查是否继承自 ConditionBase
                if (!typeof(ConditionBase).IsAssignableFrom(type))
                    continue;

                // 必须有默认构造函数
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                if (seenTypes.Add(type))
                    types.Add(type);
            }

            // 按优先级排序
            types = types.OrderBy(t =>
            {
                var condAttr = t.GetCustomAttributes(typeof(ConditionMenuAttribute), false)
                    .FirstOrDefault() as ConditionMenuAttribute;
                if (condAttr != null)
                    return condAttr.Priority;

                var mechAttr = t.GetCustomAttributes(typeof(MechanismMenuAttribute), false)
                    .FirstOrDefault() as MechanismMenuAttribute;
                return mechAttr?.Priority ?? 99;
            }).ToList();

            typeCache[cacheKey] = types;
            return types;
        }

        private string GetMenuCategory(Type type)
        {
            var condAttr = type.GetCustomAttributes(typeof(ConditionMenuAttribute), false)
                .FirstOrDefault() as ConditionMenuAttribute;
            if (condAttr != null)
                return condAttr.Category;

            var mechAttr = type.GetCustomAttributes(typeof(MechanismMenuAttribute), false)
                .FirstOrDefault() as MechanismMenuAttribute;
            if (mechAttr != null)
                return mechAttr.Category;

            var name = type.Name.ToLower();

            if (name.Contains("cooldown")) return "⏳ 冷却";
            if (name.Contains("health")) return "❤️ 生命";
            if (name.Contains("mana")) return "💙 魔法";
            if (name.Contains("buff")) return "✨ 增益";
            if (name.Contains("debuff")) return "💀 减益";
            if (name.Contains("func")) return "🔧 自定义";

            return "📦 其他";
        }

        private string GetFriendlyDisplayName(Type type)
        {
            var condAttr = type.GetCustomAttributes(typeof(ConditionMenuAttribute), false)
                .FirstOrDefault() as ConditionMenuAttribute;
            if (condAttr?.DisplayName != null)
                return condAttr.DisplayName;

            var mechAttr = type.GetCustomAttributes(typeof(MechanismMenuAttribute), false)
                .FirstOrDefault() as MechanismMenuAttribute;
            if (mechAttr?.DisplayName != null)
                return mechAttr.DisplayName;

            var name = type.Name;

            // 移除常见前缀和后缀
            name = name.Replace("Condition", "");
            name = name.Replace("Character", "");
            name = name.Replace("Enemy", "");

            // 驼峰命名添加空格
            name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

            return name.Trim();
        }

        private bool IsTypeInstantiable(Type type)
        {
            if (type.IsAbstract) return false;
            if (type.IsInterface) return false;
            if (type.IsGenericTypeDefinition) return false;
            if (type.GetConstructor(Type.EmptyTypes) == null) return false;
            if (!type.IsSerializable) return false;
            return true;
        }

        private Type GetOwnerType(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            return targetObject?.GetType();
        }
    }
}
#endif