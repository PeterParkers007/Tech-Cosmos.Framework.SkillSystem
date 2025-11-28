namespace TechCosmos.SkillSystem.Runtime
{
    public class ExecuteLayer<T> : IExecuteLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }

        public void Execute(SkillContext<T> skillContext)
        {
            if (Skill.ConditionLayer.CheckCondition(skillContext))
                Skill.MechanismLayer.Mechanism(skillContext);
        }
    }
}
