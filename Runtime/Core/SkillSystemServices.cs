using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能系统全局服务入口，提供时钟、随机源、中间件及执行 ID 分配。
    /// </summary>
    public static class SkillSystemServices
    {
        /// <summary>全局时间时钟，默认为 Unity 时间。</summary>
        public static ISkillClock Clock { get; set; } = new UnitySkillClock();
        /// <summary>全局随机数提供者，默认为 Unity 随机。</summary>
        public static IRandomProvider Random { get; set; } = new UnityRandomProvider();

        private static readonly List<ISkillMiddleware> _globalMiddleware = new();

        /// <summary>已注册的全局执行中间件列表（只读）。</summary>
        public static IReadOnlyList<ISkillMiddleware> GlobalMiddleware => _globalMiddleware;

        /// <summary>注册全局中间件，重复注册会被忽略。</summary>
        public static void RegisterMiddleware(ISkillMiddleware middleware)
        {
            if (middleware != null && !_globalMiddleware.Contains(middleware))
                _globalMiddleware.Add(middleware);
        }

        /// <summary>注销全局中间件。</summary>
        public static void UnregisterMiddleware(ISkillMiddleware middleware)
            => _globalMiddleware.Remove(middleware);

        /// <summary>清空所有全局中间件。</summary>
        public static void ClearMiddleware() => _globalMiddleware.Clear();

        private static int _executionIdCounter;

        /// <summary>分配下一个执行 ID，用于追踪单次技能执行。</summary>
        public static int NextExecutionId() => ++_executionIdCounter;
    }
}
