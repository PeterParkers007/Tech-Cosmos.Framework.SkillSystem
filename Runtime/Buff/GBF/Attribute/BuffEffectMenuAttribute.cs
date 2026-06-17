// ============================================================
// �ļ���BuffEffectMenuAttribute.cs
// ·����TechCosmos.SkillSystem.Runtime/BuffEffectMenuAttribute.cs
// ============================================================
using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// ��� BuffEffect �ڱ༭���˵��еķ������ʾ��
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class BuffEffectMenuAttribute : Attribute
    {
        public string Category { get; }
        public string DisplayName { get; set; }
        public int Priority { get; set; } = 99;

        public BuffEffectMenuAttribute(string category)
        {
            Category = category;
        }
    }
}