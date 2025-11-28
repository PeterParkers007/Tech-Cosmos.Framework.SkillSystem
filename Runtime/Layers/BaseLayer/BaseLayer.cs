namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class BaseLayer<T> : IBaseLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
        public string TriggerEvent { get; set; }

        public BaseLayer(string triggerEvent)
        {
            this.TriggerEvent = triggerEvent;
        }

        public virtual void Trigger(SkillContext<T> context) { }
    }
}
