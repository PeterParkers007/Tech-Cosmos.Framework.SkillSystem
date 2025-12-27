using System;
using System.Collections.Generic;
using UnityEngine;
 
namespace TechCosmos.SkillSystem.Runtime
{
    public class UnitEvent<T> where T : class, IUnit<T>
    {
        // 改用List存储监听者
        private Dictionary<string, List<Action<SkillContext<T>>>> _listeners
            = new Dictionary<string, List<Action<SkillContext<T>>>>(StringComparer.Ordinal);

        // 缓存数组，避免每次ToArray
        private Dictionary<string, Action<SkillContext<T>>[]> _cachedArrays
            = new Dictionary<string, Action<SkillContext<T>>[]>(StringComparer.Ordinal);

        public UnitEvent(params string[] events)
        {
            foreach (var @event in events)
            {
                _listeners[@event] = new List<Action<SkillContext<T>>>(4);
            }
        }

        public void Subscribe(string eventName, Action<SkillContext<T>> action)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("事件名不能为null或空字符串");
                return;
            }

            if (!_listeners.TryGetValue(eventName, out var listenerList))
            {
                listenerList = new List<Action<SkillContext<T>>>(4);
                _listeners[eventName] = listenerList;
            }

            // 避免重复订阅
            if (!listenerList.Contains(action))
            {
                listenerList.Add(action);
                InvalidateCache(eventName);
            }
        }

        public void Unsubscribe(string eventName, Action<SkillContext<T>> action)
        {
            if (string.IsNullOrEmpty(eventName) || !_listeners.TryGetValue(eventName, out var listenerList))
                return;

            if (listenerList.Remove(action))
            {
                InvalidateCache(eventName);
            }
        }

        public void Trigger(string eventName, SkillContext<T> skillContext)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            var actions = GetCachedActions(eventName);
            if (actions == null)
                return;

            // 遍历数组执行，无委托链GC
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i]?.Invoke(skillContext);
            }
        }

        // 保持原有接口，但内部优化
        public void SubscribeMany(string[] eventNames, Action<SkillContext<T>> action)
        {
            foreach (var name in eventNames)
                Subscribe(name, action);
        }

        public int GetSubscriberCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_listeners.TryGetValue(eventName, out var list))
                return 0;
            return list.Count;
        }

        public bool HasEvent(string eventName) => _listeners.ContainsKey(eventName);

        public void ClearEvent(string eventName)
        {
            if (_listeners.Remove(eventName))
            {
                _cachedArrays.Remove(eventName);
            }
        }

        public void ClearAllEvents()
        {
            _listeners.Clear();
            _cachedArrays.Clear();
        }

        // 私有辅助方法
        private Action<SkillContext<T>>[] GetCachedActions(string eventName)
        {
            if (!_listeners.TryGetValue(eventName, out var list) || list.Count == 0)
                return null;

            // 如果缓存不存在或大小不匹配，重新创建
            if (!_cachedArrays.TryGetValue(eventName, out var cachedArray) ||
                cachedArray.Length != list.Count)
            {
                cachedArray = new Action<SkillContext<T>>[list.Count];
                list.CopyTo(cachedArray);
                _cachedArrays[eventName] = cachedArray;
            }

            return cachedArray;
        }

        private void InvalidateCache(string eventName)
        {
            _cachedArrays.Remove(eventName);
        }
    }
}