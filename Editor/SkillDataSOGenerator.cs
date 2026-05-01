#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    public static class SkillDataSOGenerator
    {
        private const string GENERATED_FOLDER = "Assets/Generated/SkillDataSO";
        private const string GENERATED_NAMESPACE = "TechCosmos.SkillSystem.Runtime.Generated";

        // 基础类型白名单
        private static readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>
        {
            typeof(int), typeof(float), typeof(double), typeof(long),
            typeof(string), typeof(bool), typeof(char), typeof(byte),
            typeof(Vector2), typeof(Vector3), typeof(Vector4),
            typeof(Vector2Int), typeof(Vector3Int),
            typeof(Color), typeof(Color32),
            typeof(Quaternion), typeof(Rect), typeof(RectInt),
            typeof(Bounds), typeof(BoundsInt),
            typeof(LayerMask), typeof(AnimationCurve),
            typeof(Gradient), typeof(GameObject), typeof(Transform)
        };

        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Generate All SkillDataSO")]
        public static void GenerateAllSkillDataSO()
        {
            if (!Directory.Exists(GENERATED_FOLDER))
                Directory.CreateDirectory(GENERATED_FOLDER);

            // 清理旧文件
            foreach (var file in Directory.GetFiles(GENERATED_FOLDER, "*.g.cs"))
                File.Delete(file);

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToList();

            int generatedCount = 0;

            foreach (var assembly in allAssemblies)
            {
                try
                {
                    var types = assembly.GetExportedTypes()
                        .Where(t => t.IsClass && !t.IsAbstract)
                        .Where(t => t.GetCustomAttribute<GenerateSkillDataSOAttribute>() != null)
                        .Where(t => t.GetInterfaces().Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnit<>)));

                    foreach (var type in types)
                    {
                        GenerateSkillDataSOForType(type);
                        generatedCount++;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"扫描程序集失败: {e.Message}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"✅ 成功生成 {generatedCount} 个 SkillDataSO 类到 {GENERATED_FOLDER}");
        }

        private static void GenerateSkillDataSOForType(Type unitType)
        {
            var attr = unitType.GetCustomAttribute<GenerateSkillDataSOAttribute>();
            var unitTypeName = unitType.Name;
            var className = attr?.FileName ?? $"{unitTypeName}SkillDataSO";
            var menuName = attr?.MenuName ?? $"Tech-Cosmos/Skill/{unitTypeName}";

            // 收集所有标记字段
            var markedFields = unitType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<SkillDataFieldAttribute>() != null)
                .ToList();

            // 收集 DataEntry 标记
            var dataEntries = new List<(string key, Type valueType, string defaultValue, string description)>();
            foreach (var field in unitType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attrs = field.GetCustomAttributes<SkillDataEntryAttribute>();
                foreach (var entryAttr in attrs)
                {
                    dataEntries.Add((entryAttr.Key, entryAttr.ValueType, entryAttr.DefaultValue, entryAttr.Description));
                }
            }

            var code = GenerateClassCode(unitTypeName, className, menuName, markedFields, dataEntries);
            var filePath = Path.Combine(GENERATED_FOLDER, $"{className}.g.cs");
            File.WriteAllText(filePath, code, Encoding.UTF8);
        }

        private static string GenerateClassCode(
            string unitTypeName,
            string className,
            string menuName,
            List<FieldInfo> markedFields,
            List<(string key, Type valueType, string defaultValue, string description)> dataEntries)
        {
            var sb = new StringBuilder();
            var fieldGroups = new Dictionary<string, List<GeneratedProperty>>();

            // 分析每个字段，生成属性列表
            foreach (var field in markedFields)
            {
                var attr = field.GetCustomAttribute<SkillDataFieldAttribute>();
                var category = attr?.Category ?? "基础属性";

                if (!fieldGroups.ContainsKey(category))
                    fieldGroups[category] = new List<GeneratedProperty>();

                var properties = AnalyzeField(field, attr);
                fieldGroups[category].AddRange(properties);
            }

            // 生成头部
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            sb.AppendLine($"// 源类型: {unitTypeName}");
            sb.AppendLine("// 请勿手动修改此文件");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace {GENERATED_NAMESPACE}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {unitTypeName} 的技能数据配置");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"New {unitTypeName} Skill\", menuName = \"{menuName}\")]");
            sb.AppendLine($"    public class {className} : TechCosmos.SkillSystem.Runtime.SkillDataSO<{unitTypeName}>");
            sb.AppendLine("    {");

            // 初始化方法
            sb.AppendLine("        #region 初始化");
            sb.AppendLine();
            sb.AppendLine("        private bool _initialized = false;");
            sb.AppendLine();
            sb.AppendLine("        void OnEnable()");
            sb.AppendLine("        {");
            sb.AppendLine("            InitializeDefaults();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private void InitializeDefaults()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (_initialized) return;");
            sb.AppendLine("            _initialized = true;");
            sb.AppendLine();

            // 生成初始化代码
            foreach (var group in fieldGroups)
            {
                foreach (var prop in group.Value)
                {
                    sb.AppendLine($"            // {group.Key}: {prop.DisplayName}");
                    sb.AppendLine($"            if (!GetData().ContainsKey(\"{prop.DataKey}\"))");
                    sb.AppendLine($"                SetValue(\"{prop.DataKey}\", {prop.DefaultValue});");
                    sb.AppendLine();
                }
            }

            foreach (var entry in dataEntries)
            {
                sb.AppendLine($"            // {entry.description ?? entry.key}");
                sb.AppendLine($"            if (!GetData().ContainsKey(\"{entry.key}\"))");
                sb.AppendLine($"                SetValue(\"{entry.key}\", {ConvertToDefaultValue(entry.defaultValue, entry.valueType)});");
                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine("        #endregion");
            sb.AppendLine();

            // 生成属性
            sb.AppendLine("        #region 自动生成的属性");
            sb.AppendLine();

            foreach (var group in fieldGroups)
            {
                sb.AppendLine($"        // ===== {group.Key} =====");
                sb.AppendLine();

                foreach (var prop in group.Value)
                {
                    GeneratePropertyCode(sb, prop);
                }
            }

            sb.AppendLine("        #endregion");

            // 类结束
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 分析字段，返回生成的属性列表
        /// </summary>
        private static List<GeneratedProperty> AnalyzeField(FieldInfo field, SkillDataFieldAttribute attr)
        {
            var properties = new List<GeneratedProperty>();
            var fieldType = field.FieldType;
            var fieldName = field.Name;
            var category = attr?.Category ?? "基础属性";

            // 判断是否为可拆分类型
            if (ShouldFlatten(fieldType))
            {
                // 拆分结构体/类的字段
                foreach (var subField in fieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (subField.IsInitOnly || subField.IsLiteral) continue; // 跳过只读和常量

                    var subAttr = subField.GetCustomAttribute<SkillDataFieldAttribute>();
                    var prop = new GeneratedProperty
                    {
                        DataKey = $"{fieldName}.{subField.Name}",
                        PropertyName = $"{char.ToUpper(fieldName[0])}{fieldName.Substring(1)}{char.ToUpper(subField.Name[0])}{subField.Name.Substring(1)}",
                        DisplayName = subAttr?.DisplayName ?? ObjectNames.NicifyVariableName(subField.Name),
                        Category = subAttr?.Category ?? category,
                        PropertyType = GetTypeName(subField.FieldType),
                        GetterCode = GenerateGetterCode(subField.FieldType, $"{fieldName}.{subField.Name}"),
                        DefaultValue = GetDefaultValueForType(subField.FieldType,
                            subField.GetValue(Activator.CreateInstance(fieldType)))
                    };
                    properties.Add(prop);
                }
            }
            else
            {
                // 基础类型，直接生成属性
                var prop = new GeneratedProperty
                {
                    DataKey = fieldName,
                    PropertyName = $"{char.ToUpper(fieldName[0])}{fieldName.Substring(1)}",
                    DisplayName = attr?.DisplayName ?? ObjectNames.NicifyVariableName(fieldName),
                    Category = category,
                    PropertyType = GetTypeName(fieldType),
                    GetterCode = $"GetValue<{GetTypeName(fieldType)}>(\"{fieldName}\")",
                    DefaultValue = GetDefaultValueForType(fieldType,
                        field.GetValue(Activator.CreateInstance(field.DeclaringType)))
                };
                properties.Add(prop);
            }

            return properties;
        }

        /// <summary>
        /// 判断是否应该拆分为多个字段
        /// </summary>
        private static bool ShouldFlatten(Type type)
        {
            // 基础类型不拆分
            if (PrimitiveTypes.Contains(type)) return false;
            if (type.IsEnum) return false;
            if (type.IsArray) return false;
            if (type.IsGenericType) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;

            // 自定义结构体/类拆分
            if (type.IsSerializable && !type.IsAbstract)
            {
                // 检查是否有太多字段（超过5个不拆分，作为整体处理）
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields.Length > 5) return false;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 生成属性代码
        /// </summary>
        private static void GeneratePropertyCode(StringBuilder sb, GeneratedProperty prop)
        {
            sb.AppendLine($"        [Header(\"{prop.Category}\")]");
            sb.AppendLine($"        [Tooltip(\"{prop.DisplayName}\")]");
            sb.AppendLine($"        public {prop.PropertyType} {prop.PropertyName}");
            sb.AppendLine("        {");
            sb.AppendLine($"            get => {prop.GetterCode};");
            sb.AppendLine($"            set => SetValue(\"{prop.DataKey}\", value);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// 生成 Getter 代码
        /// </summary>
        private static string GenerateGetterCode(Type type, string key)
        {
            if (type.IsEnum)
                return $"({GetTypeName(type)})GetValue<int>(\"{key}\")";

            return $"GetValue<{GetTypeName(type)}>(\"{key}\")";
        }

        /// <summary>
        /// 获取类型的默认值字符串
        /// </summary>
        private static string GetDefaultValueForType(Type type, object instance)
        {
            if (type == typeof(float)) return $"{instance ?? 0f}f";
            if (type == typeof(int)) return (instance ?? 0).ToString();
            if (type == typeof(string)) return $"\"{instance ?? ""}\"";
            if (type == typeof(bool)) return (instance ?? false).ToString().ToLower();
            if (type == typeof(Vector2)) return $"new Vector2({((Vector2)(instance ?? Vector2.zero)).x}f, {((Vector2)(instance ?? Vector2.zero)).y}f)";
            if (type == typeof(Vector3)) return $"new Vector3({((Vector3)(instance ?? Vector3.zero)).x}f, {((Vector3)(instance ?? Vector3.zero)).y}f, {((Vector3)(instance ?? Vector3.zero)).z}f)";
            if (type == typeof(Vector2Int)) return $"new Vector2Int({((Vector2Int)(instance ?? Vector2Int.zero)).x}, {((Vector2Int)(instance ?? Vector2Int.zero)).y})";
            if (type == typeof(Vector3Int)) return $"new Vector3Int({((Vector3Int)(instance ?? Vector3Int.zero)).x}, {((Vector3Int)(instance ?? Vector3Int.zero)).y}, {((Vector3Int)(instance ?? Vector3Int.zero)).z})";
            if (type == typeof(Color)) return $"new Color({((Color)(instance ?? Color.white)).r}f, {((Color)(instance ?? Color.white)).g}f, {((Color)(instance ?? Color.white)).b}f, {((Color)(instance ?? Color.white)).a}f)";
            if (type == typeof(Quaternion)) return "Quaternion.identity";
            if (type.IsEnum) return $"{GetTypeName(type)}.{Enum.GetName(type, instance ?? Activator.CreateInstance(type))}";

            if (instance != null && type.IsValueType)
                return $"new {GetTypeName(type)}()";

            return "null";
        }

        private static string ConvertToDefaultValue(string value, Type type)
        {
            if (string.IsNullOrEmpty(value))
                return GetTypeDefaultValue(type);

            if (type == typeof(float)) return $"{value}f";
            if (type == typeof(int)) return value;
            if (type == typeof(string)) return $"\"{value}\"";
            if (type == typeof(bool)) return value.ToLower();
            return value;
        }

        private static string GetTypeDefaultValue(Type type)
        {
            if (type == typeof(float)) return "0f";
            if (type == typeof(int)) return "0";
            if (type == typeof(string)) return "\"\"";
            if (type == typeof(bool)) return "false";
            if (type == typeof(Vector3)) return "Vector3.zero";
            if (type == typeof(Vector2)) return "Vector2.zero";
            if (type.IsEnum) return $"{GetTypeName(type)}.{Enum.GetNames(type)[0]}";
            return "null";
        }

        private static string GetTypeName(Type type)
        {
            if (type == typeof(float)) return "float";
            if (type == typeof(int)) return "int";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(void)) return "void";
            if (type == typeof(GameObject)) return "GameObject";
            if (type == typeof(Transform)) return "Transform";
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(Vector2Int)) return "Vector2Int";
            if (type == typeof(Vector3Int)) return "Vector3Int";
            if (type == typeof(Color)) return "Color";
            if (type == typeof(Quaternion)) return "Quaternion";
            if (type.IsEnum) return type.FullName;
            return type.Name;
        }

        /// <summary>
        /// 生成的属性信息
        /// </summary>
        private class GeneratedProperty
        {
            public string DataKey { get; set; }
            public string PropertyName { get; set; }
            public string DisplayName { get; set; }
            public string Category { get; set; }
            public string PropertyType { get; set; }
            public string GetterCode { get; set; }
            public string DefaultValue { get; set; }
        }
    }
}
#endif