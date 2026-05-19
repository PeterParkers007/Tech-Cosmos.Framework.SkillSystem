// Runtime/ResourceLayerExtension.cs
using System.Collections.Generic;
using System.Linq;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Runtime
{
    public static class ResourceLayerExtension
    {
        private static readonly Dictionary<object, IResourceLayer> _resourceLayers = new();

        public static ISkill<T> WithResources<T>(this ISkill<T> skill, params (string key, string value)[] resources)
    where T : class, IUnit<T>
        {
            var dict = new Dictionary<string, string>();
            foreach (var (key, value) in resources)
                dict[key] = value;

            var layer = new ResourceLayer<T>(dict);
            layer.Skill = skill;
            _resourceLayers[skill] = layer;
            return skill;
        }
        public static ISkill<T> WithResources<T>(this ISkill<T> skill, Dictionary<string, string> resources)
            where T : class, IUnit<T>
        {
            if (resources == null || resources.Count == 0)
                return skill;

            var layer = new ResourceLayer<T>(resources);
            layer.Skill = skill;
            _resourceLayers[skill] = layer;
            return skill;
        }
        public static IResourceLayer<T> GetResourceLayer<T>(this ISkill<T> skill)
            where T : class, IUnit<T>
        {
            return _resourceLayers.TryGetValue(skill, out var layer)
                ? layer as IResourceLayer<T>
                : null;
        }
        public static ISkill<T> CreateSkillFromSO<T>(SkillDataSO<T> skillDataSO)
    where T : class, IUnit<T>
        {
            var skill = SkillFactory<T>.CreateSkill(skillDataSO.GetSkillData());

            // 自动附加资源层
            var resources = skillDataSO.GetResourceDictionary();
            if (resources.Count > 0)
            {
                skill = skill.WithResources(resources);
            }

            return skill;
        }
        public static string GetResource<T>(this ISkill<T> skill, string key)
            where T : class, IUnit<T>
        {
            return skill.GetResourceLayer()?.GetResource(key);
        }

    }

    public interface IResourceLayer
    {
        string GetResource(string key);
        void RegisterResource(string key, string path);
        bool HasResource(string key);
    }

    public interface IResourceLayer<T> : IResourceLayer where T : class, IUnit<T>
    {
        new string GetResource(string key);
        new void RegisterResource(string key, string path);
        new bool HasResource(string key);
        Dictionary<string, string> Resources { get; }
    }

    public class ResourceLayer<T> : IResourceLayer<T> where T : class, IUnit<T>
    {
        public Dictionary<string, string> Resources { get; private set; }
        public ISkill<T> Skill { get; set; }

        public ResourceLayer(Dictionary<string, string> resources)
        {
            Resources = resources ?? new Dictionary<string, string>();
        }

        public string GetResource(string key) => Resources.GetValueOrDefault(key);
        string IResourceLayer.GetResource(string key) => GetResource(key);

        public void RegisterResource(string key, string path) => Resources[key] = path;
        void IResourceLayer.RegisterResource(string key, string path) => RegisterResource(key, path);

        public bool HasResource(string key) => Resources.ContainsKey(key);
        bool IResourceLayer.HasResource(string key) => HasResource(key);
    }
}