using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 状态标签容器：管理单位身上的字符串标签集合。
    /// </summary>
    public sealed class TagContainer
    {
        private readonly HashSet<string> _tags = new(StringComparer.Ordinal);

        /// <summary>当前标签数量。</summary>
        public int Count => _tags.Count;

        /// <summary>是否拥有指定标签。</summary>
        public bool HasTag(string tag) => !string.IsNullOrEmpty(tag) && _tags.Contains(tag);

        /// <summary>是否拥有列表中任一标签。</summary>
        public bool HasAnyTag(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                if (HasTag(tag)) return true;
            }
            return false;
        }

        /// <summary>是否拥有列表中全部标签。</summary>
        public bool HasAllTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                if (!HasTag(tag)) return false;
            }
            return true;
        }

        /// <summary>添加标签。</summary>
        public void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag)) _tags.Add(tag);
        }

        /// <summary>移除标签。</summary>
        public void RemoveTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag)) _tags.Remove(tag);
        }

        /// <summary>清空所有标签。</summary>
        public void Clear() => _tags.Clear();

        /// <summary>当前所有标签（只读）。</summary>
        public IReadOnlyCollection<string> Tags => _tags;
    }
}
