using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    public static class FormulaEvaluator
    {
        public static float Evaluate<T>(SkillContext<T> context, string formula) where T : class, IUnit<T>
        {
            if (string.IsNullOrEmpty(formula))
                return 0f;

            try
            {
                // 转换为基类
                var baseContext = (SkillContextBase)context;
                var resolved = ResolveReferences(baseContext, formula);
                return SafeEvaluate(resolved);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"公式解析失败: {formula}\n{e.Message}");
                return 0f;
            }
        }
        // 非泛型版本（如果 SkillContext<T> 可以隐式转换，这个不需要了）
        // 保留以防其他地方需要
        public static float Evaluate(SkillContextBase context, string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return 0f;

            try
            {
                var resolved = ResolveReferences(context, formula);
                return SafeEvaluate(resolved);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"公式解析失败: {formula}\n{e.Message}");
                return 0f;
            }
        }
        private static string ResolveReferences(SkillContextBase context, string formula)
        {
            var regex = new Regex(@"\b(caster|target)\.([\w.]+)\b");
            return regex.Replace(formula, match =>
            {
                var source = match.Groups[1].Value;
                var path = match.Groups[2].Value;
                var value = ResolvePath(context, source, path);
                return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            });
        }

        private static float ResolvePath(SkillContextBase context, string source, string path)
        {
            object obj = source switch
            {
                "caster" => context.Caster,
                "target" => context.Target,
                _ => null
            };

            if (obj == null) return 0f;

            var parts = path.Split('.');
            foreach (var part in parts)
            {
                if (obj == null) return 0f;

                var type = obj.GetType();
                var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                var field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                if (property != null)
                    obj = property.GetValue(obj);
                else if (field != null)
                    obj = field.GetValue(obj);
                else
                    return 0f;
            }

            return obj switch
            {
                float f => f,
                int i => i,
                double d => (float)d,
                bool b => b ? 1f : 0f,
                _ => 0f
            };
        }

        private static float SafeEvaluate(string expression)
        {
            return EvaluateSimple(expression.Replace(" ", ""));
        }

        private static float EvaluateSimple(string expr)
        {
            expr = Regex.Replace(expr, @"(-?\d+\.?\d*)\s*\*\s*(-?\d+\.?\d*)", m =>
            {
                float a = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                float b = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                return (a * b).ToString(System.Globalization.CultureInfo.InvariantCulture);
            });

            expr = Regex.Replace(expr, @"(-?\d+\.?\d*)\s*/\s*(-?\d+\.?\d*)", m =>
            {
                float a = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                float b = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                return b != 0 ? (a / b).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
            });

            expr = Regex.Replace(expr, @"(-?\d+\.?\d*)\s*\+\s*(-?\d+\.?\d*)", m =>
            {
                float a = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                float b = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                return (a + b).ToString(System.Globalization.CultureInfo.InvariantCulture);
            });

            expr = Regex.Replace(expr, @"(-?\d+\.?\d*)\s*\-\s*(-?\d+\.?\d*)", m =>
            {
                float a = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                float b = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                return (a - b).ToString(System.Globalization.CultureInfo.InvariantCulture);
            });

            if (float.TryParse(expr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
                return result;

            return 0f;
        }
    }
}