namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 随机数提供者抽象，支持可替换的随机源以实现确定性与测试。
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>返回 [0, 1) 范围内的随机浮点数。</summary>
        float Value { get; }
        /// <summary>返回 [min, max) 范围内的随机浮点数。</summary>
        float Range(float min, float max);
        /// <summary>返回 [min, maxExclusive) 范围内的随机整数。</summary>
        int Range(int min, int maxExclusive);
    }

    /// <summary>基于 Unity <see cref="UnityEngine.Random"/> 的随机实现。</summary>
    public sealed class UnityRandomProvider : IRandomProvider
    {
        /// <inheritdoc/>
        public float Value => UnityEngine.Random.value;
        /// <inheritdoc/>
        public float Range(float min, float max) => UnityEngine.Random.Range(min, max);
        /// <inheritdoc/>
        public int Range(int min, int maxExclusive) => UnityEngine.Random.Range(min, maxExclusive);
    }

    /// <summary>基于种子的确定性随机实现，适用于测试与回放。</summary>
    public sealed class SeededRandomProvider : IRandomProvider
    {
        private readonly System.Random _random;

        /// <summary>使用指定种子创建随机提供者。</summary>
        public SeededRandomProvider(int seed) => _random = new System.Random(seed);

        /// <inheritdoc/>
        public float Value => (float)_random.NextDouble();
        /// <inheritdoc/>
        public float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        /// <inheritdoc/>
        public int Range(int min, int maxExclusive) => _random.Next(min, maxExclusive);
    }
}
