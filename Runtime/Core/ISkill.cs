namespace TechCosmos.SkillSystem.Runtime
{
    public interface ISkill<T> where T : class, IUnit<T>
    {
        IBaseLayer<T> BaseLayer { get; }
        IConditionLayer<T> ConditionLayer { get; }
        IInformationLayer<T> InformationLayer { get; }
        IMechanismLayer<T> MechanismLayer { get; }
        IDataLayer<T> DataLayer { get; }
        IExecuteLayer<T> ExecuteLayer { get; }
    }
}
