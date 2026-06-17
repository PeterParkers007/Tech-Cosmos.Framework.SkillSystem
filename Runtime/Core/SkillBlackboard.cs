using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能执行黑板，在单次执行流程中共享键值数据。
    /// </summary>
    public sealed class SkillBlackboard
    {
        private readonly Dictionary<string, object> _values = new();

        /// <summary>写入指定键的值。</summary>
        public void Set<T>(string key, T value) => _values[key] = value;

        /// <summary>尝试读取指定键的值。</summary>
        /// <returns>键存在且类型匹配时返回 true。</returns>
        public bool TryGet<T>(string key, out T value)
        {
            if (_values.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>读取指定键的值，不存在时返回默认值。</summary>
        public T Get<T>(string key, T defaultValue = default)
            => TryGet(key, out T value) ? value : defaultValue;

        /// <summary>清空所有键值。</summary>
        public void Clear() => _values.Clear();
    }
}
