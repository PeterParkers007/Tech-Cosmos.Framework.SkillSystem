using System;
namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 数据层接口：提供技能运行时参数与公式求值。
    /// </summary>
    public interface IDataLayer<T> : IDataLayerBase, ISkillLayer<T> where T : class, IUnit<T>
    {
        /// <summary>按键获取值，支持公式与委托。</summary>
        public TValue GetValue<TValue>(string key, SkillContext<T> context);
        /// <summary>设置静态值。</summary>
        public void SetValue<TValue>(string key, TValue value);
        /// <summary>设置基于上下文的动态公式。</summary>
        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula);
        /// <summary>尝试获取值，不存在时返回 false。</summary>
        public bool TryGetValue<TValue>(string key, SkillContext<T> context, out TValue value);
        /// <summary>是否包含指定键。</summary>
        public bool ContainsKey(string key);
    }
}
