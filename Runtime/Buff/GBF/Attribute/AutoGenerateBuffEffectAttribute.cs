// ============================================================
// �ļ���AutoGenerateBuffEffectAttribute.cs
// ·����TechCosmos.SkillSystem.Runtime/AutoGenerateBuffEffectAttribute.cs
// ============================================================
using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// �����ҪΪָ��Ŀ���������ɷ�� BuffEffect �ķ��ͻ���
    /// �÷���[AutoGenerateBuffEffect(typeof(Character), typeof(Enemy))]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AutoGenerateBuffEffectAttribute : Attribute
    {
        public Type[] TargetTypes { get; }

        public AutoGenerateBuffEffectAttribute(params Type[] targetTypes)
        {
            TargetTypes = targetTypes;
        }
    }

    /// <summary>
    /// ��Ǹ�������Ҫ�� BuffEffect ��������ΪĿ�� T
    /// ���� [ApplyBuffTarget] class Character { }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ApplyBuffTargetAttribute : Attribute
    {
    }
    // Runtime/BuffFieldAttribute.cs
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class BuffFieldAttribute : Attribute
    {
    }
}