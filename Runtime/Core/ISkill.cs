namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能接口，暴露分层架构中的各功能层访问入口。
    /// </summary>
    public interface ISkill<T> where T : class, IUnit<T>
    {
        /// <summary>基础层，负责触发与激活逻辑。</summary>
        IBaseLayer<T> BaseLayer { get; }
        /// <summary>条件层，管理释放前置条件。</summary>
        IConditionLayer<T> ConditionLayer { get; }
        /// <summary>信息层，提供技能名称等元数据。</summary>
        IInformationLayer<T> InformationLayer { get; }
        /// <summary>机制层，执行具体游戏效果。</summary>
        IMechanismLayer<T> MechanismLayer { get; }
        /// <summary>数据层，存储技能配置与序列化数据。</summary>
        IDataLayer<T> DataLayer { get; }
        /// <summary>执行层，协调条件检查与机制调用。</summary>
        IExecuteLayer<T> ExecuteLayer { get; }
    }
}
