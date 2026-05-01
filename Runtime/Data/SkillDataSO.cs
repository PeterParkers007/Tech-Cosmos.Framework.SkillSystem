using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        // 条件层 - 支持多态选择
        [SerializeReference]
        public List<ConditionBase> Conditions = new();

        // 机制层 - 支持多态选择
        [SerializeReference]
        public List<MechanismBase> Mechanisms = new();

        // 数值层
        [SerializeField]
        private List<DataEntry> serializedData = new();

        [NonSerialized]
        private Dictionary<string, object> _dataCache;

        #region ISerializationCallbackReceiver

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // 序列化前同步缓存到序列化列表
            if (_dataCache != null)
            {
                foreach (var kvp in _dataCache)
                {
                    var existing = serializedData.Find(e => e.key == kvp.Key);
                    if (existing == null)
                    {
                        serializedData.Add(new DataEntry
                        {
                            key = kvp.Key,
                            valueContainer = CreateValueContainer(kvp.Value)
                        });
                    }
                    else
                    {
                        existing.valueContainer = CreateValueContainer(kvp.Value);
                    }
                }
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // 反序列化后清空缓存，下次访问时重新构建
            _dataCache = null;
        }

        #endregion

        /// <summary>
        /// 获取数值字典
        /// </summary>
        public Dictionary<string, object> GetData()
        {
            if (_dataCache == null)
            {
                _dataCache = new Dictionary<string, object>();
                foreach (var entry in serializedData)
                {
                    if (!string.IsNullOrEmpty(entry.key))
                    {
                        _dataCache[entry.key] = entry.GetValue();
                    }
                }
            }
            return _dataCache;
        }

        /// <summary>
        /// 获取指定类型的值
        /// </summary>
        public T GetValue<T>(string key)
        {
            var data = GetData();
            if (data.TryGetValue(key, out var value))
            {
                if (value is T tValue)
                    return tValue;

                // 处理枚举（存储为 int）
                if (typeof(T).IsEnum && value is int intValue)
                    return (T)(object)intValue;

                // 尝试类型转换
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    Debug.LogWarning($"无法将键 [{key}] 的值从 {value?.GetType().Name} 转换为 {typeof(T).Name}");
                }
            }
            return default;
        }

        /// <summary>
        /// 设置数值
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            var data = GetData();

            // 枚举存储为 int
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
        /// 检查是否包含键
        /// </summary>
        public bool ContainsKey(string key)
        {
            return GetData().ContainsKey(key);
        }

        #region 数值容器创建

        private ValueContainer CreateValueContainer(object value)
        {
            if (value == null) return new StringValue { value = "" };
            if (value is float f) return new FloatValue { value = f };
            if (value is double d) return new FloatValue { value = (float)d };
            if (value is int i) return new IntValue { value = i };
            if (value is long l) return new IntValue { value = (int)l };
            if (value is string s) return new StringValue { value = s };
            if (value is bool b) return new BoolValue { value = b };

            // 其他类型转为字符串
            return new StringValue { value = value.ToString() };
        }

        #endregion

        /// <summary>
        /// 创建技能（子类实现）
        /// </summary>
        public abstract object CreateSkill();

        /// <summary>
        /// 获取 Unit 类型
        /// </summary>
        public abstract Type GetUnitType();

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中使用：获取序列化数据（用于 PropertyDrawer）
        /// </summary>
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

            // 转换条件
            foreach (var condition in Conditions)
            {
                if (condition is Condition<T> typedCondition)
                {
                    data.AddCondition(typedCondition);
                }
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

    #endregion
}