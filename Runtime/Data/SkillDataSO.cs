using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能配置 ScriptableObject 基类：序列化条件、机制、时间轴与自定义数据。
    /// </summary>
    public abstract partial class SkillDataSO : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>技能类型（主动/被动）。</summary>
        public SkillType SkillType;
        /// <summary>可触发本技能的事件名列表。</summary>
        public List<string> TriggerEvents = new List<string>() { "OnAttack" };

        /// <summary>技能显示名称。</summary>
        public string SkillName;
        /// <summary>技能描述文本。</summary>
        public string SkillDescription;
        /// <summary>施法配置（读条、引导、优先级等）。</summary>
        public SkillProfile Profile = new();

        [SerializeReference]
        /// <summary>旧版平铺条件列表（条件树未启用时使用）。</summary>
        public List<ConditionBase> Conditions = new();

        /// <summary>是否使用条件树而非平铺列表。</summary>
        public bool useConditionTree = true;

        [SerializeReference]
        /// <summary>条件树根节点。</summary>
        public ConditionTreeNodeBase conditionTreeRoot;

        /// <summary>技能时间轴配置。</summary>
        public SkillTimelineData Timeline = new();

        [SerializeReference]
        /// <summary>旧版平铺机制列表（机制树未启用时使用）。</summary>
        public List<MechanismBase> Mechanisms = new();

        /// <summary>是否使用机制树而非平铺列表。</summary>
        public bool useMechanismTree;

        [SerializeReference]
        /// <summary>机制树根节点。</summary>
        public MechanismTreeNodeBase mechanismTreeRoot;

        [SerializeField]
        private List<DataEntry> serializedData = new();

        [NonSerialized]
        private Dictionary<string, object> _dataCache;

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize() => _dataCache = null;

        /// <summary>获取反序列化后的数据字典（带缓存）。</summary>
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

        /// <summary>按键获取配置值。</summary>
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

        /// <summary>设置配置值并标记脏。</summary>
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

        /// <summary>是否包含指定数据键。</summary>
        public bool ContainsKey(string key) => GetData().ContainsKey(key);

        /// <summary>获取原始序列化数据条目。</summary>
        public List<DataEntry> GetSerializedData() => serializedData;

        /// <summary>获取代码生成器自动注入的属性键集合。</summary>
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
            if (value is RandomValue rv) return rv;
            if (value.GetType().GetCustomAttribute<DataEntryTypeAttribute>() != null)
                return new SerializableValue { value = value };
            return new StringValue { value = value.ToString() };
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (Profile == null) Profile = new SkillProfile();
            if (TriggerEvents == null) TriggerEvents = new List<string>();
        }
