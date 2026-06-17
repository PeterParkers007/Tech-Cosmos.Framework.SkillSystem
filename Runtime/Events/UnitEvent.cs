using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 单位事件总线：按事件名订阅/触发，支持优先级排序与回调缓存。
    /// </summary>
    public class UnitEvent<T> where T : class, IUnit<T>
    {
        private sealed class ListenerEntry
        {
            public Action<SkillContext<T>> callback;
            public int priority;
        }

        private readonly Dictionary<string, List<ListenerEntry>> _listeners =
            new Dictionary<string, List<ListenerEntry>>(StringComparer.Ordinal);

        private readonly Dictionary<string, Action<SkillContext<T>>[]> _cachedArrays =
            new Dictionary<string, Action<SkillContext<T>>[]>(StringComparer.Ordinal);

        /// <summary>当前触发嵌套深度，用于检测递归过深。</summary>
        private int _reentrancyDepth;

        /// <summary>预注册事件名列表。</summary>
        public UnitEvent(params string[] events)
        {
            foreach (var @event in events)
                _listeners[@event] = new List<ListenerEntry>(4);
        }

        /// <summary>订阅指定事件；同一回调重复订阅时仅更新优先级。</summary>
        public void Subscribe(string eventName, Action<SkillContext<T>> action, int priority = 0)
        {
            if (string.IsNullOrEmpty(eventName) || action == null)
            {
                Debug.LogWarning("[UnitEvent] 订阅失败：事件名或回调为空");
                return;
            }

            if (!_listeners.TryGetValue(eventName, out var listenerList))
            {
                listenerList = new List<ListenerEntry>(4);
                _listeners[eventName] = listenerList;
            }

            for (int i = 0; i < listenerList.Count; i++)
            {
                if (listenerList[i].callback == action)
                {
                    if (listenerList[i].priority != priority)
                    {
                        listenerList[i].priority = priority;
                        SortListeners(listenerList);
                        InvalidateCache(eventName);
                    }
                    return;
                }
            }

            listenerList.Add(new ListenerEntry { callback = action, priority = priority });
            SortListeners(listenerList);
            InvalidateCache(eventName);
        }

        /// <summary>取消订阅指定事件的回调。</summary>
        public void Unsubscribe(string eventName, Action<SkillContext<T>> action)
        {
            if (string.IsNullOrEmpty(eventName) || !_listeners.TryGetValue(eventName, out var listenerList))
                return;

            for (int i = listenerList.Count - 1; i >= 0; i--)
            {
                if (listenerList[i].callback == action)
                {
                    listenerList.RemoveAt(i);
                    InvalidateCache(eventName);
                    return;
                }
            }
        }

        /// <summary>批量取消多个事件的同一回调。</summary>
        public void Unsubscribe(string[] events, Action<SkillContext<T>> action)
        {
            if (events == null) return;
            for (int i = 0; i < events.Length; i++)
                Unsubscribe(events[i], action);
        }

        /// <summary>触发事件，按优先级顺序调用所有订阅者。</summary>
        public void Trigger(string eventName, SkillContext<T> skillContext)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            var actions = GetCachedActions(eventName);
            if (actions == null) return;

            if (_reentrancyDepth > 8)
            {
                Debug.LogWarning($"[UnitEvent] 事件 '{eventName}' 递归触发过深，已中止");
                return;
            }

            _reentrancyDepth++;
            try
            {
                var meta = skillContext.meta;
                meta.triggerEvent = eventName;
                skillContext.meta = meta;

                for (int i = 0; i < actions.Length; i++)
                    actions[i]?.Invoke(skillContext);
            }
            finally
            {
                _reentrancyDepth--;
            }
        }

        /// <summary>向多个事件名批量订阅同一回调。</summary>
        public void SubscribeMany(string[] eventNames, Action<SkillContext<T>> action, int priority = 0)
        {
            if (eventNames == null) return;
            for (int i = 0; i < eventNames.Length; i++)
                Subscribe(eventNames[i], action, priority);
        }

        /// <summary>获取指定事件的订阅者数量。</summary>
        public int GetSubscriberCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_listeners.TryGetValue(eventName, out var list))
                return 0;
            return list.Count;
        }

        /// <summary>是否已注册该事件名。</summary>
        public bool HasEvent(string eventName) => _listeners.ContainsKey(eventName);

        /// <summary>清除单个事件的所有订阅。</summary>
        public void ClearEvent(string eventName)
        {
            if (_listeners.Remove(eventName))
                _cachedArrays.Remove(eventName);
        }

        /// <summary>清除所有事件与缓存。</summary>
        public void ClearAllEvents()
        {
            _listeners.Clear();
            _cachedArrays.Clear();
        }

        private Action<SkillContext<T>>[] GetCachedActions(string eventName)
        {
            if (!_listeners.TryGetValue(eventName, out var list) || list.Count == 0)
                return null;

            if (!_cachedArrays.TryGetValue(eventName, out var cachedArray) ||
                cachedArray.Length != list.Count)
            {
                cachedArray = new Action<SkillContext<T>>[list.Count];
                for (int i = 0; i < list.Count; i++)
                    cachedArray[i] = list[i].callback;
                _cachedArrays[eventName] = cachedArray;
            }

            return cachedArray;
        }

        private static void SortListeners(List<ListenerEntry> listenerList)
            => listenerList.Sort((a, b) => b.priority.CompareTo(a.priority));

        private void InvalidateCache(string eventName) => _cachedArrays.Remove(eventName);
    }
}
