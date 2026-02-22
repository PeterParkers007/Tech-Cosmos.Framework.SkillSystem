namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class Mechanism<T> where T : class, IUnit<T>
    {
        public virtual void Execute(SkillContext<T> context,IDataLayer<T> dataLayer){ }
        public virtual void SkillBack(ISkill<T> skill) { }
    }
}
