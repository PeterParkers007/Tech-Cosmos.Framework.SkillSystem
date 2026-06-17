namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能系统时间时钟抽象，解耦 Unity 时间与可测试的固定时钟。
    /// </summary>
    public interface ISkillClock
    {
        /// <summary>当前时间（秒）。</summary>
        float Time { get; }
        /// <summary>上一帧增量时间（秒）。</summary>
        float DeltaTime { get; }
        /// <summary>时间缩放系数。</summary>
        float TimeScale { get; }
    }

    /// <summary>基于 Unity <see cref="UnityEngine.Time"/> 的时钟实现。</summary>
    public sealed class UnitySkillClock : ISkillClock
    {
        /// <inheritdoc/>
        public float Time => UnityEngine.Time.time;
        /// <inheritdoc/>
        public float DeltaTime => UnityEngine.Time.deltaTime;
        /// <inheritdoc/>
        public float TimeScale => UnityEngine.Time.timeScale;
    }

    /// <summary>固定值时钟，用于单元测试或确定性回放。</summary>
    public sealed class FixedSkillClock : ISkillClock
    {
        private readonly float _time;
        private readonly float _deltaTime;
        private readonly float _timeScale;

        /// <summary>创建固定时钟实例。</summary>
        public FixedSkillClock(float time, float deltaTime = 0f, float timeScale = 1f)
        {
            _time = time;
            _deltaTime = deltaTime;
            _timeScale = timeScale;
        }

        /// <inheritdoc/>
        public float Time => _time;
        /// <inheritdoc/>
        public float DeltaTime => _deltaTime;
        /// <inheritdoc/>
        public float TimeScale => _timeScale;
    }
}
