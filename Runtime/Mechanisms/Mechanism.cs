namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class Mechanism<T> where T : class, IUnit<T>
    {
        public IDataLayer<T> DataLayer { get; }

        public Mechanism(IDataLayer<T> dataLayer = null) => this.DataLayer = dataLayer;
        public virtual void Execute(SkillContext<T> context){ }
    }
}
