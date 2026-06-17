using System;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>施法阶段。</summary>
    public enum SkillCastPhase
    {
        /// <summary>空闲。</summary>
        None,
        /// <summary>读条中。</summary>
        Casting,
        /// <summary>引导中。</summary>
        Channeling,
        /// <summary>执行中。</summary>
        Executing
    }

    /// <summary>施法打断原因。</summary>
    public enum InterruptReason
    {
        /// <summary>手动取消。</summary>
        Manual,
        /// <summary>受到伤害。</summary>
        Damage,
        /// <summary>硬控（眩晕等）。</summary>
        HardCrowdControl,
        /// <summary>移动。</summary>
        Movement,
        /// <summary>沉默。</summary>
        Silence,
        /// <summary>死亡。</summary>
        Death
    }

    /// <summary>
    /// 技能施法控制器：管理读条、引导、打断与最终执行。
    /// </summary>
    public sealed class SkillExecutionController<T> where T : class, IUnit<T>
    {
        private readonly ISkillClock _clock;
        private ActiveCast _activeCast;

        /// <summary>当前施法阶段。</summary>
        public SkillCastPhase Phase => _activeCast?.phase ?? SkillCastPhase.None;
        /// <summary>当前正在施放的技能。</summary>
        public ISkill<T> ActiveSkill => _activeCast?.skill;
        /// <summary>是否正在读条或引导。</summary>
        public bool IsBusy => _activeCast != null;

        /// <summary>开始读条/引导时触发。</summary>
        public event Action<ISkill<T>, SkillContext<T>> OnCastStarted;
        /// <summary>读条/引导完成并执行后触发。</summary>
        public event Action<ISkill<T>, SkillContext<T>> OnCastCompleted;
        /// <summary>施法被打断时触发。</summary>
        public event Action<ISkill<T>, InterruptReason> OnCastInterrupted;

        public SkillExecutionController(ISkillClock clock = null)
        {
            _clock = clock ?? SkillSystemServices.Clock;
        }

        /// <summary>
        /// 尝试执行技能：有读条/引导则进入施法，否则立即执行。
        /// </summary>
        public bool TryExecute(ISkill<T> skill, SkillContext<T> context)
        {
            if (skill == null) return false;

            var profile = GetProfile(skill);
            if (IsBusy && !CanInterruptCurrent(profile))
                return false;

            if (profile.castTime > 0f || profile.channelTime > 0f)
            {
                BeginCast(skill, context, profile);
                return true;
            }

            var result = SkillExecutionPipeline.Execute(skill, context);
            return result == SkillExecutionResult.Success;
        }

        /// <summary>每帧推进读条/引导进度。</summary>
        public void Tick()
        {
            if (_activeCast == null) return;

            _activeCast.elapsed += _clock.DeltaTime;

            switch (_activeCast.phase)
            {
                case SkillCastPhase.Casting:
                    if (_activeCast.elapsed >= _activeCast.profile.castTime)
                    {
                        if (_activeCast.profile.channelTime > 0f)
                        {
                            _activeCast.phase = SkillCastPhase.Channeling;
                            _activeCast.elapsed = 0f;
                        }
                        else
                        {
                            CompleteCast();
                        }
                    }
                    break;

                case SkillCastPhase.Channeling:
                    if (_activeCast.elapsed >= _activeCast.profile.channelTime)
                        CompleteCast();
                    break;
            }
        }

        /// <summary>尝试打断当前施法。</summary>
        public bool TryInterrupt(InterruptReason reason)
        {
            if (_activeCast == null) return false;
            if (!_activeCast.profile.canBeInterrupted && reason != InterruptReason.Manual && reason != InterruptReason.Death)
                return false;

            var skill = _activeCast.skill;
            _activeCast = null;
            OnCastInterrupted?.Invoke(skill, reason);
            return true;
        }

        /// <summary>手动取消当前施法。</summary>
        public void Cancel() => TryInterrupt(InterruptReason.Manual);

        private void BeginCast(ISkill<T> skill, SkillContext<T> context, SkillProfile profile)
        {
            if (_activeCast != null)
                TryInterrupt(InterruptReason.Manual);

            _activeCast = new ActiveCast
            {
                skill = skill,
                context = context,
                profile = profile,
                phase = SkillCastPhase.Casting,
                startedAt = _clock.Time
            };

            OnCastStarted?.Invoke(skill, context);
        }

        private void CompleteCast()
        {
            if (_activeCast == null) return;

            var cast = _activeCast;
            _activeCast = null;

            SkillExecutionPipeline.Execute(cast.skill, cast.context);
            OnCastCompleted?.Invoke(cast.skill, cast.context);
        }

        private bool CanInterruptCurrent(SkillProfile incoming)
        {
            if (_activeCast == null) return true;
            if (!_activeCast.profile.canBeInterrupted) return false;
            return incoming.executionPriority > _activeCast.profile.executionPriority;
        }

        private static SkillProfile GetProfile(ISkill<T> skill)
            => skill is Skill<T> concrete ? concrete.Profile : new SkillProfile();

        private sealed class ActiveCast
        {
            public ISkill<T> skill;
            public SkillContext<T> context;
            public SkillProfile profile;
            public SkillCastPhase phase;
            public float elapsed;
            public float startedAt;
        }
    }
}
