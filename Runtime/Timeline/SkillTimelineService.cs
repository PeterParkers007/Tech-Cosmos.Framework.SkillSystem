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
        private int[] _sortedClipIndices;
        private readonly HashSet<int> _executedClipIndices = new();

        public bool IsPlaying { get; private set; }
        /// <summary>已播放时长（秒）。</summary>
        public float Elapsed => _elapsed;
        /// <summary>时间轴所属施法者（用于按单位停止）。</summary>
        public object Owner { get; private set; }

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
            Owner = context.caster;
            _elapsed = 0f;
            _nextClipIndex = 0;
            _executedClipIndices.Clear();
            IsPlaying = true;
            BuildSortedClipIndices();
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying || _timeline == null || _sortedClipIndices == null) return;

            _elapsed += deltaTime;

            var clips = _timeline.clips;
            while (_nextClipIndex < _sortedClipIndices.Length &&
                   clips[_sortedClipIndices[_nextClipIndex]].startTime <= _elapsed)
            {
                int clipIndex = _sortedClipIndices[_nextClipIndex];
                if (_executedClipIndices.Add(clipIndex))
                    ExecuteClip(clips[clipIndex]);
                _nextClipIndex++;
            }

            if (_elapsed >= _timeline.totalDuration)
                Stop();
        }

        public void Stop()
        {
            IsPlaying = false;
            _nextClipIndex = 0;
            _executedClipIndices.Clear();
            Owner = null;
        }

        private void BuildSortedClipIndices()
        {
            int count = _timeline.clips.Count;
            _sortedClipIndices = new int[count];
            for (int i = 0; i < count; i++)
                _sortedClipIndices[i] = i;

            System.Array.Sort(_sortedClipIndices, (a, b) =>
                _timeline.clips[a].startTime.CompareTo(_timeline.clips[b].startTime));
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
        private sealed class TimelinePlayerEntry
        {
            public ISkillTimelinePlayer Player;
            public object Owner;
        }

        private static readonly List<TimelinePlayerEntry> _activePlayers = new();

        /// <summary>创建并启动时间轴播放。</summary>
        public static void Play<T>(ISkill<T> skill, SkillContext<T> context, SkillTimelineData timeline)
            where T : class, IUnit<T>
        {
            if (timeline == null || !timeline.enabled) return;

            var player = new SkillTimelinePlayer<T>(skill, timeline);
            player.Play(context);
            _activePlayers.Add(new TimelinePlayerEntry
            {
                Player = player,
                Owner = context.caster
            });
        }

        /// <summary>推进所有活跃时间轴（每帧调用）。</summary>
        public static void Tick(float deltaTime)
        {
            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                var player = _activePlayers[i].Player;
                player.Tick(deltaTime);
                if (!player.IsPlaying)
                    _activePlayers.RemoveAt(i);
            }
        }

        /// <summary>停止指定 owner 上的所有时间轴。</summary>
        public static void StopForOwner(object owner)
        {
            if (owner == null) return;

            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_activePlayers[i].Owner, owner))
                    continue;

                _activePlayers[i].Player.Stop();
                _activePlayers.RemoveAt(i);
            }
        }

        /// <summary>停止并清空所有时间轴。</summary>
        public static void StopAll()
        {
            for (int i = _activePlayers.Count - 1; i >= 0; i--)
                _activePlayers[i].Player.Stop();
            _activePlayers.Clear();
        }
    }
}
