using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 单位技能容器，管理技能的注册、触发订阅与生命周期。
    /// </summary>
    public class SkillHolder<T> where T : class, IUnit<T>
    {
        private readonly Dictionary<string, ISkill<T>> skills = new();
        private readonly UnitEvent<T> unitEvent;

        /// <summary>创建技能容器并绑定单位事件总线。</summary>
        public SkillHolder(UnitEvent<T> unitEvent) => this.unitEvent = unitEvent;

        /// <summary>已注册的技能字典（只读）。</summary>
        public IReadOnlyDictionary<string, ISkill<T>> Skills => skills;

        /// <summary>
        /// 注册技能并订阅其触发事件；被动技能会立即执行一次。
        /// </summary>
        /// <param name="skill">要添加的技能。</param>
        /// <param name="caster">施法者，被动技能激活时需要。</param>
        public void AddSkill(ISkill<T> skill, T caster = null)
        {
            if (skill == null) return;

            var name = ResolveSkillName(skill);
            if (skills.ContainsKey(name))
                Debug.LogWarning($"[SkillHolder] 技能名称 '{name}' 已存在，将被覆盖。");

            var baseLayer = skill.BaseLayer as BaseLayer<T>;
            var triggerEvents = baseLayer?.GetCachedTriggerEvents();
            var priority = skill is Skill<T> concrete ? concrete.Profile.executionPriority : 0;

            if (triggerEvents != null && triggerEvents.Length > 0)
                unitEvent.SubscribeMany(triggerEvents, skill.BaseLayer.Trigger, priority);

            if (skill.BaseLayer is PassiveBaseLayer<T> && caster != null)
            {
                var ctx = new SkillContext<T>(caster, caster).WithSkill(skill);
                skill.ExecuteLayer.Execute(ctx);
            }

            skills[name] = skill;
        }

        /// <summary>移除技能，取消事件订阅并清理资源。</summary>
        public void RemoveSkill(ISkill<T> skill)
        {
            if (skill == null) return;

            var baseLayer = skill.BaseLayer as BaseLayer<T>;
            var triggerEvents = baseLayer?.GetCachedTriggerEvents();
            if (triggerEvents != null && triggerEvents.Length > 0)
                unitEvent.Unsubscribe(triggerEvents, skill.BaseLayer.Trigger);

            if (skill is Skill<T> concreteSkill)
                concreteSkill.OnRemove();
            else
                ResourceLayerExtension.RemoveResourceLayer(skill);

            var name = skill.InformationLayer?.Name;
            if (!string.IsNullOrEmpty(name) && skills.ContainsKey(name))
                skills.Remove(name);
        }

        /// <summary>按名称尝试获取技能。</summary>
        public bool TryGetSkill(string name, out ISkill<T> skill) => skills.TryGetValue(name, out skill);

        /// <summary>按名称获取技能，不存在时返回 null。</summary>
        public ISkill<T> GetSkill(string name) => skills.GetValueOrDefault(name);

        /// <summary>检查是否已注册指定名称的技能。</summary>
        public bool HasSkill(string name) => skills.ContainsKey(name);

        /// <summary>移除所有已注册技能。</summary>
        public void RemoveAllSkills()
        {
            var allSkills = new List<ISkill<T>>(skills.Values);
            foreach (var skill in allSkills)
                RemoveSkill(skill);
        }

        private static string ResolveSkillName(ISkill<T> skill)
        {
            var name = skill.InformationLayer?.Name;
            if (!string.IsNullOrEmpty(name)) return name;
            var generated = $"Skill_{skill.GetHashCode()}";
            Debug.LogWarning($"[SkillHolder] 技能缺少名称，使用临时键 '{generated}'。");
            return generated;
        }
    }
}
