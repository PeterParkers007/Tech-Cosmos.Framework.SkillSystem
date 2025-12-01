namespace TechCosmos.SkillSystem.Runtime
{
    public interface IInformationLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
