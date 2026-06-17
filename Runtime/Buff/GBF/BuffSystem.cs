// ============================================================
// 文件：BuffSystem.cs
// 路径：TechCosmos.SkillSystem.Runtime/BuffSystem.cs
// ============================================================
using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public class BuffSystem<T> where T : class
    {
        protected T _target;
        protected List<IBuff<T>> buffs = new();

        private List<IBuff<T>> _pendingAddBuffs = new();
        private bool _isUpdating = false;

        public event Action<IBuff<T>> OnBuffAdded;
        public event Action<IBuff<T>> OnBuffRemoved;
        public T Target => _target;
        public int BuffCount => buffs.Count + _pendingAddBuffs.Count;
        public BuffSystem(T target) => _target = target;

        public void BuffUpdate(float deltaTime)
        {
            _isUpdating = true;

            try
            {
                FlushPendingBuffs();

                for (int i = buffs.Count - 1; i >= 0; i--)
                {
                    var buff = buffs[i];
                    buff.Update(deltaTime);
                    if (buff.isOver) RemoveBuff(buff);
                }
            }
            finally
            {
                _isUpdating = false;
                FlushPendingBuffs();
            }
        }

        private void FlushPendingBuffs()
        {
            if (_pendingAddBuffs.Count == 0) return;

            foreach (var buff in _pendingAddBuffs)
            {
                var existing = FindBuffByName(buff.BuffName);
                if (existing != null)
                {
                    ApplyStackPolicy(existing, buff);
                }
                else
                {
                    buffs.Add(buff);
                    OnBuffAdded?.Invoke(buff);
                }
            }

            _pendingAddBuffs.Clear();

            if (buffs.Count > 0)
                SortBuffs();
        }

        public void SortBuffs() => buffs.Sort((a, b) => a.priority.CompareTo(b.priority));

        public void AddBuff(IBuff<T> buff)
        {
            if (buff == null) return;

            var existing = FindBuffByName(buff.BuffName);
            if (existing != null)
            {
                switch (existing.StackPolicy)
                {
                    case BuffStackPolicy.ExtendDuration:
                        existing.Refresh();
                        OnBuffAdded?.Invoke(existing);
                        return;

                    case BuffStackPolicy.StackAndRefresh:
                        if (existing.CurrentStacks < existing.MaxStacks)
                            existing.CurrentStacks++;
                        existing.Refresh();
                        OnBuffAdded?.Invoke(existing);
                        return;

                    case BuffStackPolicy.Independent:
                        break;

                    case BuffStackPolicy.Replace:
                        RemoveBuff(existing);
                        break;
                }
            }

            if (_isUpdating)
            {
                _pendingAddBuffs.Add(buff);
            }
            else
            {
                var pendingExisting = _pendingAddBuffs.Find(b => b.BuffName == buff.BuffName);
                if (pendingExisting != null)
                {
                    _pendingAddBuffs.Remove(pendingExisting);
                }

                buff.target = _target;
                buff.TriggerApplyEvent(buff.target);
                buffs.Add(buff);
                SortBuffs();
                OnBuffAdded?.Invoke(buff);
            }
        }

        public void AddBuff(params IBuff<T>[] buffs)
        {
            for (int i = 0; i < buffs.Length; i++)
                AddBuff(buffs[i]);
        }

        public void RemoveBuff(IBuff<T> buff)
        {
            if (buff == null) return;
            buff.isOver = true;
            buffs.Remove(buff);
            OnBuffRemoved?.Invoke(buff);
        }

        public void ManualRemoveBuff(IBuff<T> buff)
        {
            if (buff != null) buff.isOver = true;
        }

        public void ClearBuff() => buffs.Clear();

        public void DispelByTags(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags))
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public void RemoveBuffsByName(string buffName)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].BuffName == buffName)
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public float GetModifiedValue(string modifyType, float baseValue, BuffModifyContext<T> context = null)
        {
            float result = baseValue;
            var ctx = context ?? new BuffModifyContext<T> { target = _target };
            for (int i = 0; i < buffs.Count; i++)
                if (!buffs[i].isOver)
                    result = buffs[i].ModifyValue(modifyType, result, ctx);
            return result;
        }

        /// <summary>
        /// 分发 Action 事件给所有 Buff
        /// </summary>
        /// <param name="actionName">Action 名称</param>
        /// <param name="caster">施法者/触发者</param>
        /// <param name="target">目标（通常是被作用的对象）</param>
        /// <param name="value">数值（如伤害值、治疗值）</param>
        /// <param name="damageType">伤害类型</param>
        public void DispatchAction(string actionName, T caster, T target, float value = default, string damageType = default)
        {
            for (int i = 0; i < buffs.Count; i++)
                if (!buffs[i].isOver)
                    buffs[i].OnAction(actionName, caster, target, value, damageType);
        }

        // 便捷方法：只有 caster（当 caster 和 target 相同时）
        public void DispatchAction(string actionName, T caster, float value = default, string damageType = default)
        {
            DispatchAction(actionName, caster, caster, value, damageType);
        }

        public void RemoveBuffsByAnyTag(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags))
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public void RemoveBuffsByAllTags(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (CheckBuffHasAllTags(buffs[i], tags))
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public bool HasAnyBuff(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags)) return true;
            }
            return false;
        }

        public bool HasAllBuff(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAllTags(buffs[i], tags)) return true;
            }
            return false;
        }

        public IBuff<T> FindBuffByAnyTag(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags)) return buffs[i];
            }
            return null;
        }

        public IBuff<T> FindBuffByAllTags(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAllTags(buffs[i], tags)) return buffs[i];
            }
            return null;
        }

        public List<IBuff<T>> FindAllBuffsByAnyTag(params string[] tags)
        {
            var r = new List<IBuff<T>>();
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags)) r.Add(buffs[i]);
            }
            return r;
        }

        public List<IBuff<T>> FindAllBuffsByAllTags(params string[] tags)
        {
            var r = new List<IBuff<T>>();
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAllTags(buffs[i], tags)) r.Add(buffs[i]);
            }
            return r;
        }

        public IBuff<T> FindBuffByName(string buffName)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i].BuffName == buffName) return buffs[i];
            }
            return null;
        }
        public List<IBuff<T>> GetBuffList() => buffs;

        private void ApplyStackPolicy(IBuff<T> existing, IBuff<T> newBuff)
        {
            switch (existing.StackPolicy)
            {
                case BuffStackPolicy.ExtendDuration:
                    existing.Refresh();
                    break;

                case BuffStackPolicy.StackAndRefresh:
                    if (existing.CurrentStacks < existing.MaxStacks)
                        existing.CurrentStacks++;
                    existing.Refresh();
                    break;

                case BuffStackPolicy.Independent:
                    buffs.Add(newBuff);
                    break;

                case BuffStackPolicy.Replace:
                    RemoveBuff(existing);
                    buffs.Add(newBuff);
                    break;
            }
        }

        private bool CheckBuffHasAnyTag(IBuff<T> buff, params string[] searchTags)
        {
            var bt = buff.tags;
            if (bt == null) return false;
            for (int i = 0; i < bt.Length; i++)
                for (int j = 0; j < searchTags.Length; j++)
                    if (bt[i] == searchTags[j]) return true;
            return false;
        }

        private bool CheckBuffHasAllTags(IBuff<T> buff, params string[] searchTags)
        {
            var bt = buff.tags;
            if (bt == null) return false;
            for (int i = 0; i < searchTags.Length; i++)
            {
                bool f = false;
                for (int j = 0; j < bt.Length; j++)
                {
                    if (bt[j] == searchTags[i]) { f = true; break; }
                }
                if (!f) return false;
            }
            return true;
        }
    }
}