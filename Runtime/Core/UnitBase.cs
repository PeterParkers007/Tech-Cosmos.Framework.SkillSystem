using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能单位基类，集成事件总线、技能容器、Buff 与执行控制器。
    /// </summary>
    public abstract class UnitBase<T> : MonoBehaviour, IUnit<T>, IBuffHost<T>, ISkillExecutionOwner<T>
        where T : class, IUnit<T>
    {
        protected UnitEvent<T> UnitEvent { get; private set; }
        protected SkillHolder<T> SkillHolder { get; private set; }
        /// <summary>单位 Buff 系统（GBF）。</summary>
        public BuffSystem<T> BuffSystem { get; private set; }
        /// <summary>单位标签容器。</summary>
        public TagContainer Tags { get; private set; }
        /// <summary>技能执行状态控制器。</summary>
        public SkillExecutionController<T> ExecutionController { get; private set; }

        protected virtual void Awake()
        {
            Tags = new TagContainer();
            BuffSystem = new BuffSystem<T>((T)(object)this);
            BuffSystem.OnBuffAdded += SyncBuffTagsOnAdd;
            BuffSystem.OnBuffRemoved += SyncBuffTagsOnRemove;
            ExecutionController = new SkillExecutionController<T>();
            UnitEvent = new UnitEvent<T>(GetSupportedEvents());
            SkillHolder = new SkillHolder<T>(UnitEvent);
        }

        protected virtual void Update()
        {
            ExecutionController?.Tick();
            BuffSystem?.BuffUpdate(SkillSystemServices.Clock.DeltaTime);
            SkillTimelineService.Tick(SkillSystemServices.Clock.DeltaTime);
        }

        /// <summary>返回该单位支持的事件名称列表。</summary>
        public abstract string[] GetSupportedEvents();

        /// <summary>使用完整上下文触发指定事件。</summary>
        public void TriggerEvent(string eventName, SkillContext<T> context)
        {
            UnitEvent.Trigger(eventName, context);
        }

        /// <summary>以自身为施法者触发指定事件。</summary>
        public void TriggerEvent(string eventName, T target = null, Vector3 targetPos = default)
        {
            UnitEvent.Trigger(eventName, new SkillContext<T>((T)(object)this, target, targetPos));
        }

        /// <summary>向单位注册技能。</summary>
        public void AddSkill(ISkill<T> skill) => SkillHolder.AddSkill(skill, (T)(object)this);

        /// <summary>从单位移除技能。</summary>
        public void RemoveSkill(ISkill<T> skill) => SkillHolder.RemoveSkill(skill);

        /// <summary>按名称尝试获取技能。</summary>
        public bool TryGetSkill(string name, out ISkill<T> skill) => SkillHolder.TryGetSkill(name, out skill);

        /// <summary>按名称获取技能。</summary>
        public ISkill<T> GetSkill(string name) => SkillHolder.GetSkill(name);

        /// <summary>综合所有 Buff 修改器计算最终属性值。</summary>
        public float EvaluateStat(string statKey, float baseValue)
        {
            return BuffSystem?.GetModifiedValue(statKey, baseValue) ?? baseValue;
        }

        protected virtual void OnDestroy()
        {
            if (BuffSystem != null)
            {
                BuffSystem.OnBuffAdded -= SyncBuffTagsOnAdd;
                BuffSystem.OnBuffRemoved -= SyncBuffTagsOnRemove;
            }

            ExecutionController?.Cancel();
            SkillTimelineService.StopAll();
            BuffSystem?.ClearBuff();
            SkillHolder?.RemoveAllSkills();
        }

        private void SyncBuffTagsOnAdd(IBuff<T> buff)
        {
            if (buff?.tags == null) return;
            for (int i = 0; i < buff.tags.Length; i++)
                Tags.AddTag(buff.tags[i]);
        }

        private void SyncBuffTagsOnRemove(IBuff<T> buff)
        {
            if (buff?.tags == null) return;
            for (int i = 0; i < buff.tags.Length; i++)
                Tags.RemoveTag(buff.tags[i]);
        }
    }
}
