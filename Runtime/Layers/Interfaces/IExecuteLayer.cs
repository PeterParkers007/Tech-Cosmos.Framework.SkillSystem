namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 执行层接口：技能执行的统一入口。
    /// </summary>
    public interface IExecuteLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        /// <summary>执行技能（经管线校验条件并触发机制）。</summary>
        public void Execute(SkillContext<T> skillContext);
    }
}
