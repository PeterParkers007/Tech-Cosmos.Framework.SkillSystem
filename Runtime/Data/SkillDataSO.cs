using System;
using System.Collections.Generic;
using UnityEngine;
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

        // 数值层 - 手动添加的数据（序列化到列表）
        [SerializeField]
        private List<DataEntry> serializedData = new();

        // 生成属性专用的不序列化字典
        [NonSerialized]
        protected Dictionary<string, object> _generatedValues = new();

        // 缓存
        [NonSerialized]
        private Dictionary<string, object> _dataCache;

        #region ISerializationCallbackReceiver

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // 序列化前不需要特殊处理，serializedData 已经是列表形式
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _dataCache = null;
        }

        #endregion

        /// <summary>
        /// 获取完整数据字典（序列化数据 + 生成属性数据）
        /// </summary>
        public Dictionary<string, object> GetData()
        {
            if (_dataCache == null)
            {
                _dataCache = new Dictionary<string, object>();

                // 1. 加载手动添加的序列化数据
                foreach (var entry in serializedData)
                {
                    if (!string.IsNullOrEmpty(entry.key))
                    {
                        _dataCache[entry.key] = entry.GetValue();
                    }
                }

                // 2. 加载生成属性的默认值（不覆盖已有键）
                if (_generatedValues != null)
                {
                    foreach (var kvp in _generatedValues)
                    {
                        if (!_dataCache.ContainsKey(kvp.Key))
                        {
                            _dataCache[kvp.Key] = kvp.Value;
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
                catch
                {
                    Debug.LogWarning($"无法转换键 [{key}]: {value?.GetType()} -> {typeof(T)}");
                }
            }
            return default;
        }

        /// <summary>
        /// 设置值（生成属性用，只更新缓存和 generatedValues，不写序列化列表）
        /// </summary>
        protected void SetGeneratedValue<T>(string key, T value)
        {
            if (_generatedValues == null)
                _generatedValues = new Dictionary<string, object>();

            object storeValue = value is Enum e ? Convert.ToInt32(e) : (object)value;
            _generatedValues[key] = storeValue;

            // 同步到缓存
            if (_dataCache != null)
                _dataCache[key] = storeValue;
        }

        /// <summary>
        /// 手动设置值（写入序列化列表）
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            var data = GetData();
            object storeValue = value is Enum e ? Convert.ToInt32(e) : (object)value;
            data[key] = storeValue;

            // 同步到序列化列表
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

        private ValueContainer CreateValueContainer(object value)
        {
            if (value == null) return new StringValue { value = "" };
            if (value is float f) return new FloatValue { value = f };
            if (value is double d) return new FloatValue { value = (float)d };
            if (value is int i) return new IntValue { value = i };
            if (value is long l) return new IntValue { value = (int)l };
            if (value is string s) return new StringValue { value = s };
            if (value is bool b) return new BoolValue { value = b };
            return new StringValue { value = value.ToString() };
        }

        public abstract object CreateSkill();
        public abstract Type GetUnitType();

#if UNITY_EDITOR
        public List<DataEntry> GetSerializedData() => serializedData;
#endif
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

        public override object CreateSkill()
        {
            return SkillFactory<T>.CreateSkill(GetSkillData());
        }

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