using System;
using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class DataLayer<T> : IDataLayer<T> where T : class, IUnit<T>
    {
        private Dictionary<string, object> _data = new();
        public ISkill<T> Skill { get; set; }
        public DataLayer(Dictionary<string, object> data) => _data = data;
        public TValue GetValue<TValue>(string key, SkillContext<T> context)
        {
            if (!_data.ContainsKey(key))
                return default(TValue);

            var value = _data[key];

            if (value is Func<SkillContext<T>, TValue> func)
                return func(context);

            return (TValue)value;
        }

        public void SetValue<TValue>(string key, TValue value) => _data[key] = value;

        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula)
            => _data[key] = formula;
    }
}
