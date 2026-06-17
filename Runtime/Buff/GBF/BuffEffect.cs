// BuffEffectBase.cs
using System;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    [Serializable]
    public abstract class BuffEffectBase
    {
        public abstract void ExecuteBase(object target, BuffContextBase context);
    }

    [Serializable]
    public abstract class BuffEffect<T> : BuffEffectBase where T : class
    {
        public override void ExecuteBase(object target, BuffContextBase context)
        {
            if (target is T typedTarget && context is BuffContext<T> typedContext)
                Execute(typedTarget, typedContext);
        }

        public abstract void Execute(T target, BuffContext<T> context);
    }

    public class BuffContextBase
    {
        public float deltaTime;
        public float elapsedTime;
        public float progress;
        public int currentStacks;
        public object source;
    }

    public class BuffContext<T> : BuffContextBase where T : class
    {
        // �� new ���ػ���� source���ṩǿ���ͷ���
        public new T source
        {
            get => base.source as T;
            set => base.source = value;
        }
    }
}