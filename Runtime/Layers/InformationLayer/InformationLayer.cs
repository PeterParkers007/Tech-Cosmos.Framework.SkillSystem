namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 信息层：存储技能的名称与描述文本。
    /// </summary>
    public class InformationLayer<T> : IInformationLayer<T> where T : class, IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public InformationLayer(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
