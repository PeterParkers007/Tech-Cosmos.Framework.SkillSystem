namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class Mechanism<T> where T : class, IUnit<T>
    {
        public SkillContext<T> Context { get; }
        public IDataLayer<T> DataLayer { get; }

        public Mechanism(SkillContext<T> context, IDataLayer<T> dataLayer = null)
        {
            this.Context = context;
            this.DataLayer = dataLayer;
        }
        public virtual void Execute(SkillContext<T> context){ }
        public virtual void SkillBack(ISkill<T> skill) { }
    }
}
