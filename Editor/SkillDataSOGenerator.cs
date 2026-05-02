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

        private static readonly HashSet<string> SimpleTypeNames = new HashSet<string>
        {
            "float", "int", "string", "bool", "double", "long", "char", "byte",
            "Vector2", "Vector3", "Vector4", "Vector2Int", "Vector3Int",
            "Color", "Color32", "Quaternion",
            "Rect", "RectInt", "Bounds", "BoundsInt",
            "LayerMask", "AnimationCurve", "Gradient",
            "GameObject", "Transform"
        };

        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Generate All SkillDataSO")]
        public static void GenerateAllSkillDataSO()
        {
            if (!Directory.Exists(GENERATED_FOLDER))
                Directory.CreateDirectory(GENERATED_FOLDER);

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

            var markedFields = unitType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<SkillDataFieldAttribute>() != null)
                .ToList();

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

            foreach (var field in markedFields)
            {
                var attr = field.GetCustomAttribute<SkillDataFieldAttribute>();
                var category = attr?.Category ?? "基础属性";

                if (!fieldGroups.ContainsKey(category))
                    fieldGroups[category] = new List<GeneratedProperty>();

                var properties = AnalyzeField(field, attr);
                fieldGroups[category].AddRange(properties);
            }

            // 头部
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

            // 初始化区域
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

            // 简单类型初始化
            foreach (var group in fieldGroups)
            {
                foreach (var prop in group.Value)
                {
                    if (prop.IsComplex) continue;
                    if (prop.DefaultValue == "null") continue;

                    sb.AppendLine($"            // {group.Key}: {prop.DisplayName}");
                    sb.AppendLine($"            if (!ContainsKey(\"{prop.DataKey}\"))");
                    sb.AppendLine($"                SetGeneratedValue(\"{prop.DataKey}\", {prop.DefaultValue});");
                    sb.AppendLine();
                }
            }

            // DataEntry 初始化
            foreach (var entry in dataEntries)
            {
                sb.AppendLine($"            // {entry.description ?? entry.key}");
                sb.AppendLine($"            if (!ContainsKey(\"{entry.key}\"))");
                sb.AppendLine($"                SetValue(\"{entry.key}\", {ConvertToDefaultValue(entry.defaultValue, entry.valueType)});");
                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine("        #endregion");
            sb.AppendLine();

            // 生成属性区域
            sb.AppendLine("        #region 自动生成的属性");
            sb.AppendLine();

            foreach (var group in fieldGroups)
            {
                if (group.Value.Count > 0)
                {
                    sb.AppendLine($"        // ===== {group.Key} =====");
                    sb.AppendLine();

                    foreach (var prop in group.Value)
                    {
                        GeneratePropertyCode(sb, prop);
                    }
                }
            }

            sb.AppendLine("        #endregion");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void GeneratePropertyCode(StringBuilder sb, GeneratedProperty prop)
        {
            if (prop.IsComplex)
            {
                // 复杂类型用序列化字段（不用字典）
                var fieldName = $"_{char.ToLower(prop.PropertyName[0])}{prop.PropertyName.Substring(1)}";
                sb.AppendLine($"        [SerializeField]");
                sb.AppendLine($"        private {prop.PropertyType} {fieldName} = {prop.DefaultValue};");
                sb.AppendLine();
                sb.AppendLine($"        [Tooltip(\"[{prop.Category}] {prop.DisplayName}\")]");
                sb.AppendLine($"        public {prop.PropertyType} {prop.PropertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {fieldName};");
                sb.AppendLine($"            set => {fieldName} = value ?? {prop.DefaultValue};");
                sb.AppendLine("        }");
            }
            else
            {
                // 简单类型用字典存储
                sb.AppendLine($"        [Tooltip(\"[{prop.Category}] {prop.DisplayName}\")]");
                sb.AppendLine($"        public {prop.PropertyType} {prop.PropertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => GetValue<{prop.PropertyType}>(\"{prop.DataKey}\");");
                sb.AppendLine($"            set => SetGeneratedValue(\"{prop.DataKey}\", value);");
                sb.AppendLine("        }");
            }
            sb.AppendLine();
        }

        private static List<GeneratedProperty> AnalyzeField(FieldInfo field, SkillDataFieldAttribute attr)
        {
            var properties = new List<GeneratedProperty>();
            var fieldType = field.FieldType;
            var fieldName = field.Name;
            var category = attr?.Category ?? "基础属性";
            var typeName = GetTypeName(fieldType);
            var isComplex = IsComplexTypeName(typeName) || IsComplexType(fieldType);

            // 复杂类型不拆分，直接整体
            if (isComplex || !ShouldFlatten(fieldType))
            {
                var prop = new GeneratedProperty
                {
                    DataKey = fieldName,
                    PropertyName = $"{char.ToUpper(fieldName[0])}{fieldName.Substring(1)}",
                    DisplayName = attr?.DisplayName ?? ObjectNames.NicifyVariableName(fieldName),
                    Category = category,
                    PropertyType = typeName,
                    GetterCode = isComplex ? fieldName : $"GetValue<{typeName}>(\"{fieldName}\")",
                    DefaultValue = GetDefaultValueForType(fieldType, field.GetValue(Activator.CreateInstance(field.DeclaringType))),
                    IsComplex = isComplex
                };
                properties.Add(prop);
                return properties;
            }

            // 可拆分类型
            foreach (var subField in fieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (subField.IsInitOnly || subField.IsLiteral) continue;

                var subAttr = subField.GetCustomAttribute<SkillDataFieldAttribute>();
                var subTypeName = GetTypeName(subField.FieldType);
                var subIsComplex = IsComplexTypeName(subTypeName);

                properties.Add(new GeneratedProperty
                {
                    DataKey = $"{fieldName}.{subField.Name}",
                    PropertyName = $"{char.ToUpper(fieldName[0])}{fieldName.Substring(1)}{char.ToUpper(subField.Name[0])}{subField.Name.Substring(1)}",
                    DisplayName = subAttr?.DisplayName ?? ObjectNames.NicifyVariableName(subField.Name),
                    Category = subAttr?.Category ?? category,
                    PropertyType = subTypeName,
                    GetterCode = subField.FieldType.IsEnum
                        ? $"({subTypeName})GetValue<int>(\"{fieldName}.{subField.Name}\")"
                        : $"GetValue<{subTypeName}>(\"{fieldName}.{subField.Name}\")",
                    DefaultValue = GetDefaultValueForType(subField.FieldType,
                        subField.GetValue(Activator.CreateInstance(fieldType))),
                    IsComplex = subIsComplex
                });
            }

            return properties;
        }

        private static bool IsComplexTypeName(string typeName)
        {
            if (typeName.Contains("<")) return true;
            if (SimpleTypeNames.Contains(typeName)) return false;
            if (typeName.Contains(".")) return false; // 枚举
            return true;
        }

        private static bool IsComplexType(Type type)
        {
            if (type.IsArray) return true;
            if (type.IsGenericType) return true;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;
            if (type.IsAbstract) return true;
            return false;
        }

        private static bool ShouldFlatten(Type type)
        {
            if (PrimitiveTypes.Contains(type)) return false;
            if (type.IsEnum) return false;
            if (type.IsArray) return false;

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>)) return false;
                if (genericDef == typeof(Dictionary<,>)) return false;
                if (genericDef == typeof(HashSet<>)) return false;
                if (genericDef == typeof(IEnumerable<>)) return false;
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;

            if (type.IsSerializable && !type.IsAbstract)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields.Length > 5) return false;
                return true;
            }

            return false;
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

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var typeName = genericDef.Name;
                int backtickIndex = typeName.IndexOf('`');
                if (backtickIndex > 0)
                    typeName = typeName.Substring(0, backtickIndex);

                var args = type.GetGenericArguments();
                var argNames = string.Join(", ", args.Select(a => GetTypeName(a)));

                if (genericDef == typeof(List<>)) return $"List<{argNames}>";
                if (genericDef == typeof(Dictionary<,>)) return $"Dictionary<{argNames}>";
                if (genericDef == typeof(HashSet<>)) return $"HashSet<{argNames}>";

                return $"{typeName}<{argNames}>";
            }

            if (type.IsEnum) return type.FullName ?? type.Name;

            return type.Name;
        }

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

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>)) return $"new {GetTypeName(type)}()";
                if (genericDef == typeof(Dictionary<,>)) return $"new {GetTypeName(type)}()";
                if (genericDef == typeof(HashSet<>)) return $"new {GetTypeName(type)}()";
                return "null";
            }

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

        private class GeneratedProperty
        {
            public string DataKey { get; set; }
            public string PropertyName { get; set; }
            public string DisplayName { get; set; }
            public string Category { get; set; }
            public string PropertyType { get; set; }
            public string GetterCode { get; set; }
            public string DefaultValue { get; set; }
            public bool IsComplex { get; set; }
        }
    }
}
#endif