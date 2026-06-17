using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 资源层扩展：为技能附加图标、特效、音效等路径映射。
    /// </summary>
    public static class ResourceLayerExtension
    {
        private static readonly Dictionary<object, IResourceLayer> _resourceLayers = new();

        /// <summary>通过键值对数组为技能附加资源层。</summary>
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

        /// <summary>通过字典为技能附加资源层。</summary>
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

        /// <summary>获取技能绑定的资源层。</summary>
        public static IResourceLayer<T> GetResourceLayer<T>(this ISkill<T> skill)
            where T : class, IUnit<T>
        {
            return _resourceLayers.TryGetValue(skill, out var layer)
                ? layer as IResourceLayer<T>
                : null;
        }

        /// <summary>移除技能绑定的资源层。</summary>
        public static void RemoveResourceLayer<T>(this ISkill<T> skill)
            where T : class, IUnit<T>
        {
            _resourceLayers.Remove(skill);
        }

        /// <summary>从 SkillDataSO 创建技能（工厂方法别名）。</summary>
        public static ISkill<T> CreateSkillFromSO<T>(SkillDataSO<T> skillDataSO)
            where T : class, IUnit<T>
        {
            return SkillFactory<T>.CreateSkill(skillDataSO);
        }

        /// <summary>按 key 读取资源路径。</summary>
        public static string GetResource<T>(this ISkill<T> skill, string key)
            where T : class, IUnit<T>
        {
            return skill.GetResourceLayer()?.GetResource(key);
        }
    }

    /// <summary>非泛型资源层接口，用于跨类型存储。</summary>
    public interface IResourceLayer
    {
        /// <summary>获取指定 key 的资源路径。</summary>
        string GetResource(string key);

        /// <summary>注册或覆盖资源路径。</summary>
        void RegisterResource(string key, string path);

        /// <summary>是否包含指定 key。</summary>
        bool HasResource(string key);
    }

    /// <summary>泛型资源层接口。</summary>
    public interface IResourceLayer<T> : IResourceLayer where T : class, IUnit<T>
    {
        /// <inheritdoc />
        new string GetResource(string key);

        /// <inheritdoc />
        new void RegisterResource(string key, string path);

        /// <inheritdoc />
        new bool HasResource(string key);

        /// <summary>全部资源键值映射。</summary>
        Dictionary<string, string> Resources { get; }
    }

    /// <summary>资源层默认实现，持有 key → 路径 字典。</summary>
    public class ResourceLayer<T> : IResourceLayer<T> where T : class, IUnit<T>
    {
        /// <inheritdoc />
        public Dictionary<string, string> Resources { get; private set; }

        /// <summary>所属技能实例。</summary>
        public ISkill<T> Skill { get; set; }

        /// <summary>使用初始资源字典创建资源层。</summary>
        public ResourceLayer(Dictionary<string, string> resources)
        {
            Resources = resources ?? new Dictionary<string, string>();
        }

        /// <inheritdoc />
        public string GetResource(string key) => Resources.GetValueOrDefault(key);
        string IResourceLayer.GetResource(string key) => GetResource(key);

        /// <inheritdoc />
        public void RegisterResource(string key, string path) => Resources[key] = path;
        void IResourceLayer.RegisterResource(string key, string path) => RegisterResource(key, path);

        /// <inheritdoc />
        public bool HasResource(string key) => Resources.ContainsKey(key);
        bool IResourceLayer.HasResource(string key) => HasResource(key);
    }
}
