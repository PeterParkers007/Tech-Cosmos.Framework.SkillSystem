namespace TechCosmos.SkillSystem.Runtime
{
    public interface IBaseLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public string TriggerEvent { get; set; }
        public void Trigger(SkillContext<T> context);
    }
}
