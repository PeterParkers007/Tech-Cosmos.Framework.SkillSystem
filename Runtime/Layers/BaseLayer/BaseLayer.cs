using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 基础层抽象类：管理触发事件与执行优先级。
    /// </summary>
    public abstract class BaseLayer<T> : IBaseLayer<T> where T : class, IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
        public List<string> TriggerEvents { get; set; }
        /// <summary>执行优先级，用于施法打断判定。</summary>
        public int ExecutionPriority { get; private set; }

        private string[] _cachedTriggerEvents = System.Array.Empty<string>();
        private int _cachedTriggerEventsVersion = -1;

        public BaseLayer(List<string> triggerEvents)
        {
            TriggerEvents = triggerEvents ?? new List<string>();
        }

        /// <summary>设置执行优先级。</summary>
        public void SetExecutionPriority(int priority) => ExecutionPriority = priority;

        /// <summary>获取缓存的触发事件数组（避免每次分配）。</summary>
        public string[] GetCachedTriggerEvents()
        {
            var events = TriggerEvents;
            int count = events?.Count ?? 0;
            if (_cachedTriggerEventsVersion == count && _cachedTriggerEvents.Length == count)
                return _cachedTriggerEvents;

            if (count == 0)
            {
                _cachedTriggerEvents = System.Array.Empty<string>();
            }
            else
            {
                _cachedTriggerEvents = new string[count];
                for (int i = 0; i < count; i++)
                    _cachedTriggerEvents[i] = events[i];
            }

            _cachedTriggerEventsVersion = count;
            return _cachedTriggerEvents;
        }

        public virtual void Trigger(SkillContext<T> context) { }
    }
}
