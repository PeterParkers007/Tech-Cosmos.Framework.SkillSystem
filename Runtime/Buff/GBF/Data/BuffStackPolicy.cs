// ============================================================
// �ļ���BuffStackPolicy.cs
// ·����TechCosmos.SkillSystem.Runtime/BuffStackPolicy.cs
// ============================================================
namespace TechCosmos.SkillSystem.Runtime
{
    public enum BuffStackPolicy
    {
        /// <summary>ˢ�³���ʱ�䣬�����Ӳ���</summary>
        ExtendDuration,
        /// <summary>���Ӳ�����ˢ�³���ʱ��</summary>
        StackAndRefresh,
        /// <summary>ÿ�������ʱ</summary>
        Independent,
        /// <summary>�µ��滻�ɵ�</summary>
        Replace
    }
}