using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 声明机制/条件需要的数据项。
    /// 添加后在编辑器的数值层会自动创建对应条目，删除机制/条件后自动移除。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequiredDataAttribute : Attribute
    {
        /// <summary>数据键名（对应 DataLayer.GetValue 的 key）</summary>
        public string Key { get; }

        /// <summary>数据类型</summary>
        public Type ValueType { get; }

        /// <summary>默认值（字符串形式，float 默认 0，int 默认 0，string 默认 ""，bool 默认 false）</summary>
        public string DefaultValue { get; set; }

        /// <summary>描述文字（显示在数值层条目标题）</summary>
        public string Description { get; set; }

        /// <summary>是否是公式类型（设为 true 则自动创建 FormulaValue）</summary>
        public bool IsFormula { get; set; }

        /// <summary>公式类型（IsFormula=true 时生效）</summary>
        public FormulaValue.FormulaType FormulaType { get; set; } = FormulaValue.FormulaType.Static;

        /// <summary>公式的静态默认值（IsFormula=true 时生效）</summary>
        public float StaticValue { get; set; }

        /// <summary>公式的引用路径（IsFormula=true 且 FormulaType=Reference 时生效）</summary>
        public string ReferencePath { get; set; }

        /// <summary>公式的自定义表达式（IsFormula=true 且 FormulaType=Custom 时生效）</summary>
        public string CustomFormula { get; set; }

        /// <summary>
        /// 允许切换到的类型白名单。
        /// 为 null 或空数组表示允许所有类型。
        /// 例如 new[] { typeof(float), typeof(FormulaValue) } 只允许 Float 和 Formula 切换。
        /// </summary>
        public Type[] AllowedTypes { get; set; }

        public RequiredDataAttribute(string key, Type valueType)
        {
            Key = key;
            ValueType = valueType;
        }
    }
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
