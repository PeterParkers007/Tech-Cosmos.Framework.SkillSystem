using System;
namespace TechCosmos.SkillSystem.Runtime
{
    public interface IDataLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        public TValue GetValue<TValue>(string key);
        public void SetValue<TValue>(string key, TValue value);
        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula);
    }
}
