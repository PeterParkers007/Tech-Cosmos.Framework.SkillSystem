namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class BaseMechanism<T> where T : class, IUnit<T>
    {
        public SkillContext<T> Context { get; }
        public IDataLayer<T> DataLayer { get; }

        public BaseMechanism(SkillContext<T> context, IDataLayer<T> dataLayer = null)
        {
            this.Context = context;
            this.DataLayer = dataLayer;
        }
    }
}
