using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能执行管线：串联中间件、条件校验、机制执行与时间轴。
    /// </summary>
    public static class SkillExecutionPipeline
    {
        /// <summary>
        /// 执行技能完整流程并返回结果。
        /// </summary>
        public static SkillExecutionResult Execute<T>(
            ISkill<T> skill,
            SkillContext<T> context,
            IReadOnlyList<ISkillMiddleware> middleware = null,
            MechanismErrorPolicy mechanismPolicy = MechanismErrorPolicy.ContinueOnError)
            where T : class, IUnit<T>
        {
            var meta = context.meta;
            if (meta.executionId == 0)
            {
                meta.executionId = SkillSystemServices.NextExecutionId();
                context.meta = meta;
            }

            context = context.WithSkill(skill);

            SkillProfilerMarkers.Execute.Begin();
            try
            {
                RunMiddlewareBefore(skill, ref context, middleware);

                if (context.meta.cancelled)
                {
                    SkillExecutionTrace.Record(skill, context, SkillExecutionResult.Cancelled);
                    return SkillExecutionResult.Cancelled;
                }

                RaiseInstanceExecuting(skill, context);
                ExecuteLayer<T>.RaiseGlobalExecuting(context);

                if (!skill.ConditionLayer.CheckCondition(context))
                {
                    NotifyConditionsOnFailure(skill, context);
                    RunMiddlewareOnFailed(skill, context, middleware);
                    RaiseInstanceFailed(skill, context);
                    ExecuteLayer<T>.RaiseGlobalFailed(context);
                    SkillExecutionTrace.Record(skill, context, SkillExecutionResult.ConditionFailed);
                    return SkillExecutionResult.ConditionFailed;
                }

                if (skill.MechanismLayer is MechanismLayer<T> mechanismLayer)
                    mechanismLayer.Mechanism(context, mechanismPolicy);
                else
                    skill.MechanismLayer.Mechanism(context);

                NotifyConditionsOnSuccess(skill, context);
                RunMiddlewareAfter(skill, context, SkillExecutionResult.Success, middleware);
                RaiseInstanceExecuted(skill, context);
                ExecuteLayer<T>.RaiseGlobalExecuted(context);
                TryStartTimeline(skill, context);
                SkillExecutionTrace.Record(skill, context, SkillExecutionResult.Success);
                return SkillExecutionResult.Success;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SkillExecutionPipeline] 执行异常 [{skill.InformationLayer?.Name}]: {ex.Message}");
                SkillExecutionTrace.Record(skill, context, SkillExecutionResult.Error);
                return SkillExecutionResult.Error;
            }
            finally
            {
                SkillProfilerMarkers.Execute.End();
            }
        }

        private static void RunMiddlewareBefore<T>(
            ISkill<T> skill,
            ref SkillContext<T> context,
            IReadOnlyList<ISkillMiddleware> middleware) where T : class, IUnit<T>
        {
            var global = SkillSystemServices.GlobalMiddleware;
            for (int i = 0; i < global.Count; i++)
            {
                if (!global[i].OnBeforeExecute(skill, ref context))
                    context.meta.cancelled = true;
            }

            if (middleware == null) return;
            for (int i = 0; i < middleware.Count; i++)
            {
                if (!middleware[i].OnBeforeExecute(skill, ref context))
                    context.meta.cancelled = true;
            }
        }

        private static void RunMiddlewareOnFailed<T>(
            ISkill<T> skill,
            SkillContext<T> context,
            IReadOnlyList<ISkillMiddleware> middleware) where T : class, IUnit<T>
        {
            RunMiddleware(skill, middleware, m => m.OnConditionFailed(skill, context));
        }

        private static void RunMiddlewareAfter<T>(
            ISkill<T> skill,
            SkillContext<T> context,
            SkillExecutionResult result,
            IReadOnlyList<ISkillMiddleware> middleware) where T : class, IUnit<T>
        {
            RunMiddleware(skill, middleware, m => m.OnAfterExecute(skill, context, result));
        }

        private static void RunMiddleware<T>(
            ISkill<T> skill,
            IReadOnlyList<ISkillMiddleware> middleware,
            Action<ISkillMiddleware> action) where T : class, IUnit<T>
        {
            var global = SkillSystemServices.GlobalMiddleware;
            if (global.Count > 0)
            {
                for (int i = 0; i < global.Count; i++)
                    action(global[i]);
            }

            if (middleware == null || middleware.Count == 0) return;
            for (int i = 0; i < middleware.Count; i++)
                action(middleware[i]);
        }

        private static void RaiseInstanceExecuting<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T>
        {
            if (skill.ExecuteLayer is ExecuteLayer<T> layer)
                layer.InvokeExecuting(context);
        }

        private static void RaiseInstanceExecuted<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T>
        {
            if (skill.ExecuteLayer is ExecuteLayer<T> layer)
                layer.InvokeExecuted(context);
        }

        private static void RaiseInstanceFailed<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T>
        {
            if (skill.ExecuteLayer is ExecuteLayer<T> layer)
                layer.InvokeFailed(context);
        }

        private static void TryStartTimeline<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T>
        {
            if (skill is Skill<T> concrete &&
                concrete.Timeline != null &&
                concrete.Timeline.enabled &&
                concrete.Timeline.clips != null &&
                concrete.Timeline.clips.Count > 0)
            {
                SkillTimelineService.Play(skill, context, concrete.Timeline);
            }
        }

        private static void NotifyConditionsOnSuccess<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T>
        {
            if (skill.ConditionLayer is ConditionLayer<T> conditionLayer)
            {
                var conditions = conditionLayer.Conditions;
                if (conditions == null) return;

                for (int i = 0; i < conditions.Count; i++)
                {
                    try { conditions[i]?.OnSkillExecuted(context, skill.DataLayer); }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError(
                            $"[SkillExecutionPipeline] 条件回调异常 [{conditions[i]?.GetType().Name}]: {ex.Message}");
                    }
                }
            }
        }

        private static void NotifyConditionsOnFailure<T>(ISkill<T> skill, SkillContext<T> context) where T : class, IUnit<T>
        {
            if (skill.ConditionLayer is ConditionLayer<T> conditionLayer)
            {
                var conditions = conditionLayer.Conditions;
                if (conditions == null) return;

                for (int i = 0; i < conditions.Count; i++)
                {
                    try { conditions[i]?.OnConditionFailed(context, skill.DataLayer); }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError(
                            $"[SkillExecutionPipeline] 条件回调异常 [{conditions[i]?.GetType().Name}]: {ex.Message}");
                    }
                }
            }
        }
    }
}
