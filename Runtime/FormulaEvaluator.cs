using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能公式求值器，支持 caster/target 属性引用、random() 与四则运算。
    /// </summary>
    public static class FormulaEvaluator
    {
        private static readonly Regex ReferenceRegex =
            new(@"\b(caster|target)\.([\w.]+)\b", RegexOptions.Compiled);

        private static readonly Regex RandomRegex =
            new(@"random\(\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\)", RegexOptions.Compiled);

        private static readonly Regex MulDivExprRegex =
            new(@"(-?\d+\.?\d*)([\*/])(-?\d+\.?\d*)", RegexOptions.Compiled);

        private static readonly Dictionary<string, float> _expressionCache = new();

        public static float Evaluate<T>(SkillContext<T> context, string formula) where T : class, IUnit<T>
        {
            if (string.IsNullOrEmpty(formula)) return 0f;

            SkillProfilerMarkers.Formula.Begin();
            try
            {
                var baseContext = (SkillContextBase)context;
                var random = context.meta.ResolveRandom();
                var resolved = ResolveReferences(baseContext, formula, random);
                return EvaluateExpressionStatic(resolved);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FormulaEvaluator] 公式求值失败: {formula}\n{e.Message}");
                return 0f;
            }
            finally
            {
                SkillProfilerMarkers.Formula.End();
            }
        }

        public static float Evaluate(SkillContextBase context, string formula, IRandomProvider random = null)
        {
            if (string.IsNullOrEmpty(formula)) return 0f;
            random ??= SkillSystemServices.Random;
            var resolved = ResolveReferences(context, formula, random);
            return EvaluateExpressionStatic(resolved);
        }

        public static float EvaluateExpressionStatic(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return 0f;
            if (_expressionCache.TryGetValue(expression, out var cached)) return cached;

            expression = expression.Replace(" ", "");
            expression = ResolveParentheses(expression);
            var result = EvaluateNoParentheses(expression);
            if (_expressionCache.Count < 512)
                _expressionCache[expression] = result;
            return result;
        }

        public static void ClearCache()
        {
            _expressionCache.Clear();
            PathResolver.ClearCache();
        }

        public static float ToFloat(object obj) => obj switch
        {
            float f => f,
            int i => i,
            double d => (float)d,
            bool b => b ? 1f : 0f,
            long l => l,
            short s => s,
            byte bt => bt,
            _ => 0f
        };

        private static string ResolveReferences(SkillContextBase context, string formula, IRandomProvider random)
        {
            var resolved = ReferenceRegex.Replace(formula, match =>
            {
                var source = match.Groups[1].Value;
                var path = match.Groups[2].Value;
                var value = PathResolver.Resolve(context, source, path);
                return value.ToString(CultureInfo.InvariantCulture);
            });

            resolved = RandomRegex.Replace(resolved, match =>
            {
                float min = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float max = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                return random.Range(min, max).ToString(CultureInfo.InvariantCulture);
            });

            return resolved;
        }

        private static string ResolveParentheses(string expr)
        {
            int start = expr.LastIndexOf('(');
            while (start >= 0)
            {
                int end = expr.IndexOf(')', start);
                if (end < 0)
                {
                    Debug.LogWarning($"[FormulaEvaluator] 公式括号不匹配: {expr}");
                    return expr.Replace("(", "").Replace(")", "");
                }

                string inner = expr.Substring(start + 1, end - start - 1);
                float innerResult = EvaluateNoParentheses(inner);
                string innerStr = innerResult.ToString(CultureInfo.InvariantCulture);
                expr = expr.Substring(0, start) + innerStr + expr.Substring(end + 1);
                start = expr.LastIndexOf('(');
            }

            return expr;
        }

        private static float EvaluateNoParentheses(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return 0f;
            expr = HandleUnaryMinus(expr);
            expr = EvaluateMultiplyDivide(expr);
            expr = EvaluateAddSubtract(expr);

            if (float.TryParse(expr, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out float result))
                return result;

            return 0f;
        }

        private static string HandleUnaryMinus(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return expr;
            expr = expr.Replace("(-", "(0-");
            return expr;
        }

        private static string EvaluateMultiplyDivide(string expr)
        {
            var match = MulDivExprRegex.Match(expr);
            while (match.Success)
            {
                float left = float.Parse(match.Groups[1].Value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                float right = float.Parse(match.Groups[3].Value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                float result = match.Groups[2].Value[0] == '*'
                    ? left * right
                    : (right != 0f ? left / right : 0f);

                expr = expr.Substring(0, match.Index)
                    + result.ToString(CultureInfo.InvariantCulture)
                    + expr.Substring(match.Index + match.Length);
                match = MulDivExprRegex.Match(expr);
            }

            return expr;
        }

        private static string EvaluateAddSubtract(string expr)
        {
            var numbers = new List<float>();
            var operators = new List<char>();
            int i = 0;

            while (i < expr.Length)
            {
                char c = expr[i];
                if (c == '+' || c == '-')
                {
                    if (i > 0 && expr[i - 1] != '+' && expr[i - 1] != '-')
                    {
                        operators.Add(c);
                        i++;
                        continue;
                    }
                }

                if (c == '+' || c == '-' || char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    if (expr[i] == '+' || expr[i] == '-')
                        i++;
                    while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                        i++;

                    var token = expr.Substring(start, i - start);
                    if (float.TryParse(token, NumberStyles.Float | NumberStyles.AllowLeadingSign,
                            CultureInfo.InvariantCulture, out float num))
                    {
                        numbers.Add(num);
                    }
                }
                else
                {
                    i++;
                }
            }

            if (numbers.Count == 0) return expr;

            float result = numbers[0];
            for (int op = 0; op < operators.Count; op++)
            {
                float next = numbers[op + 1];
                result = operators[op] == '+' ? result + next : result - next;
            }

            return result.ToString(CultureInfo.InvariantCulture);
        }

        private static class PathResolver
        {
            private static readonly Dictionary<string, Func<SkillContextBase, float>> _cache = new();

            public static void ClearCache() => _cache.Clear();

            public static float Resolve(SkillContextBase context, string source, string path)
            {
                var key = source + "." + path;
                if (!_cache.TryGetValue(key, out var resolver))
                {
                    resolver = BuildResolver(source, path);
                    _cache[key] = resolver;
                }
                return resolver(context);
            }

            private static Func<SkillContextBase, float> BuildResolver(string source, string path)
            {
                var parts = path.Split('.');
                return ctx =>
                {
                    object obj = source switch
                    {
                        "caster" => ctx.Caster,
                        "target" => ctx.Target,
                        _ => null
                    };

                    foreach (var part in parts)
                    {
                        if (obj == null) return 0f;
                        var type = obj.GetType();

                        var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (property != null && property.CanRead)
                        {
                            obj = property.GetValue(obj);
                            continue;
                        }

                        var field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            obj = field.GetValue(obj);
                            continue;
                        }

                        return 0f;
                    }

                    return ToFloat(obj);
                };
            }
        }
    }
}
