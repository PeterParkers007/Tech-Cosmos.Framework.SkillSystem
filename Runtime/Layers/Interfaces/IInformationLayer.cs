namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 信息层接口：技能的展示名称与描述。
    /// </summary>
    public interface IInformationLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        /// <summary>技能名称。</summary>
        public string Name { get; set; }
        /// <summary>技能描述。</summary>
        public string Description { get; set; }
    }
}
