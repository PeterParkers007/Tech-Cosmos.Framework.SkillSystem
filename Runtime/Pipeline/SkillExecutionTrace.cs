using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>技能系统性能采样标记。</summary>
    public static class SkillProfilerMarkers
    {
        public static readonly UnityEngine.Profiling.CustomSampler Execute =
            UnityEngine.Profiling.CustomSampler.Create("SkillSystem.Execute");

        public static readonly UnityEngine.Profiling.CustomSampler Mechanism =
            UnityEngine.Profiling.CustomSampler.Create("SkillSystem.Mechanism");

        public static readonly UnityEngine.Profiling.CustomSampler Formula =
            UnityEngine.Profiling.CustomSampler.Create("SkillSystem.Formula");
    }

    /// <summary>单条技能执行追踪记录。</summary>
    public readonly struct SkillTraceEntry
    {
        public readonly string skillName;
        public readonly SkillExecutionResult result;
        public readonly float timestamp;
        public readonly int executionId;

        public SkillTraceEntry(string skillName, SkillExecutionResult result, float timestamp, int executionId)
        {
            this.skillName = skillName;
            this.result = result;
            this.timestamp = timestamp;
            this.executionId = executionId;
        }
    }

    /// <summary>
    /// 技能执行追踪：环形缓冲区记录最近执行历史，便于调试。
    /// </summary>
    public static class SkillExecutionTrace
    {
        private const int Capacity = 128;
        private static readonly SkillTraceEntry[] _buffer = new SkillTraceEntry[Capacity];
        private static int _head;
        private static int _count;

        /// <summary>当前缓冲区中的记录数。</summary>
        public static int Count => _count;

        /// <summary>记录一次技能执行结果。</summary>
        public static void Record<T>(ISkill<T> skill, in SkillContext<T> context, SkillExecutionResult result)
            where T : class, IUnit<T>
        {
            var entry = new SkillTraceEntry(
                skill?.InformationLayer?.Name ?? "Unknown",
                result,
                SkillSystemServices.Clock.Time,
                context.meta.executionId);

            _buffer[_head] = entry;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }

        /// <summary>获取最近的执行记录（最新在前）。</summary>
        public static IReadOnlyList<SkillTraceEntry> GetRecentEntries(int maxCount = 32)
        {
            var list = new List<SkillTraceEntry>(maxCount);
            int take = _count < maxCount ? _count : maxCount;
            for (int i = 0; i < take; i++)
            {
                int index = (_head - 1 - i + Capacity) % Capacity;
                list.Add(_buffer[index]);
            }
            return list;
        }

        /// <summary>清空追踪缓冲区。</summary>
        public static void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
