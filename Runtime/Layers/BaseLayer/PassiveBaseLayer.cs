namespace TechCosmos.SkillSystem.Runtime
{
    public class PassiveBaseLayer<T> : BaseLayer<T> where T : class, IUnit<T>
    {
        public PassiveBaseLayer(string triggerEvent) : base(triggerEvent) { }

        public override void Trigger(SkillContext<T> context)
            => Skill.ExecuteLayer.Execute(context);
    }
}