#endif

        /// <summary>创建运行时技能实例。</summary>
        public abstract object CreateSkill();
        /// <summary>获取关联的单位类型。</summary>
        public abstract Type GetUnitType();
    }

    /// <summary>
    /// 泛型技能配置 SO：编译条件树并生成 <see cref="SkillData{T}"/>。
    /// </summary>
    public abstract class SkillDataSO<T> : SkillDataSO where T : class, IUnit<T>
    {
        /// <summary>将 SO 配置编译为运行时技能数据。</summary>
        public SkillData<T> GetSkillData()
        {
            List<Condition<T>> runtimeConditions;

            if (useConditionTree && conditionTreeRoot != null)
            {
                runtimeConditions = ConditionTreeCompiler.CompileToConditionList<T>(conditionTreeRoot, null);
                foreach (var leaf in conditionTreeRoot.EnumerateLeafConditions())
                {
                    if (leaf != null && !(leaf is Condition<T>))
                    {
                        Debug.LogWarning(
                            $"[SkillDataSO] 条件树叶子 '{leaf.GetType().Name}' 不是 Condition<{typeof(T).Name}> 类型。");
                    }
                }
            }
            else
            {
                runtimeConditions = ConditionTreeCompiler.CompileToConditionList<T>(null, base.Conditions);
            }

            List<MechanismBase> runtimeMechanisms;
            if (useMechanismTree && mechanismTreeRoot != null)
            {
                runtimeMechanisms = MechanismTreeCompiler.CompileToMechanismList<T>(mechanismTreeRoot, null);
                foreach (var leaf in mechanismTreeRoot.EnumerateLeafMechanisms())
                {
                    if (leaf != null && !(leaf is Mechanism<T>))
                    {
                        Debug.LogWarning(
                            $"[SkillDataSO] 机制树叶子 '{leaf.GetType().Name}' 不是 Mechanism<{typeof(T).Name}> 类型。");
                    }
                }
            }
            else
            {
                runtimeMechanisms = MechanismTreeCompiler.CompileToMechanismList<T>(null, base.Mechanisms);
            }

            return new SkillData<T>
            {
                SkillType = SkillType,
                TriggerEvents = TriggerEvents,
                SkillName = SkillName,
                SkillDescription = SkillDescription,
                Profile = CloneProfile(Profile),
                Conditions = runtimeConditions,
                Mechanisms = runtimeMechanisms,
                Timeline = CloneTimeline(Timeline),
                Data = new Dictionary<string, object>(GetData())
            };
        }

        public override object CreateSkill() => SkillFactory<T>.CreateSkill(this);
        public override Type GetUnitType() => typeof(T);

        private static SkillProfile CloneProfile(SkillProfile source)
        {
            if (source == null) return new SkillProfile();
            return new SkillProfile
            {
                executionPriority = source.executionPriority,
                castTime = source.castTime,
                channelTime = source.channelTime,
                canBeInterrupted = source.canBeInterrupted,
                tags = source.tags != null ? new List<string>(source.tags) : new List<string>()
            };
        }

        private static SkillTimelineData CloneTimeline(SkillTimelineData source)
        {
            if (source == null) return new SkillTimelineData();
            var clone = new SkillTimelineData
            {
                enabled = source.enabled,
                totalDuration = source.totalDuration
            };
            if (source.clips != null)
            {
                foreach (var clip in source.clips)
                {
                    clone.clips.Add(new SkillTimelineClip
                    {
                        label = clip.label,
                        clipType = clip.clipType,
                        startTime = clip.startTime,
                        duration = clip.duration,
                        eventName = clip.eventName,
                        mechanism = clip.mechanism
                    });
                }
            }
            return clone;
        }
    }

    #region 数据条目与值容器

    /// <summary>序列化数据键值对。</summary>
    [Serializable] public class DataEntry { public string key; [SerializeReference] public ValueContainer valueContainer; public object GetValue() => valueContainer?.GetValue(); }
    /// <summary>值容器基类。</summary>
    [Serializable] public abstract class ValueContainer { public abstract object GetValue(); }
    /// <summary>浮点值容器。</summary>
    [Serializable] public class FloatValue : ValueContainer { public float value; public override object GetValue() => value; }
    /// <summary>整数值容器。</summary>
    [Serializable] public class IntValue : ValueContainer { public int value; public override object GetValue() => value; }
    /// <summary>字符串值容器。</summary>
    [Serializable] public class StringValue : ValueContainer { public string value; public override object GetValue() => value; }
    /// <summary>布尔值容器。</summary>
    [Serializable] public class BoolValue : ValueContainer { public bool value; public override object GetValue() => value; }
    /// <summary>公式值容器，支持静态值、引用、表达式与自定义公式。</summary>
    [Serializable]
    public class FormulaValue : ValueContainer
    {
        /// <summary>公式类型。</summary>
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

    /// <summary>随机值容器，运行时求值。</summary>
    [Serializable]
    public class RandomValue : ValueContainer
    {
        /// <summary>随机分布模式。</summary>
        public enum RandomMode { Uniform }
        public RandomMode mode = RandomMode.Uniform;
        public float min = 0f;
        public float max = 1f;
        public bool useInteger = false;
        public override object GetValue() => this;

        public float Resolve()
        {
            if (useInteger)
                return UnityEngine.Random.Range((int)min, (int)max + 1);
            return UnityEngine.Random.Range(min, max);
        }

        public float Resolve(int seed)
        {
            var oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            float result = Resolve();
            UnityEngine.Random.state = oldState;
            return result;
        }
    }

    [Serializable] public class SerializableValue : ValueContainer { [SerializeReference] public object value; public override object GetValue() => value; }

    #endregion
}