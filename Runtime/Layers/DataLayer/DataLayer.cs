using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 数据层：存储技能参数，支持公式、随机值与委托求值。
    /// </summary>
    public class DataLayer<T> : IDataLayer<T> where T : class, IUnit<T>
    {
        private Dictionary<string, object> _data = new();
        public ISkill<T> Skill { get; set; }

        public DataLayer(Dictionary<string, object> data) => _data = data ?? new Dictionary<string, object>();

        public bool ContainsKey(string key) => _data.ContainsKey(key);

        public bool TryGetValue<TValue>(string key, SkillContext<T> context, out TValue value)
        {
            if (!_data.ContainsKey(key))
            {
                value = default;
                return false;
            }

            var rawValue = _data[key];

            if (rawValue is FormulaValue formulaVal)
            {
                value = ResolveFormula<TValue>(formulaVal, context);
                return true;
            }

            if (rawValue is RandomValue randomVal)
            {
                float randomResult = randomVal.Resolve();
                value = ConvertValue<TValue>(randomResult);
                return true;
            }

            if (rawValue is Func<SkillContext<T>, TValue> func)
            {
                value = func(context);
                return true;
            }

            if (rawValue is TValue typedValue)
            {
                value = typedValue;
                return true;
            }

            try
            {
                value = (TValue)Convert.ChangeType(rawValue, typeof(TValue));
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public TValue GetValue<TValue>(string key, SkillContext<T> context)
        {
            if (TryGetValue(key, context, out TValue value))
                return value;

            Debug.LogWarning($"[DataLayer] 找不到数据键 '{key}'");
            return default;
        }

        private TValue ResolveFormula<TValue>(FormulaValue formula, SkillContext<T> context)
        {
            switch (formula.formulaType)
            {
                case FormulaValue.FormulaType.Static:
                    return ConvertValue<TValue>(formula.staticValue);

                case FormulaValue.FormulaType.Reference:
                    float refValue = FormulaEvaluator.Evaluate<T>(context, formula.referencePath);
                    refValue = ApplyOperator(refValue, formula);
                    return ConvertValue<TValue>(refValue);

                case FormulaValue.FormulaType.Expression:
                    float baseValue = 0f;
                    if (!string.IsNullOrEmpty(formula.referencePath))
                        baseValue = FormulaEvaluator.Evaluate<T>(context, formula.referencePath);
                    baseValue = ApplyOperator(baseValue, formula);
                    return ConvertValue<TValue>(baseValue);

                case FormulaValue.FormulaType.Custom:
                    float customValue = FormulaEvaluator.Evaluate<T>(context, formula.customFormula);
                    return ConvertValue<TValue>(customValue);

                default:
                    return default;
            }
        }

        private float ApplyOperator(float value, FormulaValue formula)
        {
            return formula.operatorType switch
            {
                "Multiply" => value * formula.multiplier + formula.offset,
                "Add" => value + formula.offset,
                "Set" => formula.staticValue,
                _ => value
            };
        }

        private TValue ConvertValue<TValue>(float value)
        {
            if (typeof(TValue) == typeof(float)) return (TValue)(object)value;
            if (typeof(TValue) == typeof(int)) return (TValue)(object)Mathf.RoundToInt(value);
            if (typeof(TValue) == typeof(bool)) return (TValue)(object)(value != 0f);
            if (typeof(TValue) == typeof(double)) return (TValue)(object)(double)value;
            if (typeof(TValue) == typeof(long)) return (TValue)(object)(long)value;
            if (typeof(TValue) == typeof(string)) return (TValue)(object)value.ToString("F2");

            try { return (TValue)Convert.ChangeType(value, typeof(TValue)); }
            catch { return default; }
        }

        public void SetValue<TValue>(string key, TValue value) => _data[key] = value;

        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula)
            => _data[key] = formula;
    }
}