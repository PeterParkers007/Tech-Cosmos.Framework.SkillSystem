namespace TechCosmos.SkillSystem.Runtime
{
    public class InformationLayer<T> : IInformationLayer<T> where T : IUnit<T>
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
