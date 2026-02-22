using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    public class CooldownCondition<T> : Condition<T> where T : class, IUnit<T>
    {
        public float cooldown;
        private float _nextAvailableTime;

        public CooldownCondition(float cooldown, SkillData<T> skillData = null)
        {
            this.cooldown = cooldown;
            if (skillData != null) skillData.AddMechanism(StartCooldown);
        }

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
            => UnityEngine.Time.time >= _nextAvailableTime;

        public void StartCooldown(SkillContext<T> skillContext = default)
            => _nextAvailableTime = UnityEngine.Time.time + cooldown;
    }
}
