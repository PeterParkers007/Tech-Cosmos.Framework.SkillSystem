using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Tests
{
    /// <summary>测试用宿主：实现 IUnit + IBuffHost，供条件/机制测试。</summary>
    internal sealed class TestHost : IUnit<TestHost>, IBuffHost<TestHost>
    {
        public BuffSystem<TestHost> BuffSystem { get; }
        public TagContainer Tags { get; } = new TagContainer();

        public TestHost()
        {
            BuffSystem = new BuffSystem<TestHost>(this);
        }

        public string[] GetSupportedEvents() => new[] { "OnAttack", "OnBeingHit" };
        public void TriggerEvent(string eventName, SkillContext<TestHost> context) { }
        public void AddSkill(ISkill<TestHost> skill) { }
        public void RemoveSkill(ISkill<TestHost> skill) { }
    }

    public class FormulaEvaluatorExtendedTests
    {
        [SetUp]
        public void SetUp() => FormulaEvaluator.ClearCache();

        [Test]
        public void EvaluateExpressionStatic_DivisionAndMixedOps_Works()
        {
            Assert.AreEqual(8f, FormulaEvaluator.EvaluateExpressionStatic("10/2+3"), 0.001f);
            Assert.AreEqual(7f, FormulaEvaluator.EvaluateExpressionStatic("10-3*1"), 0.001f);
        }

        [Test]
        public void EvaluateExpressionStatic_NestedParentheses_Works()
        {
            Assert.AreEqual(18f, FormulaEvaluator.EvaluateExpressionStatic("((2+1)*3)+9"), 0.001f);
        }

        [Test]
        public void EvaluateExpressionStatic_UnaryMinus_Works()
        {
            Assert.AreEqual(-5f, FormulaEvaluator.EvaluateExpressionStatic("-5"), 0.001f);
            Assert.AreEqual(5f, FormulaEvaluator.EvaluateExpressionStatic("10+-5"), 0.001f);
        }

        [Test]
        public void ClearCache_AllowsReevaluation()
        {
            FormulaEvaluator.EvaluateExpressionStatic("1+1");
            FormulaEvaluator.ClearCache();
            Assert.AreEqual(2f, FormulaEvaluator.EvaluateExpressionStatic("1+1"), 0.001f);
        }
    }

    public class CompositeConditionTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        [Test]
        public void AndCondition_RequiresAll()
        {
            var cond = new AndCondition<MockUnit>(
                new FuncCondition<MockUnit>(_ => true),
                new FuncCondition<MockUnit>(_ => false));
            Assert.IsFalse(cond.IsEligible(default, null));
        }

        [Test]
        public void OrCondition_PassesWhenAny()
        {
            var cond = new OrCondition<MockUnit>(
                new FuncCondition<MockUnit>(_ => false),
                new FuncCondition<MockUnit>(_ => true));
            Assert.IsTrue(cond.IsEligible(default, null));
        }

        [Test]
        public void NotCondition_InvertsResult()
        {
            var cond = new NotCondition<MockUnit>(new FuncCondition<MockUnit>(_ => true));
            Assert.IsFalse(cond.IsEligible(default, null));
        }
    }

    public class ConditionImplementationTests
    {
        [Test]
        public void HasTagCondition_ChecksTargetTags()
        {
            var host = new TestHost();
            host.Tags.AddTag("Stun");
            var ctx = new SkillContext<TestHost>(host, host);
            var condition = new HasTagCondition<TestHost> { requiredTag = "Stun", checkTarget = true };

            Assert.IsTrue(condition.IsEligible(ctx, null));
            host.Tags.RemoveTag("Stun");
            Assert.IsFalse(condition.IsEligible(ctx, null));
        }

        [Test]
        public void HasBuffCondition_RequiresMinStacks()
        {
            var host = new TestHost();
            host.BuffSystem.AddBuff(new SimpleBuff<TestHost>(
                host, "Poison", 5f, null, null, null, BuffStackPolicy.StackAndRefresh, 3));
            host.BuffSystem.AddBuff(new SimpleBuff<TestHost>(
                host, "Poison", 5f, null, null, null, BuffStackPolicy.StackAndRefresh, 3));

            var ctx = new SkillContext<TestHost>(host, host);
            var data = new Dictionary<string, object> { ["BuffId"] = "Poison" };
            var layer = new DataLayer<TestHost>(data);

            var ok = new HasBuffCondition<TestHost> { buffId = "Poison", minStacks = 2 };
            var fail = new HasBuffCondition<TestHost> { buffId = "Poison", minStacks = 5 };

            Assert.IsTrue(ok.IsEligible(ctx, layer));
            Assert.IsFalse(fail.IsEligible(ctx, layer));
        }

        [Test]
        public void CachedCondition_ReusesResultForSameContext()
        {
            int calls = 0;
            var inner = new FuncCondition<TestHost>(_ =>
            {
                calls++;
                return true;
            });
            var cached = new CachedCondition<TestHost>(inner);
            var ctx = new SkillContext<TestHost>(new TestHost(), null);

            cached.IsEligible(ctx, null);
            cached.IsEligible(ctx, null);

            Assert.AreEqual(1, calls);
        }
    }

    public class MechanismTreeExtendedTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        private sealed class CounterMechanism : Mechanism<MockUnit>
        {
            public int Count;
            public override void Execute(SkillContext<MockUnit> context, IDataLayer<MockUnit> dataLayer) => Count++;
        }

        [Test]
        public void CompileParallel_ExecutesAllChildren()
        {
            var a = new CounterMechanism();
            var b = new CounterMechanism();
            var tree = new MechanismTreeParallel
            {
                children = new List<MechanismTreeNodeBase>
                {
                    new MechanismTreeLeaf { mechanism = a },
                    new MechanismTreeLeaf { mechanism = b }
                }
            };

            var compiled = MechanismTreeCompiler.Compile<MockUnit>(tree);
            compiled.Execute(new SkillContext<MockUnit>(new MockUnit()), null);

            Assert.AreEqual(1, a.Count);
            Assert.AreEqual(1, b.Count);
        }

        [Test]
        public void CompileSequence_SkillBack_InvokesChildrenInReverse()
        {
            var backOrder = new List<int>();
            var first = new SkillBackTrackerMechanism<MockUnit>(1, backOrder);
            var second = new SkillBackTrackerMechanism<MockUnit>(2, backOrder);
            var seq = new SequenceMechanism<MockUnit>(first, second);

            seq.SkillBack(null);

            Assert.AreEqual(2, backOrder.Count);
            Assert.AreEqual(2, backOrder[0]);
            Assert.AreEqual(1, backOrder[1]);
        }

        private sealed class SkillBackTrackerMechanism<T> : Mechanism<T> where T : class, IUnit<T>
        {
            private readonly int _id;
            private readonly List<int> _order;

            public SkillBackTrackerMechanism(int id, List<int> order)
            {
                _id = id;
                _order = order;
            }

            public override void SkillBack(ISkill<T> skill) => _order.Add(_id);
        }
    }

    public class SkillExecutionPipelineTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        private sealed class CancelMiddleware : SkillMiddlewareBase
        {
            public override bool OnBeforeExecute<T>(ISkill<T> skill, ref SkillContext<T> context) => false;
        }

        private sealed class TrackMiddleware : SkillMiddlewareBase
        {
            public SkillExecutionResult LastResult;
            public override void OnAfterExecute<T>(ISkill<T> skill, SkillContext<T> context, SkillExecutionResult result)
                => LastResult = result;
        }

        [Test]
        public void Execute_MiddlewareCancel_ReturnsCancelled()
        {
            var skill = CreateSkill(new List<Condition<MockUnit>>());
            var result = SkillExecutionPipeline.Execute(
                skill,
                new SkillContext<MockUnit>(new MockUnit()),
                new[] { new CancelMiddleware() });

            Assert.AreEqual(SkillExecutionResult.Cancelled, result);
        }

        [Test]
        public void Execute_ConditionFailed_ReturnsConditionFailed()
        {
            var skill = CreateSkill(new List<Condition<MockUnit>>
            {
                new FuncCondition<MockUnit>(_ => false)
            });

            var result = SkillExecutionPipeline.Execute(skill, new SkillContext<MockUnit>(new MockUnit()));
            Assert.AreEqual(SkillExecutionResult.ConditionFailed, result);
        }

        [Test]
        public void Execute_Success_InvokesExecutedEvent()
        {
            var skill = CreateSkill(new List<Condition<MockUnit>>());
            bool executed = false;
            if (skill.ExecuteLayer is ExecuteLayer<MockUnit> layer)
                layer.Executed += _ => executed = true;

            SkillExecutionPipeline.Execute(skill, new SkillContext<MockUnit>(new MockUnit()));
            Assert.IsTrue(executed);
        }

        [Test]
        public void Execute_Success_RunsMiddlewareAfter()
        {
            var tracker = new TrackMiddleware();
            var skill = CreateSkill(new List<Condition<MockUnit>>());

            SkillExecutionPipeline.Execute(
                skill,
                new SkillContext<MockUnit>(new MockUnit()),
                new[] { tracker });

            Assert.AreEqual(SkillExecutionResult.Success, tracker.LastResult);
        }

        [Test]
        public void Execute_StartsTimelineOnSuccess()
        {
            var owner = new MockUnit();
            var counter = new CounterMechanism();
            var skillData = new SkillData<MockUnit>
            {
                SkillName = "TimelinePipeline",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" },
                Timeline = new SkillTimelineData
                {
                    enabled = true,
                    totalDuration = 0.5f,
                    clips = new List<SkillTimelineClip>
                    {
                        new SkillTimelineClip
                        {
                            clipType = SkillTimelineClipType.Mechanism,
                            startTime = 0.1f,
                            mechanism = counter
                        }
                    }
                }
            };
            skillData.AddMechanism(new CounterMechanism());
            var skill = SkillFactory<MockUnit>.CreateSkill(skillData);

            SkillExecutionPipeline.Execute(skill, new SkillContext<MockUnit>(owner));
            SkillTimelineService.Tick(0.2f);

            Assert.AreEqual(1, counter.Count);
            SkillTimelineService.StopAll();
        }

        private sealed class CounterMechanism : Mechanism<MockUnit>
        {
            public int Count;
            public override void Execute(SkillContext<MockUnit> context, IDataLayer<MockUnit> dataLayer) => Count++;
        }

        private static ISkill<MockUnit> CreateSkill(List<Condition<MockUnit>> conditions)
        {
            var data = new SkillData<MockUnit>
            {
                SkillName = "PipelineTest",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" },
                Conditions = conditions
            };
            data.AddMechanism(new FuncMechanism<MockUnit>());
            return SkillFactory<MockUnit>.CreateSkill(data);
        }

        private sealed class FuncMechanism<T> : Mechanism<T> where T : class, IUnit<T>
        {
            public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer) { }
        }
    }

    public class UnitEventTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        [Test]
        public void Trigger_InvokesHigherPriorityFirst()
        {
            var order = new List<int>();
            var bus = new UnitEvent<MockUnit>("OnAttack");
            bus.Subscribe("OnAttack", _ => order.Add(1), priority: 1);
            bus.Subscribe("OnAttack", _ => order.Add(10), priority: 10);

            bus.Trigger("OnAttack", new SkillContext<MockUnit>(new MockUnit()));

            Assert.AreEqual(2, order.Count);
            Assert.AreEqual(10, order[0]);
            Assert.AreEqual(1, order[1]);
        }

        [Test]
        public void Unsubscribe_StopsCallback()
        {
            int count = 0;
            var bus = new UnitEvent<MockUnit>("OnAttack");
            Action<SkillContext<MockUnit>> cb = _ => count++;
            bus.Subscribe("OnAttack", cb);
            bus.Unsubscribe("OnAttack", cb);
            bus.Trigger("OnAttack", new SkillContext<MockUnit>(new MockUnit()));

            Assert.AreEqual(0, count);
        }
    }

    public class SkillHolderTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        [Test]
        public void AddPassiveSkill_ExecutesImmediately()
        {
            bool executed = false;
            var host = new MockUnit();
            var bus = new UnitEvent<MockUnit>("OnPassive");
            var holder = new SkillHolder<MockUnit>(bus);

            var data = new SkillData<MockUnit>
            {
                SkillName = "PassiveTest",
                SkillType = SkillType.Passive,
                TriggerEvents = new List<string>()
            };
            data.AddMechanism(new ActionMechanism<MockUnit>(_ => executed = true));
            holder.AddSkill(SkillFactory<MockUnit>.CreateSkill(data), host);

            Assert.IsTrue(executed);
        }

        [Test]
        public void TriggerEvent_InvokesSubscribedActiveSkill()
        {
            int count = 0;
            var host = new MockUnit();
            var bus = new UnitEvent<MockUnit>("OnAttack");
            var holder = new SkillHolder<MockUnit>(bus);

            var data = new SkillData<MockUnit>
            {
                SkillName = "ActiveTest",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            };
            data.AddMechanism(new ActionMechanism<MockUnit>(_ => count++));
            holder.AddSkill(SkillFactory<MockUnit>.CreateSkill(data), host);

            bus.Trigger("OnAttack", new SkillContext<MockUnit>(host, host));
            Assert.AreEqual(1, count);
        }

        private sealed class ActionMechanism<T> : Mechanism<T> where T : class, IUnit<T>
        {
            private readonly Action<SkillContext<T>> _action;
            public ActionMechanism(Action<SkillContext<T>> action) => _action = action;
            public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer) => _action(context);
        }
    }

    public class DataLayerTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public float Power = 10f;
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        [Test]
        public void GetValue_FuncDelegate_EvaluatesPerContext()
        {
            var layer = new DataLayer<MockUnit>(new Dictionary<string, object>
            {
                ["Scaled"] = new Func<SkillContext<MockUnit>, float>(ctx =>
                    ctx.caster is MockUnit u ? u.Power * 2f : 0f)
            });

            var caster = new MockUnit { Power = 7f };
            var ctx = new SkillContext<MockUnit>(caster, null);
            Assert.AreEqual(14f, layer.GetValue<float>("Scaled", ctx), 0.001f);
        }
    }

    public class BuffSystemExtendedTests
    {
        [Test]
        public void DispelByTags_RemovesMatchingBuffs()
        {
            var host = new TestHost();
            host.BuffSystem.AddBuff(new SimpleBuff<TestHost>(
                host, "SlowDebuff", 5f, new[] { "Slow" }, null, null, BuffStackPolicy.ExtendDuration, 1));
            host.BuffSystem.AddBuff(new SimpleBuff<TestHost>(
                host, "StunDebuff", 5f, new[] { "Stun" }, null, null, BuffStackPolicy.ExtendDuration, 1));

            host.BuffSystem.DispelByTags("Slow");

            Assert.IsNull(host.BuffSystem.FindBuffByName("SlowDebuff"));
            Assert.IsNotNull(host.BuffSystem.FindBuffByName("StunDebuff"));
        }

        [Test]
        public void RemoveBuffsByName_RemovesAllWithSameName()
        {
            var host = new TestHost();
            host.BuffSystem.AddBuff(new SimpleBuff<TestHost>(
                host, "Mark", 5f, null, null, null, BuffStackPolicy.ExtendDuration, 1));
            host.BuffSystem.RemoveBuffsByName("Mark");
            Assert.IsNull(host.BuffSystem.FindBuffByName("Mark"));
        }

        [Test]
        public void MultiplyModifier_AppliesCorrectly()
        {
            var host = new TestHost();
            host.BuffSystem.AddBuff(new SimpleBuff<TestHost>(
                host, "Double", 10f, null, new[]
                {
                    new StatModifier { statKey = "Attack", operation = ModifierOperation.Multiply, value = 2f }
                }, null, BuffStackPolicy.ExtendDuration, 1));

            Assert.AreEqual(20f, host.BuffSystem.GetModifiedValue("Attack", 10f), 0.001f);
        }
    }

    public class MechanismMismatchTests
    {
        private sealed class MockUnitA : IUnit<MockUnitA>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnitA> context) { }
            public void AddSkill(ISkill<MockUnitA> skill) { }
            public void RemoveSkill(ISkill<MockUnitA> skill) { }
        }

        private sealed class MockUnitB : IUnit<MockUnitB>
        {
            public string[] GetSupportedEvents() => new[] { "OnAttack" };
            public void TriggerEvent(string eventName, SkillContext<MockUnitB> context) { }
            public void AddSkill(ISkill<MockUnitB> skill) { }
            public void RemoveSkill(ISkill<MockUnitB> skill) { }
        }

        [Test]
        public void ExecuteBase_LogsWarningOnTypeMismatch()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*类型不匹配.*"));
            var mech = new MismatchMechanism();
            mech.ExecuteBase(new SkillContext<MockUnitB>(new MockUnitB()), null);
        }

        private sealed class MismatchMechanism : Mechanism<MockUnitA> { }
    }
}
