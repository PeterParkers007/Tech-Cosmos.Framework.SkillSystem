using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Globalization;

namespace TechCosmos.SkillSystem.Runtime
{
    public static class FormulaEvaluator
    {
        // 缓存：Type → (成员名 → 访问器委托)
        private static readonly Dictionary<Type, Dictionary<string, MemberAccessor>> _accessorCache
            = new Dictionary<Type, Dictionary<string, MemberAccessor>>();

        // 成员访问器委托：传入对象，返回值
        private delegate float MemberAccessor(object obj);

        public static float Evaluate<T>(SkillContext<T> context, string formula) where T : class, IUnit<T>
        {
            if (string.IsNullOrEmpty(formula))
                return 0f;

            try
            {
                var baseContext = (SkillContextBase)context;
                var resolved = ResolveReferences(baseContext, formula);
                return EvaluateExpression(resolved);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"公式解析失败: {formula}\n{e.Message}");
                return 0f;
            }
        }

        public static float Evaluate(SkillContextBase context, string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return 0f;

            try
            {
                var resolved = ResolveReferences(context, formula);
                return EvaluateExpression(resolved);
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
                return value.ToString(CultureInfo.InvariantCulture);
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
                var accessor = GetOrCreateAccessor(type, part);

                if (accessor != null)
                    obj = accessor(obj);
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

        /// <summary>
        /// 获取或创建成员访问器（带缓存）
        /// </summary>
        private static MemberAccessor GetOrCreateAccessor(Type type, string memberName)
        {
            // 检查缓存
            if (!_accessorCache.TryGetValue(type, out var memberCache))
            {
                memberCache = new Dictionary<string, MemberAccessor>();
                _accessorCache[type] = memberCache;
            }

            if (memberCache.TryGetValue(memberName, out var cachedAccessor))
                return cachedAccessor;

            // 创建访问器
            var accessor = CreateAccessor(type, memberName);
            memberCache[memberName] = accessor;
            return accessor;
        }

        /// <summary>
        /// 为指定类型的指定成员创建访问器委托
        /// </summary>
        private static MemberAccessor CreateAccessor(Type type, string memberName)
        {
            // 查找属性
            var property = type.GetProperty(memberName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (property != null && property.CanRead)
            {
                var getMethod = property.GetGetMethod(true);
                if (getMethod != null)
                {
                    return obj =>
                    {
                        var value = getMethod.Invoke(obj, null);
                        return ConvertToFloat(value);
                    };
                }
            }

            // 查找字段
            var field = type.GetField(memberName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                return obj =>
                {
                    var value = field.GetValue(obj);
                    return ConvertToFloat(value);
                };
            }

            return null;
        }

        /// <summary>
        /// 将任意值转换为 float
        /// </summary>
        private static float ConvertToFloat(object value)
        {
            if (value == null) return 0f;

            if (value is float f) return f;
            if (value is int i) return i;
            if (value is double d) return (float)d;
            if (value is bool b) return b ? 1f : 0f;
            if (value is long l) return l;
            if (value is short s) return s;
            if (value is byte bt) return bt;
            if (value is IConvertible convertible)
                return convertible.ToSingle(CultureInfo.InvariantCulture);

            return 0f;
        }

        /// <summary>
        /// 完整表达式求值：支持括号、标准运算符优先级
        /// </summary>
        private static float EvaluateExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0f;

            expression = expression.Replace(" ", "");

            // 第一步：处理括号（递归）
            expression = ResolveParentheses(expression);

            // 第二步：按优先级处理运算符
            return EvaluateNoParentheses(expression);
        }

        private static string ResolveParentheses(string expr)
        {
            int start = expr.LastIndexOf('(');
            while (start >= 0)
            {
                int end = expr.IndexOf(')', start);
                if (end < 0)
                {
                    Debug.LogWarning($"公式括号不匹配: {expr}");
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
            if (string.IsNullOrEmpty(expr))
                return 0f;

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

            if (expr[0] == '-')
            {
                expr = "0" + expr;
            }

            expr = expr.Replace("(-", "(0-");

            return expr;
        }

        private static string EvaluateMultiplyDivide(string expr)
        {
            var tokens = Tokenize(expr);

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                if (token.Contains("*") || token.Contains("/"))
                {
                    tokens[i] = EvaluateMulDivToken(token);
                }
            }

            return string.Join("", tokens);
        }

        private static string EvaluateMulDivToken(string token)
        {
            var numbers = new List<float>();
            var operators = new List<char>();

            var regex = new Regex(@"(-?\d+\.?\d*|[\*/])");
            foreach (Match match in regex.Matches(token))
            {
                string val = match.Value;
                if (val == "*" || val == "/")
                {
                    operators.Add(val[0]);
                }
                else if (float.TryParse(val, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out float num))
                {
                    numbers.Add(num);
                }
            }

            if (numbers.Count == 0) return token;

            float result = numbers[0];
            for (int i = 0; i < operators.Count; i++)
            {
                float next = numbers[i + 1];
                if (operators[i] == '*')
                    result *= next;
                else if (operators[i] == '/')
                    result = next != 0 ? result / next : 0;
            }

            return result.ToString(CultureInfo.InvariantCulture);
        }

        private static string EvaluateAddSubtract(string expr)
        {
            var numbers = new List<float>();
            var operators = new List<char>();

            var regex = new Regex(@"(-?\d+\.?\d*|[+\-])");
            bool expectNumber = true;

            foreach (Match match in regex.Matches(expr))
            {
                string val = match.Value;
                if ((val == "+" || val == "-") && !expectNumber)
                {
                    operators.Add(val[0]);
                    expectNumber = true;
                }
                else if (float.TryParse(val, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out float num))
                {
                    numbers.Add(num);
                    expectNumber = false;
                }
            }

            if (numbers.Count == 0) return expr;

            float result = numbers[0];
            for (int i = 0; i < operators.Count; i++)
            {
                float next = numbers[i + 1];
                if (operators[i] == '+')
                    result += next;
                else if (operators[i] == '-')
                    result -= next;
            }

            return result.ToString(CultureInfo.InvariantCulture);
        }

        private static List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            int lastIndex = 0;

            for (int i = 1; i < expr.Length; i++)
            {
                if ((expr[i] == '+' || expr[i] == '-') && expr[i - 1] != '*' && expr[i - 1] != '/')
                {
                    tokens.Add(expr.Substring(lastIndex, i - lastIndex));
                    lastIndex = i;
                }
            }

            tokens.Add(expr.Substring(lastIndex));
            return tokens;
        }
    }
}