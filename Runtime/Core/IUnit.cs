// 核心泛型接口
namespace TechCosmos.SkillSystem.Runtime
{
    public interface IUnit<T> where T : class, IUnit<T>
    {
        string[] GetSupportedEvents();
        void TriggerEvent(string eventName, SkillContext<T> context);
        void AddSkill(ISkill<T> skill);
        void RemoveSkill(ISkill<T> skill);
    }
}
