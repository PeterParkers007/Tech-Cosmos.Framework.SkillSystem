using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能网络命令：用于客户端预测与服务端校验的数据包。
    /// </summary>
    [Serializable]
    public class SkillCommand
    {
        /// <summary>技能名称。</summary>
        public string skillName;
        /// <summary>施法者实体 ID。</summary>
        public int casterEntityId;
        /// <summary>目标实体 ID。</summary>
        public int targetEntityId;
        /// <summary>逻辑帧号。</summary>
        public int tick;
        /// <summary>随机种子（保证确定性）。</summary>
        public int randomSeed;
        /// <summary>触发事件名。</summary>
        public string triggerEvent;
        /// <summary>目标位置。</summary>
        public UnityEngine.Vector3 targetPos;
    }

    /// <summary>
    /// 技能网络桥接接口：定义预测、校验与回滚流程。
    /// </summary>
    public interface ISkillNetworkBridge<T> where T : class, IUnit<T>
    {
        /// <summary>服务端校验命令合法性。</summary>
        bool ValidateServer(SkillCommand command, T caster);
        /// <summary>客户端应用预测结果。</summary>
        void ApplyPredicted(SkillCommand command, T caster);
        /// <summary>预测失败时回滚。</summary>
        void Rollback(SkillCommand command, T caster);
        /// <summary>从技能上下文创建网络命令。</summary>
        SkillCommand CreateCommand(ISkill<T> skill, SkillContext<T> context);
    }
}
