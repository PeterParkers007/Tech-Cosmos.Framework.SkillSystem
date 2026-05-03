using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class SkillDataSO : ScriptableObject, ISerializationCallbackReceiver
    {
        public SkillType SkillType;
        public string TriggerEvent = "OnAttack";

        public string SkillName;
        public string SkillDescription;

        [SerializeReference]
        public List<ConditionBase> Conditions = new();

        [SerializeReference]
        public List<MechanismBase> Mechanisms = new();

        [SerializeField]
        private List<DataEntry> serializedData = new();

        [NonSerialized]
        private Dictionary<string, object> _dataCache;

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize() => _dataCache = null;

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
                            _dataCache[entry.key] = entry.GetValue();
                    }
                }
            }
            return _dataCache;
        }

        public T GetValue<T>(string key)
        {
            var data = GetData();
            if (data.TryGetValue(key, out var value))
            {
                if (value is T tValue) return tValue;
                if (typeof(T).IsEnum && value is int iv) return (T)(object)iv;
                try { return (T)Convert.ChangeType(value, typeof(T)); } catch { }
            }
            return default;
        }

        protected void SetGeneratedValue<T>(string key, T value)
        {
            SetValueInternal(key, value is Enum e ? Convert.ToInt32(e) : (object)value);
        }

        public void SetValue<T>(string key, T value)
        {
            SetValueInternal(key, value is Enum e ? Convert.ToInt32(e) : (object)value);
        }

        private void SetValueInternal(string key, object storeValue)
        {
            if (_dataCache != null) _dataCache[key] = storeValue;
            if (serializedData == null) serializedData = new List<DataEntry>();

            var entry = serializedData.Find(e => e.key == key);
            if (entry == null)
                serializedData.Add(new DataEntry { key = key, valueContainer = CreateValueContainer(storeValue) });
            else
                entry.valueContainer = CreateValueContainer(storeValue);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public bool ContainsKey(string key) => GetData().ContainsKey(key);

        public List<DataEntry> GetSerializedData() => serializedData;

        public HashSet<string> GetGeneratedKeys()
        {
            var keys = new HashSet<string>();
            try
            {
                foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attrs = prop.GetCustomAttributes(typeof(TooltipAttribute), false);
                    if (attrs.Length > 0)
                    {
                        keys.Add(prop.Name);
                        keys.Add(char.ToLower(prop.Name[0]) + prop.Name.Substring(1));
                        keys.Add(char.ToUpper(prop.Name[0]) + prop.Name.Substring(1));
                    }
                }
            }
            catch { }
            return keys;
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
            if (value is FormulaValue fv) return fv;
            if (value.GetType().GetCustomAttribute<DataEntryTypeAttribute>() != null)
                return new SerializableValue { value = value };
            return new StringValue { value = value.ToString() };
        }

        public abstract object CreateSkill();
        public abstract Type GetUnitType();
    }

    public abstract class SkillDataSO<T> : SkillDataSO where T : class, IUnit<T>
    {
        public SkillData<T> GetSkillData()
        {
            // 关键修复：从基类的 Conditions 列表中筛选并转换出泛型条件
            var runtimeConditions = new List<Condition<T>>();
            if (base.Conditions != null)
            {
                foreach (var conditionBase in base.Conditions)
                {
                    if (conditionBase is Condition<T> typedCondition)
                    {
                        runtimeConditions.Add(typedCondition);
                    }
                    else if (conditionBase != null)
                    {
                        Debug.LogWarning(
                            $"[SkillDataSO] 跳过条件 '{conditionBase.GetType().Name}'，" +
                            $"因为它不是 Condition<{typeof(T).Name}> 类型，无法用于 {GetType().Name}。");
                    }
                }
            }

            // 同理，也要确保 Mechanisms 是正确转换的
            var runtimeMechanisms = new List<MechanismBase>();
            if (base.Mechanisms != null)
            {
                foreach (var mechanismBase in base.Mechanisms)
                {
                    if (mechanismBase is Mechanism<T> typedMechanism)
                    {
                        runtimeMechanisms.Add(typedMechanism);
                    }
                    else if (mechanismBase != null)
                    {
                        Debug.LogWarning(
                            $"[SkillDataSO] 跳过机制 '{mechanismBase.GetType().Name}'，" +
                            $"因为它不是 Mechanism<{typeof(T).Name}> 类型，无法用于 {GetType().Name}。");
                    }
                }
            }

            // 同时从泛型基类的 Conditions 中获取直接添加的泛型条件
            // （如果你的生成代码或手动代码直接向 SkillDataSO<T> 添加了 Condition<T>）
            // 注意：这里需要检查你的具体 SkillDataSO<T> 子类是否有额外字段

            return new SkillData<T>
            {
                SkillType = SkillType,
                TriggerEvent = TriggerEvent,
                SkillName = SkillName,
                SkillDescription = SkillDescription,

                // 使用转换后的列表
                Conditions = runtimeConditions,
                Mechanisms = runtimeMechanisms,
                Data = GetData()
            };
        }

        public override object CreateSkill() => SkillFactory<T>.CreateSkill(GetSkillData());
        public override Type GetUnitType() => typeof(T);
    }

    #region 数值容器

    [Serializable] public class DataEntry { public string key; [SerializeReference] public ValueContainer valueContainer; public object GetValue() => valueContainer?.GetValue(); }
    [Serializable] public abstract class ValueContainer { public abstract object GetValue(); }
    [Serializable] public class FloatValue : ValueContainer { public float value; public override object GetValue() => value; }
    [Serializable] public class IntValue : ValueContainer { public int value; public override object GetValue() => value; }
    [Serializable] public class StringValue : ValueContainer { public string value; public override object GetValue() => value; }
    [Serializable] public class BoolValue : ValueContainer { public bool value; public override object GetValue() => value; }
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
    [Serializable] public class SerializableValue : ValueContainer { [SerializeReference] public object value; public override object GetValue() => value; }

    #endregion
}