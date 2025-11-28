namespace TechCosmos.SkillSystem.Runtime
{
    public class ActiveBaseLayer<T> : BaseLayer<T> where T : IUnit<T>
    {
        public ActiveBaseLayer(string triggerEvent) : base(triggerEvent) { }

        public override void Trigger(SkillContext<T> skillContext)
            => Skill.ExecuteLayer.Execute(skillContext);
    }
}
