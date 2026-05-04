using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    public class DataLayer<T> : IDataLayer<T> where T : class, IUnit<T>
    {
        private Dictionary<string, object> _data = new();
        public ISkill<T> Skill { get; set; }

        public DataLayer(Dictionary<string, object> data) => _data = data ?? new Dictionary<string, object>();

        public TValue GetValue<TValue>(string key, SkillContext<T> context)
        {
            if (!_data.ContainsKey(key))
            {
                Debug.LogWarning($"未找到数据键 [{key}]");
                return default;
            }

            var value = _data[key];

            // 公式类型
            if (value is FormulaValue formulaVal)
            {
                return ResolveFormula<TValue>(formulaVal, context);
            }

            // 委托公式
            if (value is Func<SkillContext<T>, TValue> func)
                return func(context);

            // 直接类型匹配
            if (value is TValue typedValue)
                return typedValue;

            // 尝试类型转换
            try
            {
                return (TValue)Convert.ChangeType(value, typeof(TValue));
            }
            catch
            {
                return default;
            }
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
                    float baseValue = formula.staticValue;
                    if (!string.IsNullOrEmpty(formula.referencePath))
                    {
                        baseValue = FormulaEvaluator.Evaluate<T>(context, formula.referencePath);
                    }
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

        /// <summary>
        /// 安全转换值类型
        /// </summary>
        private TValue ConvertValue<TValue>(float value)
        {
            if (typeof(TValue) == typeof(float))
                return (TValue)(object)value;
            if (typeof(TValue) == typeof(int))
                return (TValue)(object)(int)value;
            if (typeof(TValue) == typeof(bool))
                return (TValue)(object)(value != 0f);

            // 默认尝试转换
            try
            {
                return (TValue)Convert.ChangeType(value, typeof(TValue));
            }
            catch
            {
                return default;
            }
        }

        public void SetValue<TValue>(string key, TValue value) => _data[key] = value;

        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula)
            => _data[key] = formula;
    }
}