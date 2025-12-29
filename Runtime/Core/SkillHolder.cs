using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class SkillHolder<T> where T : class, IUnit<T>
    {
        private Dictionary<string, ISkill<T>> skills = new();
        private UnitEvent<T> unitEvent;

        public SkillHolder(UnitEvent<T> unitEvent) => this.unitEvent = unitEvent;

        public void AddSkill(ISkill<T> skill)
        {
            unitEvent.Subscribe(skill.BaseLayer.TriggerEvent, skill.BaseLayer.Trigger);
            skills[skill.InformationLayer.Name] = skill;
        }

        public void RemoveSkill(ISkill<T> skill)
        {
            unitEvent.Unsubscribe(skill.BaseLayer.TriggerEvent, skill.BaseLayer.Trigger);
            if(skills.ContainsKey(skill.InformationLayer.Name)) skills.Remove(skill.InformationLayer.Name);
        }
    }
}
