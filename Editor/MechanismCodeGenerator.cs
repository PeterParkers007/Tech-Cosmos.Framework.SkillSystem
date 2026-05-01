#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    public static class MechanismCodeGeneratorV2
    {
        private const string MECHANISM_FOLDER = "Assets/Generated/Mechanisms";
        private const string CONDITION_FOLDER = "Assets/Generated/Conditions";

        #region 菜单项 - 统一在 SkillSystem/Generator 下

        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Generate ALL Classes", priority = 0)]
        public static void GenerateAllClasses()
        {
            GenerateMechanismClasses();
            GenerateConditionClasses();
            SkillDataSOGenerator.GenerateAllSkillDataSO();
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Generate Mechanism Classes", priority = 10)]
        public static void GenerateMechanismClasses()
        {
            GenerateClasses(
                MECHANISM_FOLDER,
                typeof(AutoGenerateMechanismAttribute),
                GenerateMechanismClassCode,
                "机制"
            );
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Generate Condition Classes", priority = 11)]
        public static void GenerateConditionClasses()
        {
            GenerateClasses(
                CONDITION_FOLDER,
                typeof(AutoGenerateConditionAttribute),
                GenerateConditionClassCode,
                "条件"
            );
        }

        #endregion

        #region 通用生成逻辑

        private static void GenerateClasses(
            string outputFolder,
            Type attributeType,
            Func<Type, Type, string> codeGenerator,
            string typeName)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToList();

            // 收集所有标记了 [GenerateMechanismsFor] 的 Unit 类型
            var markedUnitTypes = CollectMarkedUnitTypes(allAssemblies);

            // 收集所有标记了指定 Attribute 的泛型类型
            var genericTypes = CollectGenericTypes(allAssemblies, attributeType);

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            // 清理旧文件
            foreach (var file in Directory.GetFiles(outputFolder, "*.g.cs"))
                File.Delete(file);

            int count = 0;
            foreach (var (genericType, targetTypes) in genericTypes)
            {
                var targets = targetTypes.Length > 0
                    ? new List<Type>(targetTypes)
                    : markedUnitTypes;

                foreach (var unitType in targets)
                {
                    try
                    {
                        var code = codeGenerator(genericType, unitType);
                        var fileName = $"{unitType.Name}{GetCleanTypeName(genericType)}.g.cs";
                        var filePath = Path.Combine(outputFolder, fileName);
                        File.WriteAllText(filePath, code, Encoding.UTF8);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"生成{typeName}失败 [{genericType.Name}<{unitType.Name}>]: {e.Message}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"✅ 生成 {count} 个{typeName}类 -> {outputFolder}");
        }

        #endregion

        #region 类型收集

        private static List<Type> CollectMarkedUnitTypes(List<System.Reflection.Assembly> assemblies)
        {
            var types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var exportedTypes = assembly.GetExportedTypes()
                        .Where(t => t.GetCustomAttributes(typeof(GenerateMechanismsForAttribute), false).Any())
                        .Where(t => t.GetInterfaces().Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnit<>)));
                    types.AddRange(exportedTypes);
                }
                catch { }
            }
            return types;
        }

        private static List<(Type type, Type[] targetTypes)> CollectGenericTypes(
            List<System.Reflection.Assembly> assemblies,
            Type attributeType)
        {
            var result = new List<(Type, Type[])>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsGenericTypeDefinition)
                        .Where(t => t.GetCustomAttributes(attributeType, false).Any());

                    foreach (var type in types)
                    {
                        var attr = type.GetCustomAttributes(attributeType, false).FirstOrDefault();
                        var targetTypes = GetTargetTypes(attr) ?? Array.Empty<Type>();
                        result.Add((type, targetTypes));
                    }
                }
                catch { }
            }

            return result;
        }

        private static Type[] GetTargetTypes(object attribute)
        {
            if (attribute is AutoGenerateMechanismAttribute mechAttr)
                return mechAttr.TargetTypes;

            if (attribute is AutoGenerateConditionAttribute condAttr)
                return condAttr.TargetTypes;

            return null;
        }

        #endregion

        #region 代码生成

        private static string GenerateMechanismClassCode(Type openGenericType, Type unitType)
        {
            return GenerateClassCode(openGenericType, unitType, "Mechanisms");
        }

        private static string GenerateConditionClassCode(Type openGenericType, Type unitType)
        {
            return GenerateClassCode(openGenericType, unitType, "Conditions");
        }

        private static string GenerateClassCode(Type openGenericType, Type unitType, string subFolder)
        {
            var ns = openGenericType.Namespace ?? "TechCosmos.SkillSystem.Runtime";
            var baseClassName = GetCleanTypeName(openGenericType);
            var unitClassName = GetCleanTypeName(unitType);
            var className = $"{unitClassName}{baseClassName}";

            return $@"// <auto-generated/>
// 生成时间: {DateTime.Now:yyyy/MM/dd HH:mm:ss}
// 类型: {baseClassName}<{unitClassName}>
// 请勿手动修改此文件

using System;
using {ns};

namespace {ns}.Generated.{subFolder}
{{
    [Serializable]
    public class {className} : {baseClassName}<{unitClassName}>
    {{
    }}
}}
";
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取类型在菜单中的简短分组名
        /// </summary>
        public static string GetShortGroupName(Type type)
        {
            var name = GetCleanTypeName(type).ToLower();

            if (name.Contains("damage")) return "伤害";
            if (name.Contains("heal")) return "治疗";
            if (name.Contains("buff")) return "增益";
            if (name.Contains("debuff")) return "减益";
            if (name.Contains("move")) return "移动";
            if (name.Contains("spawn")) return "生成";
            if (name.Contains("cooldown")) return "冷却";
            if (name.Contains("func")) return "函数";

            var ns = type.Namespace ?? "";
            var lastDot = ns.LastIndexOf('.');
            return lastDot >= 0 ? ns.Substring(lastDot + 1) : ns;
        }

        /// <summary>
        /// 获取类型的干净名称，去掉泛型标记
        /// </summary>
        public static string GetCleanTypeName(Type type)
        {
            var name = type.Name;

            int backtickIndex = name.IndexOf('`');
            if (backtickIndex > 0)
            {
                name = name.Substring(0, backtickIndex);
            }

            if (type.DeclaringType != null)
            {
                return $"{GetCleanTypeName(type.DeclaringType)}.{name}";
            }

            return name;
        }

        #endregion
    }
}
#endif