using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>技能时间轴播放器接口。</summary>
    public interface ISkillTimelinePlayer
    {
        /// <summary>是否正在播放。</summary>
        bool IsPlaying { get; }
        /// <summary>推进时间轴。</summary>
        void Tick(float deltaTime);
        /// <summary>停止播放。</summary>
        void Stop();
    }

    /// <summary>
    /// 技能时间轴播放器：按时间顺序触发片段中的机制或事件。
    /// </summary>
    public sealed class SkillTimelinePlayer<T> : ISkillTimelinePlayer where T : class, IUnit<T>
    {
        private readonly ISkill<T> _skill;
        private readonly SkillTimelineData _timeline;
        private SkillContext<T> _context;
        private float _elapsed;
        private int _nextClipIndex;

        public bool IsPlaying { get; private set; }
        /// <summary>已播放时长（秒）。</summary>
        public float Elapsed => _elapsed;

        public SkillTimelinePlayer(ISkill<T> skill, SkillTimelineData timeline)
        {
            _skill = skill;
            _timeline = timeline;
        }

        /// <summary>开始播放时间轴。</summary>
        public void Play(SkillContext<T> context)
        {
            if (_timeline == null || !_timeline.enabled || _timeline.clips == null || _timeline.clips.Count == 0)
                return;

            _context = context.WithSkill(_skill);
            _elapsed = 0f;
            _nextClipIndex = 0;
            IsPlaying = true;
            _timeline.clips.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying || _timeline == null) return;

            float previous = _elapsed;
            _elapsed += deltaTime;

            var clips = _timeline.clips;
            while (_nextClipIndex < clips.Count && clips[_nextClipIndex].startTime <= _elapsed)
            {
                var clip = clips[_nextClipIndex];
                if (clip.startTime >= previous)
                    ExecuteClip(clip);
                _nextClipIndex++;
            }

            if (_elapsed >= _timeline.totalDuration)
                Stop();
        }

        public void Stop()
        {
            IsPlaying = false;
            _nextClipIndex = 0;
        }

        private void ExecuteClip(SkillTimelineClip clip)
        {
            switch (clip.clipType)
            {
                case SkillTimelineClipType.Mechanism:
                    clip.mechanism?.ExecuteBase(_context, _skill.DataLayer);
                    break;

                case SkillTimelineClipType.EventMarker:
                    if (!string.IsNullOrEmpty(clip.eventName) && _context.caster != null)
                        _context.caster.TriggerEvent(clip.eventName, _context);
                    break;
            }
        }
    }

    /// <summary>
    /// 技能时间轴服务：管理所有活跃的时间轴播放器。
    /// </summary>
    public static class SkillTimelineService
    {
        private static readonly List<ISkillTimelinePlayer> _activePlayers = new();

        /// <summary>创建并启动时间轴播放。</summary>
        public static void Play<T>(ISkill<T> skill, SkillContext<T> context, SkillTimelineData timeline)
            where T : class, IUnit<T>
        {
            if (timeline == null || !timeline.enabled) return;

            var player = new SkillTimelinePlayer<T>(skill, timeline);
            player.Play(context);
            _activePlayers.Add(player);
        }

        /// <summary>推进所有活跃时间轴（每帧调用）。</summary>
        public static void Tick(float deltaTime)
        {
            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                var player = _activePlayers[i];
                player.Tick(deltaTime);
                if (!player.IsPlaying)
                    _activePlayers.RemoveAt(i);
            }
        }

        /// <summary>停止并清空所有时间轴。</summary>
        public static void StopAll()
        {
            for (int i = _activePlayers.Count - 1; i >= 0; i--)
                _activePlayers[i].Stop();
            _activePlayers.Clear();
        }
    }
}
