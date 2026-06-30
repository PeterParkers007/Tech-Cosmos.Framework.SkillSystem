using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Tests
{
    /// <summary>复杂场景与鲁棒性：引导施法、FailFast、公式引用、追踪、中间件等。</summary>
    public class SkillSystemRobustnessTests
    {
        private sealed class MockUnit : IUnit<MockUnit>
        {
            public float Power = 10f;
            public string[] GetSupportedEvents() => new[] { "OnAttack", "OnBeingHit" };
            public void TriggerEvent(string eventName, SkillContext<MockUnit> context) { }
            public void AddSkill(ISkill<MockUnit> skill) { }
            public void RemoveSkill(ISkill<MockUnit> skill) { }
        }

        private sealed class CounterMechanism : Mechanism<MockUnit>
        {
            public int Count;
            public override void Execute(SkillContext<MockUnit> context, IDataLayer<MockUnit> dataLayer) => Count++;
        }

        private sealed class ThrowingMechanism : Mechanism<MockUnit>
        {
            public override void Execute(SkillContext<MockUnit> context, IDataLayer<MockUnit> dataLayer)
                => throw new InvalidOperationException("boom");
        }

        private sealed class CancelGlobalMiddleware : SkillMiddlewareBase
        {
            public override bool OnBeforeExecute<T>(ISkill<T> skill, ref SkillContext<T> context)
            {
                context.meta.cancelled = true;
                return true;
            }
        }

        [TearDown]
        public void TearDown()
        {
            SkillSystemServices.Clock = new UnitySkillClock();
            SkillSystemServices.ClearMiddleware();
            SkillExecutionTrace.Clear();
            SkillTimelineService.StopAll();
        }

        [Test]
        public void ChannelCast_CompletesAfterCastAndChannelTicks()
        {
            SkillSystemServices.Clock = new FixedSkillClock(0f, 0.5f);

            var skill = SkillFactory<MockUnit>.CreateSkill(new SkillData<MockUnit>
            {
                SkillName = "ChannelTest",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" },
                Profile = new SkillProfile { castTime = 0.5f, channelTime = 0.5f }
            });

            var controller = new SkillExecutionController<MockUnit>();
            int completed = 0;
            controller.OnCastCompleted += (_, __) => completed++;

            Assert.IsTrue(controller.TryExecute(skill, new SkillContext<MockUnit>(new MockUnit())));
            Assert.AreEqual(SkillCastPhase.Casting, controller.Phase);

            controller.Tick();
            Assert.AreEqual(SkillCastPhase.Channeling, controller.Phase);

            controller.Tick();
            Assert.AreEqual(SkillCastPhase.None, controller.Phase);
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void FailFast_StopsSubsequentMechanisms()
        {
            var second = new CounterMechanism();
            var data = new SkillData<MockUnit>
            {
                SkillName = "FailFast",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            };
            data.AddMechanism(new ThrowingMechanism());
            data.AddMechanism(second);
            var skill = SkillFactory<MockUnit>.CreateSkill(data);

            LogAssert.Expect(LogType.Error, new Regex(".*MechanismLayer.*"));
            SkillExecutionPipeline.Execute(
                skill,
                new SkillContext<MockUnit>(new MockUnit()),
                mechanismPolicy: MechanismErrorPolicy.FailFast);

            Assert.AreEqual(0, second.Count);
        }

        [Test]
        public void ContinueOnError_StillRunsSubsequentMechanisms()
        {
            var second = new CounterMechanism();
            var data = new SkillData<MockUnit>
            {
                SkillName = "Continue",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            };
            data.AddMechanism(new ThrowingMechanism());
            data.AddMechanism(second);
            var skill = SkillFactory<MockUnit>.CreateSkill(data);

            LogAssert.Expect(LogType.Error, new Regex(".*MechanismLayer.*"));
            SkillExecutionPipeline.Execute(
                skill,
                new SkillContext<MockUnit>(new MockUnit()),
                mechanismPolicy: MechanismErrorPolicy.ContinueOnError);

            Assert.AreEqual(1, second.Count);
        }

        [Test]
        public void FormulaEvaluator_ResolvesCasterAndTargetFields()
        {
            var caster = new MockUnit { Power = 12f };
            var target = new MockUnit { Power = 3f };
            var ctx = new SkillContext<MockUnit>(caster, target);

            Assert.AreEqual(15f, FormulaEvaluator.Evaluate(ctx, "caster.Power+target.Power"), 0.001f);
        }

        [Test]
        public void FormulaEvaluator_RandomUsesProvider()
        {
            SkillSystemServices.Random = new FixedRandomProvider(7f);
            var ctx = new SkillContext<MockUnit>(new MockUnit());
            Assert.AreEqual(7f, FormulaEvaluator.Evaluate(ctx, "random(1,10)"), 0.001f);
        }

        [Test]
        public void SkillExecutionTrace_RecordsExecution()
        {
            SkillExecutionTrace.Clear();
            SkillSystemServices.Clock = new FixedSkillClock(42f);

            var skill = SkillFactory<MockUnit>.CreateSkill(new SkillData<MockUnit>
            {
                SkillName = "Traced",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            });

            SkillExecutionPipeline.Execute(skill, new SkillContext<MockUnit>(new MockUnit()));

            Assert.Greater(SkillExecutionTrace.Count, 0);
            var recent = SkillExecutionTrace.GetRecentEntries(1);
            Assert.AreEqual("Traced", recent[0].skillName);
            Assert.AreEqual(SkillExecutionResult.Success, recent[0].result);
            Assert.AreEqual(42f, recent[0].timestamp, 0.001f);
        }

        [Test]
        public void GlobalMiddleware_CancelBlocksPipeline()
        {
            var mw = new CancelGlobalMiddleware();
            SkillSystemServices.RegisterMiddleware(mw);

            var counter = new CounterMechanism();
            var data = new SkillData<MockUnit>
            {
                SkillName = "Blocked",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            };
            data.AddMechanism(counter);
            var skill = SkillFactory<MockUnit>.CreateSkill(data);

            var result = SkillExecutionPipeline.Execute(skill, new SkillContext<MockUnit>(new MockUnit()));
            Assert.AreEqual(SkillExecutionResult.Cancelled, result);
            Assert.AreEqual(0, counter.Count);
        }

        [Test]
        public void WithResources_StoresAndRetrievesPaths()
        {
            var skill = SkillFactory<MockUnit>.CreateSkill(new SkillData<MockUnit>
            {
                SkillName = "ResSkill",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            }).WithResources(("Icon", "Icons/Fire"));

            Assert.AreEqual("Icons/Fire", skill.GetResource("Icon"));
            Assert.IsTrue(skill.GetResourceLayer().HasResource("Icon"));
        }

        [Test]
        public void BuffUpdate_RemovesExpiredBuff()
        {
            var host = new TestHost();
            host.BuffSystem.AddBuff(new SimpleBuff<TestHost>(
                host, "Short", 1f, null, null, null, BuffStackPolicy.ExtendDuration, 1));

            host.BuffSystem.BuffUpdate(1.1f);
            Assert.IsNull(host.BuffSystem.FindBuffByName("Short"));
        }

        [Test]
        public void RemoveSkill_StopsSubscribedTrigger()
        {
            int count = 0;
            var host = new MockUnit();
            var bus = new UnitEvent<MockUnit>("OnAttack");
            var holder = new SkillHolder<MockUnit>(bus);

            var data = new SkillData<MockUnit>
            {
                SkillName = "Removable",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            };
            data.AddMechanism(new ActionMechanism(_ => count++));
            var skill = SkillFactory<MockUnit>.CreateSkill(data);
            holder.AddSkill(skill, host);

            bus.Trigger("OnAttack", new SkillContext<MockUnit>(host, host));
            holder.RemoveSkill(skill);
            bus.Trigger("OnAttack", new SkillContext<MockUnit>(host, host));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void TimelineClip_ExecutesOnlyOncePerPlay()
        {
            var counter = new CounterMechanism();
            var timeline = new SkillTimelineData
            {
                enabled = true,
                totalDuration = 2f,
                clips = new List<SkillTimelineClip>
                {
                    new SkillTimelineClip
                    {
                        clipType = SkillTimelineClipType.Mechanism,
                        startTime = 0f,
                        mechanism = counter
                    }
                }
            };

            var skill = SkillFactory<MockUnit>.CreateSkill(new SkillData<MockUnit>
            {
                SkillName = "Once",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" }
            });

            SkillTimelineService.Play(skill, new SkillContext<MockUnit>(new MockUnit()), timeline);
            SkillTimelineService.Tick(0.1f);
            SkillTimelineService.Tick(0.5f);

            Assert.AreEqual(1, counter.Count);
        }

        [Test]
        public void ExecutionPriority_HigherSkillCanReplaceBusyCast()
        {
            SkillSystemServices.Clock = new FixedSkillClock(0f, 0.1f);

            var low = SkillFactory<MockUnit>.CreateSkill(new SkillData<MockUnit>
            {
                SkillName = "Low",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" },
                Profile = new SkillProfile { castTime = 2f, canBeInterrupted = true, executionPriority = 1 }
            });

            var high = SkillFactory<MockUnit>.CreateSkill(new SkillData<MockUnit>
            {
                SkillName = "High",
                SkillType = SkillType.Active,
                TriggerEvents = new List<string> { "OnAttack" },
                Profile = new SkillProfile { castTime = 1f, canBeInterrupted = true, executionPriority = 10 }
            });

            var controller = new SkillExecutionController<MockUnit>();
            int interrupted = 0;
            controller.OnCastInterrupted += (_, __) => interrupted++;

            Assert.IsTrue(controller.TryExecute(low, new SkillContext<MockUnit>(new MockUnit())));
            Assert.IsTrue(controller.TryExecute(high, new SkillContext<MockUnit>(new MockUnit())));

            Assert.AreEqual(1, interrupted);
            Assert.AreEqual("High", controller.ActiveSkill.InformationLayer.Name);
        }

        private sealed class ActionMechanism : Mechanism<MockUnit>
        {
            private readonly Action<SkillContext<MockUnit>> _action;
            public ActionMechanism(Action<SkillContext<MockUnit>> action) => _action = action;
            public override void Execute(SkillContext<MockUnit> context, IDataLayer<MockUnit> dataLayer) => _action(context);
        }

        private sealed class FixedRandomProvider : IRandomProvider
        {
            private readonly float _value;
            public FixedRandomProvider(float value) => _value = value;
            public float Value => _value;
            public float Range(float min, float max) => _value;
            public int Range(int min, int maxExclusive) => (int)_value;
        }
    }
}
