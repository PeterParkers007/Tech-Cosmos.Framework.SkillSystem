// ============================================================
// �ļ���BuffDataSO.cs
// ·����TechCosmos.SkillSystem.Runtime/BuffDataSO.cs
// ============================================================
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    [CreateAssetMenu(menuName = "SkillSystem/Buff Data", fileName = "NewBuffData")]
    public class BuffDataSO : ScriptableObject
    {
        [Header("������Ϣ")]
        public string buffName;
        public float duration;

        [Header("��ǩ")]
        public string[] tags = Array.Empty<string>();

        [Header("�ѵ�")]
        public BuffStackPolicy stackPolicy = BuffStackPolicy.ExtendDuration;
        public int maxStacks = 1;

        [Header("�����޸�")]
        public List<ModifierConfig> modifiers = new();

        [Header("�¼���Ӧ")]
        public List<ActionConfig> actions = new();

        [Header("Ч��ִ����")]
        [SerializeReference]
        public List<BuffEffectExecuterBase> effectExecuters = new();

        [Header("Graph Editor Layout")]
        public GraphEditorLayout graphLayout = new();
    }

    // ===== ����Ϊԭ���࣬���ֲ��� =====

    public enum BuffFormulaType { Static, Reference, Custom }
    public enum ModifierMode { Set, Add, Multiply }

    [Serializable]
    public class BuffFormulaValue
    {
        public BuffFormulaType formulaType = BuffFormulaType.Static;
        public float staticValue;
        public string referencePath;
        public float multiplier = 1f;
        public float offset;
        public string customFormula;

        public float Evaluate<T>(BuffModifyContext<T> ctx) where T : class
        {
            return formulaType switch
            {
                BuffFormulaType.Static => staticValue,
                BuffFormulaType.Reference => ResolveReference(ctx?.target) * multiplier + offset,
                BuffFormulaType.Custom => BuffFormulaEvaluator.Evaluate(ctx?.target, ctx?.caster, customFormula),
                _ => 0f
            };
        }

        private float ResolveReference(object target)
        {
            if (target == null || string.IsNullOrEmpty(referencePath)) return 0f;
            var type = target.GetType();
            var field = type.GetField(referencePath, BindingFlags.Public | BindingFlags.Instance);
            var prop = type.GetProperty(referencePath, BindingFlags.Public | BindingFlags.Instance);
            object value = field != null ? field.GetValue(target) : prop?.GetValue(target);
            return value switch { float f => f, int i => i, _ => 0f };
        }
    }

    [Serializable]
    public class ModifierConfig
    {
        public string modifyType;
        public ModifierMode mode;
        public BuffFormulaValue formula = new();

        public Func<float, BuffModifyContext<T>, float> BuildModifier<T>() where T : class
        {
            return (baseValue, ctx) =>
            {
                float val = formula.Evaluate(ctx);
                return mode switch
                {
                    ModifierMode.Set => val,
                    ModifierMode.Add => baseValue + val,
                    ModifierMode.Multiply => baseValue * val,
                    _ => baseValue
                };
            };
        }
    }

    [Serializable]
    public class ActionConfig
    {
        public string actionName;

        [SerializeReference]
        public List<BuffEffectBase> effects = new();
    }

    [Serializable]
    public class EffectConfig
    {
        public string effectType;
        public string description;
    }

    public static class BuffFormulaEvaluator
    {
        public static float Evaluate(object target, object caster, string formula)
        {
            if (string.IsNullOrEmpty(formula)) return 0f;
            try
            {
                var resolved = ResolveReferences(target, caster, formula);
                return EvaluateAll(resolved);
            }
            catch (Exception e) { Debug.LogWarning($"[BuffFormula] ����ʧ��: {formula}\n{e.Message}"); return 0f; }
        }

        private static string ResolveReferences(object target, object caster, string formula)
        {
            var regex = new Regex(@"\b(caster|target)\.([\w.]+)");
            return regex.Replace(formula, match =>
            {
                var source = match.Groups[1].Value;
                var field = match.Groups[2].Value;
                var obj = source == "caster" ? caster : target;
                return ResolveField(obj, field).ToString(CultureInfo.InvariantCulture);
            });
        }

        private static float ResolveField(object obj, string fieldName)
        {
            if (obj == null) return 0f;

            // ����Ƕ��·������ "Runtime.MoveSpeed"
            if (fieldName.Contains('.'))
            {
                var parts = fieldName.Split('.');
                object current = obj;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    current = ResolveSingleField(current, parts[i]);
                    if (current == null) return 0f;
                }
                return ConvertToFloat(ResolveSingleField(current, parts[parts.Length - 1]));
            }

            return ConvertToFloat(ResolveSingleField(obj, fieldName));
        }

        private static object ResolveSingleField(object obj, string fieldName)
        {
            if (obj == null) return null;
            var type = obj.GetType();

            // �ȳ����ֶ�
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(obj);

            // �ٳ�������
            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanRead) return prop.GetValue(obj);

            return null;
        }

        private static float ConvertToFloat(object value)
        {
            return value switch
            {
                float f => f,
                int i => i,
                double d => (float)d,
                bool b => b ? 1f : 0f,
                _ => 0f
            };
        }

        private static float EvaluateAll(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return 0f;
            expr = expr.Replace(" ", "");
            expr = ResolveParentheses(expr);
            var mdRegex = new Regex(@"(-?\d+\.?\d*)\s*([\*/])\s*(-?\d+\.?\d*)");
            var mdMatch = mdRegex.Match(expr);
            while (mdMatch.Success)
            {
                float left = float.Parse(mdMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                float right = float.Parse(mdMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                float result = mdMatch.Groups[2].Value == "*" ? left * right : (right != 0 ? left / right : 0);
                expr = expr.Replace(mdMatch.Value, result.ToString(CultureInfo.InvariantCulture));
                mdMatch = mdRegex.Match(expr);
            }
            var asRegex = new Regex(@"(-?\d+\.?\d*)\s*([+\-])\s*(-?\d+\.?\d*)");
            var asMatch = asRegex.Match(expr);
            while (asMatch.Success)
            {
                float left = float.Parse(asMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                float right = float.Parse(asMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                float result = asMatch.Groups[2].Value == "+" ? left + right : left - right;
                expr = expr.Replace(asMatch.Value, result.ToString(CultureInfo.InvariantCulture));
                asMatch = asRegex.Match(expr);
            }
            return float.TryParse(expr, NumberStyles.Float, CultureInfo.InvariantCulture, out float final) ? final : 0f;
        }

        private static string ResolveParentheses(string expr)
        {
            int start = expr.LastIndexOf('(');
            while (start >= 0)
            {
                int end = expr.IndexOf(')', start);
                if (end < 0) return expr;
                string inner = expr.Substring(start + 1, end - start - 1);
                float result = EvaluateAll(inner);
                expr = expr.Substring(0, start) + result.ToString(CultureInfo.InvariantCulture) + expr.Substring(end + 1);
                start = expr.LastIndexOf('(');
            }
            return expr;
        }
    }
}