using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class SkillHolder<T> where T : class, IUnit<T>
    {
        private List<ISkill<T>> skillList = new();
        private UnitEvent<T> unitEvent;

        public SkillHolder(UnitEvent<T> unitEvent) => this.unitEvent = unitEvent;

        public void AddSkill(ISkill<T> skill)
        {
            unitEvent.Subscribe(skill.BaseLayer.TriggerEvent, skill.BaseLayer.Trigger);
            skillList.Add(skill);
        }

        public void RemoveSkill(ISkill<T> skill)
        {
            unitEvent.Unsubscribe(skill.BaseLayer.TriggerEvent, skill.BaseLayer.Trigger);
            skillList.Remove(skill);
        }
    }
}
