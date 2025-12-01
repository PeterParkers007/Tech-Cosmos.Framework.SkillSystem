using System;
using UnityEngine;
namespace TechCosmos.SkillSystem.Runtime
{
    public static class SkillSystemConfig
    {
        private static Type _currentUnitType;
        private static bool _isInitialized = false;

        public static void Initialize<T>() where T : class, IUnit<T>
        {
            _currentUnitType = typeof(T);
            _isInitialized = true;
            Debug.Log($"技能系统已初始化为使用类型: {_currentUnitType.Name}");
        }

        public static Type GetCurrentUnitType()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("SkillSystemConfig not initialized. Call Initialize<T>() first.");
            return _currentUnitType;
        }

        public static bool IsInitialized => _isInitialized;
    }
}
