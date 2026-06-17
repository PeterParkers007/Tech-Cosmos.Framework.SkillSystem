using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 主动技能基础层：经施法控制器或直接执行层释放技能。
    /// </summary>
    public class ActiveBaseLayer<T> : BaseLayer<T> where T : class, IUnit<T>
    {
        public ActiveBaseLayer(List<string> triggerEvents) : base(triggerEvents) { }

        public override void Trigger(SkillContext<T> context)
        {
            var skill = Skill;
            var ctx = context.skill != null ? context : SkillContextExtensions.WithSkill(context, skill);

            if (ctx.caster is ISkillExecutionOwner<T> owner && owner.ExecutionController != null)
            {
                owner.ExecutionController.TryExecute(skill, ctx);
                return;
            }

            skill.ExecuteLayer.Execute(ctx);
        }
    }
}
