using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 条件层接口：管理释放前置条件。
    /// </summary>
    public interface IConditionLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        /// <summary>条件列表（全部满足方可释放）。</summary>
        public List<Condition<T>> Conditions { get; set; }
        /// <summary>检查所有条件是否满足。</summary>
        public bool CheckCondition(SkillContext<T> skillContext);
    }
}
