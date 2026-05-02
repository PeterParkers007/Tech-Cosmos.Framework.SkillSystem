using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能数据 ScriptableObject 基类（非泛型）
    /// </summary>
    public abstract class SkillDataSO : ScriptableObject, ISerializationCallbackReceiver
    {
        // 基础层
        public SkillType SkillType;
        public string TriggerEvent = string.Empty;

        // 信息层
        public string SkillName;
        public string SkillDescription;

        // 条件层
        [SerializeReference]
        public List<ConditionBase> Conditions = new();

        // 机制层
        [SerializeReference]
        public List<MechanismBase> Mechanisms = new();

        // 数值层 - 所有数据（手动 + 生成属性）
        [SerializeField]
        private List<DataEntry> serializedData = new();

        // 缓存
        [NonSerialized]
        private Dictionary<string, object> _dataCache;

        #region ISerializationCallbackReceiver

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _dataCache = null;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取完整数据字典
        /// </summary>
        public Dictionary<string, object> GetData()
        {
            if (_dataCache == null)
            {
                _dataCache = new Dictionary<string, object>();

                if (serializedData != null)
                {
                    foreach (var entry in serializedData)
                    {
                        if (!string.IsNullOrEmpty(entry.key))
                        {
                            _dataCache[entry.key] = entry.GetValue();
                        }
                    }
                }
            }
            return _dataCache;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        public T GetValue<T>(string key)
        {
            var data = GetData();
            if (data.TryGetValue(key, out var value))
            {
                if (value is T tValue)
                    return tValue;

                if (typeof(T).IsEnum && value is int intValue)
                    return (T)(object)intValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { }
            }
            return default;
        }

        /// <summary>
        /// 设置值（生成属性用）
        /// </summary>
        protected void SetGeneratedValue<T>(string key, T value)
        {
            SetValueInternal(key, value is Enum e ? Convert.ToInt32(e) : (object)value);
        }

        /// <summary>
        /// 手动设置值（显示在 Data 字典区域）
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            SetValueInternal(key, value is Enum e ? Convert.ToInt32(e) : (object)value);
        }

        /// <summary>
        /// 内部设值
        /// </summary>
        private void SetValueInternal(string key, object storeValue)
        {
            if (_dataCache != null)
                _dataCache[key] = storeValue;

            if (serializedData == null)
                serializedData = new List<DataEntry>();

            var entry = serializedData.Find(e => e.key == key);
            if (entry == null)
            {
                serializedData.Add(new DataEntry
                {
                    key = key,
                    valueContainer = CreateValueContainer(storeValue)
                });
            }
            else
            {
                entry.valueContainer = CreateValueContainer(storeValue);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public bool ContainsKey(string key)
        {
            return GetData().ContainsKey(key);
        }

        #endregion

        #region Editor 用

        /// <summary>
        /// 获取序列化数据列表（Editor 用）
        /// </summary>
        public List<DataEntry> GetSerializedData() => serializedData;

        /// <summary>
        /// 获取生成属性的 key 集合（Editor 用于过滤）
        /// </summary>
        public HashSet<string> GetGeneratedKeys()
        {
            var keys = new HashSet<string>();
            var soType = GetType();
            var props = soType.GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in props)
            {
                var tooltip = prop.GetCustomAttributes(typeof(TooltipAttribute), false)
                    .FirstOrDefault() as TooltipAttribute;
                if (tooltip != null)
                {
                    // 从 Tooltip 无法直接获取 DataKey，直接用属性名
                    keys.Add(prop.Name);
                }
            }
            return keys;
        }

        #endregion

        #region 内部方法

        private ValueContainer CreateValueContainer(object value)
        {
            if (value == null) return new StringValue { value = "" };
            if (value is float f) return new FloatValue { value = f };
            if (value is double d) return new FloatValue { value = (float)d };
            if (value is int i) return new IntValue { value = i };
            if (value is long l) return new IntValue { value = (int)l };
            if (value is string s) return new StringValue { value = s };
            if (value is bool b) return new BoolValue { value = b };
            if (value is FormulaValue formula) return formula;

            // 复杂类型用 ObjectValue
            return new ObjectValue { value = value };
        }

        #endregion

        public abstract object CreateSkill();
        public abstract Type GetUnitType();
    }

    /// <summary>
    /// 泛型技能数据 SO
    /// </summary>
    public abstract class SkillDataSO<T> : SkillDataSO where T : class, IUnit<T>
    {
        public SkillData<T> GetSkillData()
        {
            var data = new SkillData<T>
            {
                SkillType = SkillType,
                TriggerEvent = TriggerEvent,
                SkillName = SkillName,
                SkillDescription = SkillDescription,
                Mechanisms = new List<MechanismBase>(Mechanisms),
                Data = GetData()
            };

            foreach (var condition in Conditions)
            {
                if (condition is Condition<T> typedCondition)
                    data.AddCondition(typedCondition);
            }

            return data;
        }

        public override object CreateSkill() => SkillFactory<T>.CreateSkill(GetSkillData());

        public override Type GetUnitType() => typeof(T);
    }

    #region 数值容器类型

    [Serializable]
    public class DataEntry
    {
        public string key;
        [SerializeReference]
        public ValueContainer valueContainer;
        public object GetValue() => valueContainer?.GetValue();
    }

    [Serializable]
    public abstract class ValueContainer
    {
        public abstract object GetValue();
    }

    [Serializable]
    public class FloatValue : ValueContainer
    {
        public float value;
        public override object GetValue() => value;
    }

    [Serializable]
    public class IntValue : ValueContainer
    {
        public int value;
        public override object GetValue() => value;
    }

    [Serializable]
    public class StringValue : ValueContainer
    {
        public string value;
        public override object GetValue() => value;
    }

    [Serializable]
    public class BoolValue : ValueContainer
    {
        public bool value;
        public override object GetValue() => value;
    }

    [Serializable]
    public class ObjectValue : ValueContainer
    {
        [SerializeReference]
        public object value;
        public override object GetValue() => value;
    }

    [Serializable]
    public class FormulaValue : ValueContainer
    {
        public enum FormulaType { Static, Reference, Expression, Custom }

        public FormulaType formulaType = FormulaType.Static;
        public float staticValue;
        public string referencePath;
        public float multiplier = 1f;
        public float offset;
        public string operatorType = "Multiply";
        public string customFormula;

        public override object GetValue() => this;
    }

    #endregion
}