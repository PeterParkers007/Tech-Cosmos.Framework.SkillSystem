// ============================================================
// �ļ���BaseBuff.cs
// ·����TechCosmos.SkillSystem.Runtime/BaseBuff.cs
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class BaseBuff<T> : IBuff<T> where T : class
    {
        public bool isOver { get; set; }
        public int priority { get; set; }
        public string[] tags { get; set; }
        public string icon { get; set; }
        public T target { get; set; }

        public event Action<T> OnApply;
        public event Action<T> OnRemove;

        protected List<BuffEffectExecuterBase> _effectExecuters = new();

        protected float _duration;
        protected float _timer;
        protected bool _isPaused;
        protected bool _isTimePaused;
        protected float _timeScale = 1f;

        protected T _caster;

        public virtual string BuffName => GetType().Name;
        public virtual BuffStackPolicy StackPolicy => BuffStackPolicy.ExtendDuration;
        public virtual int MaxStacks => 1;
        public int CurrentStacks { get; set; } = 1;

        public bool IsPermanent => _duration < 0;

        protected Dictionary<string, Func<float, BuffModifyContext<T>, float>> _modifiers = new();
        protected Dictionary<string, BuffActionDelegate<T>> _actions = new();

        public BaseBuff(T target, float duration, string[] tags = null)
        {
            this.target = target;
            isOver = false;
            _duration = duration;
            _timer = 0f;
            _isPaused = false;
            _timeScale = 1f;
            this.tags = tags ?? Array.Empty<string>();
        }

        public void AddEffectExecuter(BuffEffectExecuterBase executer) => _effectExecuters.Add(executer);
        public void AddEffectExecuter(params BuffEffectExecuterBase[] executers) => _effectExecuters.AddRange(executers);
        public void RemoveEffectExecuter(BuffEffectExecuterBase executer) => _effectExecuters.Remove(executer);

        public void RegisterModifier(string modifyType, Func<float, BuffModifyContext<T>, float> modifier)
            => _modifiers[modifyType] = modifier;

        public virtual float ModifyValue(string modifyType, float baseValue, BuffModifyContext<T> context = null)
        {
            var ctx = context ?? new BuffModifyContext<T> { target = target, caster = _caster };
            if (_modifiers.TryGetValue(modifyType, out var modifier))
                return modifier(baseValue, ctx);
            return baseValue;
        }

        public void RegisterAction(string actionName, BuffActionDelegate<T> action)
            => _actions[actionName] = action;

        public virtual void OnAction(string actionName, T caster, T target, float value = default, string damageType = default)
        {
            if (_actions.TryGetValue(actionName, out var action))
                action(actionName, caster, target, value, damageType);
        }

        public void Apply()
        {
            var context = new BuffContext<T>
            {
                deltaTime = Time.deltaTime,
                elapsedTime = _timer,
                progress = Progress,
                currentStacks = CurrentStacks,
                source = target
            };

            for (int i = 0; i < _effectExecuters.Count; i++)
                _effectExecuters[i].Apply(target, context);
        }

        public void Remove() => TriggerRemoveEvent(target);
        public void TriggerApplyEvent(T target) => OnApply?.Invoke(target);
        public void TriggerRemoveEvent(T target) => OnRemove?.Invoke(target);

        public void Update(float deltaTime)
        {
            if (_isPaused) return;
            if (!_isTimePaused) _timer += deltaTime * _timeScale;
            Apply();

            if (!IsPermanent && _timer >= _duration)
            {
                Remove();
                isOver = true;
            }
        }

        public void Pause() => _isPaused = true;
        public void TimePause() => _isTimePaused = true;
        public void Resume() => _isPaused = false;
        public void TimeResume() => _isTimePaused = false;
        public void SetPaused(bool paused) => _isPaused = paused;
        public void SetTimePaused(bool paused) => _isTimePaused = paused;
        public void SetTimeScale(float scale) => _timeScale = Mathf.Max(0, scale);
        public void SetDuration(float duration)
        {
            _duration = duration;
            _timer = 0f;
            isOver = false;
        }

        public float TimeScale => _timeScale;

        public float RemainingTime
        {
            get
            {
                if (IsPermanent) return float.MaxValue;
                return Mathf.Max(0, _duration - _timer);
            }
        }

        public float ElapsedTime => _timer;

        public float Progress
        {
            get
            {
                if (IsPermanent) return 0f;
                return _duration > 0 ? Mathf.Clamp01(_timer / _duration) : 1f;
            }
        }

        public bool IsPaused => _isPaused;


        public void Refresh() { _timer = 0f; isOver = false; }
        public void ResetTimer() => _timer = 0f;
        public void ExtendDuration(float extraTime)
        {
            if (!IsPermanent)
                _duration += extraTime;
        }
        public void SetRemainingTime(float remaining)
        {
            if (!IsPermanent)
                _timer = Mathf.Max(0, _duration - remaining);
        }
    }
}