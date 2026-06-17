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
    public static class BuffEffectCodeGenerator
    {
        // ������·����Ӧ����ϵͳ�� Generated/Mechanisms �� Generated/Conditions
        private const string EFFECT_GENERATED_FOLDER = "Assets/Generated/SkillSystem/Effects";
        private const string MODE_GENERATED_FOLDER = "Assets/Generated/SkillSystem/ExecutionModes";

        [MenuItem("Tech-Cosmos/SkillSystem/Generate All BuffEffect Classes", priority = 20)]
        public static void GenerateAllBuffEffectClasses()
        {
            GenerateEffectClasses();
            GenerateExecutionModeClasses();
            Debug.Log("GBF: BuffEffect �� ExecutionMode ���������");
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Generate BuffEffect Classes", priority = 21)]
        public static void GenerateEffectClasses()
        {
            GenerateClasses(
                EFFECT_GENERATED_FOLDER,
                typeof(BuffEffect<>),
                "Ч��"
            );
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Generate ExecutionMode Classes", priority = 22)]
        public static void GenerateExecutionModeClasses()
        {
            GenerateClasses(
                MODE_GENERATED_FOLDER,
                typeof(ExecutionMode<>),
                "ִ��ģʽ"
            );
        }

        private static void GenerateClasses(string outputFolder, Type genericBaseType, string typeLabel)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToList();

            // �ռ����б���� [AutoGenerateBuffEffect] �Ŀ��ŷ���
            var genericTypes = CollectGenericTypes(allAssemblies, genericBaseType);

            // �ռ�����ע���Ŀ�����ͣ�����û�������ض��� T��
            var targetTypes = CollectTargetTypes(allAssemblies);

            if (targetTypes.Count == 0)
            {
                // ���û���κ�����ע�ᣬĬ���ó�������
                targetTypes.Add(typeof(object));
                Debug.LogWarning($"[GBF Generator] δ�ҵ��κ�ע���Ŀ�����ͣ�Ĭ��ʹ�� object");
            }

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            // ������ļ�
            foreach (var file in Directory.GetFiles(outputFolder, "*.g.cs"))
                File.Delete(file);

            int count = 0;
            foreach (var (openGenericType, _) in genericTypes)
            {
                foreach (var targetType in targetTypes)
                {
                    try
                    {
                        var code = GenerateClassCode(openGenericType, targetType, outputFolder);
                        var fileName = $"{GetCleanName(targetType)}{GetCleanName(openGenericType)}.g.cs";
                        var filePath = Path.Combine(outputFolder, fileName);
                        File.WriteAllText(filePath, code, Encoding.UTF8);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GBF Generator] ����{typeLabel}ʧ�� [{openGenericType.Name}<{targetType.Name}>]: {e.Message}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"GBF: ���� {count} ��{typeLabel}�� -> {outputFolder}");
        }

        private static List<(Type type, Type[] targetTypes)> CollectGenericTypes(
            List<System.Reflection.Assembly> assemblies, Type genericBaseType)
        {
            var result = new List<(Type, Type[])>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsGenericTypeDefinition && !t.IsAbstract)
                        .Where(t =>
                        {
                            // ����Ƿ�̳��Է��ͻ���
                            var baseType = t.BaseType;
                            while (baseType != null)
                            {
                                if (baseType.IsGenericType &&
                                    baseType.GetGenericTypeDefinition() == genericBaseType)
                                    return true;
                                baseType = baseType.BaseType;
                            }
                            return false;
                        });

                    foreach (var type in types)
                    {
                        var targetTypesAttr = type.GetCustomAttributes(typeof(AutoGenerateBuffEffectAttribute), false)
                            .FirstOrDefault() as AutoGenerateBuffEffectAttribute;
                        var targets = targetTypesAttr?.TargetTypes ?? Array.Empty<Type>();
                        result.Add((type, targets));
                    }
                }
                catch { }
            }

            return result;
        }

        private static List<Type> CollectTargetTypes(List<System.Reflection.Assembly> assemblies)
        {
            var types = new HashSet<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var marked = assembly.GetExportedTypes()
                        .Where(t => t.GetCustomAttributes(typeof(ApplyBuffTargetAttribute), false).Any());

                    foreach (var t in marked)
                        types.Add(t);
                }
                catch { }
            }

            return types.ToList();
        }

        private static string GenerateClassCode(Type openGenericType, Type targetType, string outputFolder)
        {
            var ns = openGenericType.Namespace ?? "TechCosmos.SkillSystem.Runtime.Effects";
            var baseClassName = GetCleanName(openGenericType);
            var targetClassName = GetCleanName(targetType);
            var className = $"{targetClassName}{baseClassName}";

            // �ж��� Effect ���� ExecutionMode
            bool isEffect = outputFolder.Contains("Effects");
            string baseClass = isEffect
                ? $"{baseClassName}<{targetClassName}>"
                : $"{baseClassName}<{targetClassName}>";

            var attr = openGenericType.GetCustomAttributes(typeof(BuffEffectMenuAttribute), false)
                .FirstOrDefault() as BuffEffectMenuAttribute;

            string attributeLine = "";
            if (attr != null)
            {
                string displayName = attr.DisplayName ?? baseClassName;
                attributeLine = $"\n    [BuffEffectMenu(\"{attr.Category}\", DisplayName = \"{displayName}\")]";
            }

            return $@"// <auto-generated/>
// ����ʱ��: {DateTime.Now:yyyy/MM/dd HH:mm:ss}
// ����: {baseClassName}<{targetClassName}>
// �����ֶ��޸Ĵ��ļ�

using System;
using {ns};

namespace {ns}.Generated
{{
    [Serializable]{attributeLine}
    public sealed class {className} : {baseClass}
    {{
    }}
}}
";
        }

        private static string GetCleanName(Type type)
        {
            var name = type.Name;
            int backtick = name.IndexOf('`');
            if (backtick > 0)
                name = name.Substring(0, backtick);
            return name;
        }
    }
}
#endif