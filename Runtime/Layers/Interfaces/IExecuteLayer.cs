namespace TechCosmos.SkillSystem.Runtime
{
    public interface IExecuteLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public void Execute(SkillContext<T> skillContext);
    }
}
