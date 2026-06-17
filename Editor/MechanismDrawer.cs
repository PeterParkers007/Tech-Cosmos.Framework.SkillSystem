#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TechCosmos.SkillSystem.Runtime;

/// <summary>
/// 机制（MechanismBase）的 Inspector 属性绘制器，支持选择、切换、复制与删除。
/// </summary>
[CustomPropertyDrawer(typeof(MechanismBase), true)]
public class MechanismDrawer : PropertyDrawer
{
    private Dictionary<string, List<Type>> typeCache = new();

    /// <summary>绘制机制字段的 Inspector UI。</summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Type ownerType = GetOwnerType(property);

        if (property.managedReferenceValue == null)
        {
            // 绘制标签
            position = EditorGUI.PrefixLabel(position, label);

            // 显示添加按钮
            if (GUI.Button(position, "Select Mechanism"))
            {
                ShowAddMenu(property, ownerType);
            }
        }
        else
        {
            var typeName = property.managedReferenceValue is MechanismBase mech
                ? mech.GetDisplayName()
                : property.managedReferenceValue.GetType().Name;

            // 折叠区域
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                $"{label.text}: {typeName}",
                true
            );

            // 展开后显示属性和操作按钮
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

                var thirdWidth = buttonRect.width / 3;

                // 切换按钮
                if (GUI.Button(new Rect(buttonRect.x, buttonRect.y, thirdWidth - 2, buttonRect.height), "Switch"))
                {
                    ShowAddMenu(property, ownerType);
                }

                // 复制按钮
                if (GUI.Button(new Rect(buttonRect.x + thirdWidth, buttonRect.y, thirdWidth - 2, buttonRect.height), "Copy"))
                {
                    var json = JsonUtility.ToJson(property.managedReferenceValue);
                    GUIUtility.systemCopyBuffer = json;
                    Debug.Log($"已复制机制数据: {typeName}");
                }

                // 删除按钮
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUI.Button(new Rect(buttonRect.x + thirdWidth * 2, buttonRect.y, thirdWidth - 2, buttonRect.height), "Remove"))
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

    /// <summary>根据展开状态计算机制字段的绘制高度。</summary>
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

        // 获取所有可用的机制类型
        var availableTypes = GetAvailableMechanismTypes(ownerType);

        if (availableTypes.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("没有可用的机制（请先生成或创建具体类型）"));
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

                    // 验证类型是否可实例化
                    if (IsTypeInstantiable(type))
                    {
                        menu.AddItem(new GUIContent(path), false, () =>
                        {
                            try
                            {
                                var instance = CreateMechanismInstance(type, ownerType);
                                property.managedReferenceValue = instance;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"创建机制失败: {e.Message}");
                            }
                        });
                    }
                    else
                    {
                        // 不可实例化的类型显示为禁用
                        menu.AddDisabledItem(new GUIContent($"{path} (不可用)"));
                    }
                }
            }
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// 检查类型是否可实例化
    /// </summary>
    private bool IsTypeInstantiable(Type type)
    {
        // 不能是抽象类
        if (type.IsAbstract) return false;

        // 不能是接口
        if (type.IsInterface) return false;

        // 不能是泛型类型定义（如 DamageMechanism<>）
        if (type.IsGenericTypeDefinition) return false;

        // 必须有默认构造函数
        if (type.GetConstructor(Type.EmptyTypes) == null) return false;

        // 必须可以序列化
        if (!type.IsSerializable) return false;

        return true;
    }

    /// <summary>
    /// 获取菜单分类
    /// </summary>
    private string GetMenuCategory(Type type)
    {
        var name = type.Name.ToLower();

        if (name.Contains("damage")) return "⚔ 伤害";
        if (name.Contains("heal")) return "💚 治疗";
        if (name.Contains("buff")) return "⬆ 增益";
        if (name.Contains("debuff")) return "⬇ 减益";
        if (name.Contains("move")) return "🏃 移动";
        if (name.Contains("spawn")) return "✨ 生成";
        if (name.Contains("log")) return "📝 日志";

        return "📦 其他";
    }

    /// <summary>
    /// 获取友好的显示名称
    /// </summary>
    private string GetFriendlyDisplayName(Type type)
    {
        // 去除 Unit 前缀和 Mechanism 后缀，让名字更简洁
        var name = GetTypeDisplayName(type);

        // 移除泛型参数（因为已经在类型中体现了）
        if (name.Contains("<"))
            name = name.Substring(0, name.IndexOf('<'));

        // 移除常见的后缀
        name = name.Replace("Mechanism", "");

        // 添加空格让驼峰命名更可读
        name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

        return name.Trim();
    }

    private List<Type> GetAvailableMechanismTypes(Type ownerType)
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

            // 关键：跳过所有泛型类型（包括封闭泛型）
            // 因为 Unity 的 [SerializeReference] 不支持泛型类型
            if (type.IsGenericType)
                continue;

            // 检查是否继承自 MechanismBase
            if (!typeof(MechanismBase).IsAssignableFrom(type))
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
            var attr = t.GetCustomAttributes(typeof(MechanismMenuAttribute), false)
                .FirstOrDefault() as MechanismMenuAttribute;
            return attr?.Priority ?? 99;
        }).ToList();

        typeCache[cacheKey] = types;
        return types;
    }

    /// <summary>
    /// 检查封闭泛型类型是否与宿主类型兼容
    /// </summary>
    private bool IsValidClosedGeneric(Type type, Type ownerType)
    {
        if (!type.IsGenericType || type.IsGenericTypeDefinition)
            return false;

        if (ownerType == null)
            return true;

        var unitType = InferUnitType(ownerType);
        if (unitType == null)
            return true;

        // 检查泛型参数是否匹配
        var genericArgs = type.GetGenericArguments();
        foreach (var arg in genericArgs)
        {
            if (!arg.IsAssignableFrom(unitType) && !unitType.IsAssignableFrom(arg))
                return false;
        }

        return true;
    }

    private Type InferUnitType(Type ownerType)
    {
        if (ownerType == null) return null;

        // 从宿主类型推断 IUnit<T> 的 T
        var unitInterface = ownerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnit<>));

        if (unitInterface != null)
            return unitInterface.GetGenericArguments()[0];

        // 递归检查基类
        var baseType = ownerType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(SkillData<>))
                return baseType.GetGenericArguments()[0];

            baseType = baseType.BaseType;
        }

        return null;
    }

    private Type GetOwnerType(SerializedProperty property)
    {
        var targetObject = property.serializedObject.targetObject;
        return targetObject?.GetType();
    }

    private object CreateMechanismInstance(Type type, Type ownerType)
    {
        // 如果是泛型类型定义，需要构造封闭泛型
        if (type.IsGenericTypeDefinition)
        {
            var unitType = InferUnitType(ownerType);
            if (unitType != null)
            {
                type = type.MakeGenericType(unitType);
            }
        }

        return Activator.CreateInstance(type);
    }

    private string GetTypeDisplayName(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name;
            if (name.Contains("`"))
                name = name.Substring(0, name.IndexOf('`'));

            var args = type.GetGenericArguments();
            return $"{name}<{string.Join(",", args.Select(a => a.Name))}>";
        }
        return type.Name;
    }
}
#endif