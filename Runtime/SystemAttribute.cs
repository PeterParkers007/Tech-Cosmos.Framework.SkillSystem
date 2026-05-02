using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 标记可以被添加到数据层字典的自定义类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class DataEntryTypeAttribute : Attribute
    {
        /// <summary>
        /// 在菜单中的显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 菜单分类
        /// </summary>
        public string Category { get; set; }

        public DataEntryTypeAttribute() { }

        public DataEntryTypeAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
    /// <summary>
    /// 标记需要生成 SkillDataSO 的类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class GenerateSkillDataSOAttribute : Attribute
    {
        /// <summary>
        /// 菜单路径
        /// </summary>
        public string MenuName { get; set; } = "Tech-Cosmos/Skill";

        /// <summary>
        /// 文件名前缀
        /// </summary>
        public string FileName { get; set; }
    }

    /// <summary>
    /// 标记需要暴露到 SkillDataSO 的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class SkillDataFieldAttribute : Attribute
    {
        /// <summary>
        /// 在 Inspector 中的分类
        /// </summary>
        public string Category { get; set; } = "基础属性";

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 默认值（字符串形式）
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 是否在条件层显示
        /// </summary>
        public bool ShowInCondition { get; set; }

        /// <summary>
        /// 是否在机制层显示
        /// </summary>
        public bool ShowInMechanism { get; set; }
    }

    /// <summary>
    /// 标记 SkillDataSO 应该包含的自定义数据项
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class SkillDataEntryAttribute : Attribute
    {
        public string Key { get; }
        public Type ValueType { get; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }

        public SkillDataEntryAttribute(string key, Type valueType)
        {
            Key = key;
            ValueType = valueType;
        }
    }
}
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class AutoGenerateConditionAttribute : Attribute
{
    public Type[] TargetTypes { get; }
    public AutoGenerateConditionAttribute(params Type[] targetTypes)
    {
        TargetTypes = targetTypes;
    }
}
/// <summary>
/// 标记需要自动生成非泛型子类的泛型机制
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AutoGenerateMechanismAttribute : Attribute
    {
        /// <summary>
        /// 目标 Unit 类型（可选，不指定则为所有 IUnit 类型生成）
        /// </summary>
        public Type[] TargetTypes { get; }

        public AutoGenerateMechanismAttribute(params Type[] targetTypes)
        {
            TargetTypes = targetTypes;
        }
    }
    /// <summary>
    /// 自定义机制在菜单中的分类和显示名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MechanismMenuAttribute : Attribute
    {
        /// <summary>
        /// 菜单分类
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// 显示名称（可选，默认使用类名）
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 排序优先级（越小越靠前）
        /// </summary>
        public int Priority { get; set; }

        public MechanismMenuAttribute(string category)
        {
            Category = category;
        }
    }
    /// <summary>
    /// 标记需要为其生成机制的 IUnit 类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateMechanismsForAttribute : Attribute
    {
    }
