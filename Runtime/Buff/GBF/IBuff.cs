// ============================================================
// �ļ���IBuff.cs
// ·����TechCosmos.SkillSystem.Runtime/IBuff.cs
// ============================================================
using System;

namespace TechCosmos.SkillSystem.Runtime
{
    public delegate void BuffActionDelegate<T>(string actionName, T caster, T target, float value, string damageType) where T : class;

    public interface IBuff<T> where T : class
    {
        T target { get; set; }
        int priority { get; set; }
        bool isOver { get; set; }
        string[] tags { get; set; }
        string icon {  get; set; }

        string BuffName { get; }
        BuffStackPolicy StackPolicy { get; }
        int MaxStacks { get; }
        int CurrentStacks { get; set; }

        void TriggerApplyEvent(T target);
        void TriggerRemoveEvent(T target);
        void Apply();
        void Remove();
        void Update(float deltaTime);
        void Refresh();

        event Action<T> OnApply;
        event Action<T> OnRemove;

        void RegisterModifier(string modifyType, Func<float, BuffModifyContext<T>, float> modifier);
        float ModifyValue(string modifyType, float baseValue, BuffModifyContext<T> context = null);

        void RegisterAction(string actionName, BuffActionDelegate<T> action);
        void OnAction(string actionName, T caster, T target, float value = default, string damageType = default);
    }
}