// Runtime/SkillDataSO_ResourceExtension.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    public class SkillResourceEntry
    {
        public string key;
        public string path;
    }

    /// <summary>
    /// 资源层数据 - 通过 partial 扩展 SkillDataSO
    /// </summary>
    public partial class SkillDataSO
    {
        [SerializeField]
        private List<SkillResourceEntry> _skillResources = new();

        public IReadOnlyList<SkillResourceEntry> SkillResources => _skillResources;

        public Dictionary<string, string> GetResourceDictionary()
        {
            var dict = new Dictionary<string, string>();
            foreach (var entry in _skillResources)
            {
                if (!string.IsNullOrEmpty(entry.key))
                    dict[entry.key] = entry.path;
            }
            return dict;
        }
    }
}