using System;
using System.Collections.Generic;
using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    public class UnitEvent<T> where T : IUnit<T>
    {
        private Dictionary<string, Action<SkillContext<T>>> _events = new Dictionary<string, Action<SkillContext<T>>>();

        public UnitEvent(params string[] events)
        {
            foreach (var @event in events) _events[@event] = null;
        }

        public void Subscribe(string eventName, Action<SkillContext<T>> action)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("事件名不能为null或空字符串");
                return;
            }

            if (!_events.ContainsKey(eventName))
                _events[eventName] = null;

            _events[eventName] += action;
        }

        public void Unsubscribe(string eventName, Action<SkillContext<T>> action)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_events.ContainsKey(eventName))
                _events[eventName] -= action;
        }

        public void Trigger(string eventName, SkillContext<T> skillContext)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_events.ContainsKey(eventName))
                _events[eventName]?.Invoke(skillContext);
        }

        public void SubscribeMany(string[] eventNames, Action<SkillContext<T>> action)
        {
            foreach (var name in eventNames)
                Subscribe(name, action);
        }

        public int GetSubscriberCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_events.ContainsKey(eventName))
                return 0;

            return _events[eventName]?.GetInvocationList().Length ?? 0;
        }

        public bool HasEvent(string eventName) => _events.ContainsKey(eventName);

        public void ClearEvent(string eventName) => _events.Remove(eventName);
        public void ClearAllEvents() => _events.Clear();
    }
}
