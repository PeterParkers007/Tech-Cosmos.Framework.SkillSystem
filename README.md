# Tech-Cosmos 技能系统

一个基于 Unity 的泛型技能系统框架，支持 Inspector 可视化配置，适用于 RPG、动作游戏、卡牌游戏等需要灵活技能系统的项目。

---

## 目录

- [特性](#特性)
- [架构概览](#架构概览)
- [快速开始](#快速开始)
- [核心概念](#核心概念)
- [分层设计](#分层设计)
- [代码生成器](#代码生成器)
- [配置指南](#配置指南)
- [API 参考](#api-参考)
- [最佳实践](#最佳实践)

---

## 特性

- 🎯 **泛型架构**：基于 `IUnit<T>` 的泛型设计，类型安全，高性能
- 🧩 **分层解耦**：基础层、条件层、信息层、机制层、数据层、执行层
- 🎨 **Inspector 可视化**：机制和条件支持多态选择，像枚举一样切换
- 🔧 **代码生成器**：自动生成非泛型子类，解决 Unity 序列化限制
- 📦 **ScriptableObject 配置**：技能可作为资产创建和复用
- 🎪 **条件组合系统**：支持 AND/OR/NOT 逻辑运算符，支持条件池复用
- ⚡ **高性能设计**：对象池、缓存数组、for 循环优化
- 📝 **完整的事件系统**：基于字符串的事件订阅/触发机制

---

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                     SkillDataSO (ScriptableObject)          │
│  ┌─────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │ 基础层   │ │ 条件层    │ │ 信息层    │ │ 机制层    │       │
│  │SkillType│ │Conditions│ │Name      │ │Mechanisms│       │
│  │Trigger  │ │          │ │Description│ │          │       │
│  └─────────┘ └──────────┘ └──────────┘ └──────────┘       │
│                        ┌──────────┐                         │
│                        │ 数值层    │                         │
│                        │ Data     │                         │
│                        └──────────┘                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    SkillFactory<T>.CreateSkill()            │
│                        组装各层                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       ISkill<T>                             │
│  ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌──────────┐     │
│  │BaseLayer │ │Condition  │ │Mechanism │ │Execute   │     │
│  │          │ │Layer      │ │Layer     │ │Layer     │     │
│  └──────────┘ └───────────┘ └──────────┘ └──────────┘     │
└─────────────────────────────────────────────────────────────┘
```

---

## 快速开始

### 1. 创建 Unit 类

```csharp
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

[GenerateSkillDataSO(MenuName = "Tech-Cosmos/Skill/Character")]
public class Character : MonoBehaviour, IUnit<Character>
{
    public void AddSkill(ISkill<Character> skill) { }
    public void RemoveSkill(ISkill<Character> skill) { }
    public string[] GetSupportedEvents() => new[] { "OnAttack", "OnDamaged" };
    public void TriggerEvent(string eventName, SkillContext<Character> context) { }
}
```

### 2. 创建自定义机制

```csharp
using System;
using UnityEngine;

[Serializable]
[AutoGenerateMechanism(typeof(Character))]
public class DamageMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    [SerializeField] private float baseDamage = 10f;

    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        Debug.Log($"造成 {baseDamage} 点伤害");
    }
}
```

### 3. 生成代码

点击菜单 `Tech-Cosmos → SkillSystem → Generator → Generate ALL Classes`

### 4. 创建技能资产

右键 `Create → Tech-Cosmos → Skill → Character`，在 Inspector 中配置机制和条件。

### 5. 使用技能

```csharp
public class Character : MonoBehaviour, IUnit<Character>
{
    public CharacterSkillDataSO skillAsset;

    void Start()
    {
        var skill = skillAsset.CreateSkill() as ISkill<Character>;
        AddSkill(skill);
    }
}
```

---

## 核心概念

### IUnit\<T\>

所有技能使用者的基接口，采用 CRTP（奇异递归模板模式）设计：

```csharp
public interface IUnit<T> where T : class, IUnit<T>
{
    string[] GetSupportedEvents();
    void TriggerEvent(string eventName, SkillContext<T> context);
    void AddSkill(ISkill<T> skill);
    void RemoveSkill(ISkill<T> skill);
}
```

### SkillContext\<T\>

技能执行的上下文，包含释放者、目标、位置等信息：

```csharp
public struct SkillContext<T> where T : class, IUnit<T>
{
    public T caster;
    public T target;
    public Vector3 targetPos;
}
```

### 条件系统

支持逻辑运算符组合条件：

```csharp
// AND 条件
var canCast = hasMana & isAlive & !isSilenced;

// OR 条件
var canUse = isGrounded | isFlying;

// 条件池（高性能）
var condition = ConditionPool<T>.RentAnd(hasMana, isAlive);
```

---

## 分层设计

### 基础层 (BaseLayer)
负责技能触发事件的管理，分为主动和被动两种模式。

| 类型 | 说明 |
|------|------|
| `ActiveBaseLayer<T>` | 主动技能，由事件触发执行 |
| `PassiveBaseLayer<T>` | 被动技能，自动响应事件 |

### 条件层 (ConditionLayer)
管理技能释放条件，支持多条件组合。

| 条件类型 | 说明 |
|---------|------|
| `CooldownCondition<T>` | 冷却条件 |
| `FuncCondition<T>` | 自定义函数条件 |
| `AndCondition<T>` | AND 组合 |
| `OrCondition<T>` | OR 组合 |
| `NotCondition<T>` | 取反条件 |

### 信息层 (InformationLayer)
存储技能的元数据（名称、描述等）。

### 机制层 (MechanismLayer)
技能的具体执行逻辑，支持多机制组合。

```csharp
[Serializable]
[AutoGenerateMechanism(typeof(Character))]
public class DamageMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    [SerializeField] private float baseDamage = 10f;

    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        float multiplier = dataLayer?.GetValue<float>("damageMult", context) ?? 1f;
        float finalDamage = baseDamage * multiplier;
        // 执行伤害逻辑...
    }
}
```

### 数据层 (DataLayer)
运行时的键值对数据存储，支持静态值和动态公式。

```csharp
// 设置静态值
dataLayer.SetValue("damageMult", 1.5f);

// 设置动态公式
dataLayer.SetFormula("damageMult", (ctx) => ctx.caster.AttackPower * 0.1f);

// 获取值
float mult = dataLayer.GetValue<float>("damageMult", context);
```

### 执行层 (ExecuteLayer)
统一的执行流程：**条件检查 → 机制调用**。

---

## 代码生成器

### 为什么需要代码生成器？

Unity 的 `[SerializeReference]` 不支持序列化封闭泛型类型（如 `DamageMechanism<Character>`）。代码生成器自动创建非泛型子类来解决此问题。

### 可用属性

| 属性 | 用途 |
|------|------|
| `[AutoGenerateMechanism]` | 标记需要生成子类的泛型机制 |
| `[AutoGenerateCondition]` | 标记需要生成子类的泛型条件 |
| `[GenerateSkillDataSO]` | 标记需要生成 SO 的 IUnit 类 |
| `[SkillDataField]` | 从字段生成可编辑属性 |
| `[SkillDataEntry]` | 声明独立的数据键值对 |
| `[MechanismMenu]` | 自定义 Inspector 菜单分类 |

### 生成命令

| 菜单路径 | 功能 |
|---------|------|
| `Generate ALL Classes` | 一键生成所有代码 |
| `Generate Mechanism Classes` | 仅生成机制子类 |
| `Generate Condition Classes` | 仅生成条件子类 |
| `Generate SkillDataSO Only` | 仅生成 SO 类 |
| `Clear All Generated` | 清理所有生成文件 |

### 生成文件结构

```
Assets/
└─ Generated/
   ├─ Mechanisms/
   │  └─ CharacterDamageMechanism.g.cs
   ├─ Conditions/
   │  └─ CharacterCooldownCondition.g.cs
   └─ SkillDataSO/
      └─ CharacterSkill.g.cs
```

---

## 配置指南

### 创建完整技能

```csharp
// 1. 定义机制
[Serializable]
[AutoGenerateMechanism(typeof(Character))]
[MechanismMenu("⚔ 伤害", DisplayName = "基础伤害", Priority = 1)]
public class DamageMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private DamageType type = DamageType.Physical;

    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        float mult = dataLayer?.GetValue<float>("damageMult", context) ?? 1f;
        ApplyDamage(context.target, damage * mult, type);
    }
}

// 2. 定义条件
[Serializable]
[AutoGenerateCondition(typeof(Character))]
public class CooldownCondition<T> : Condition<T> where T : class, IUnit<T>
{
    [SerializeField] private float cooldown = 3f;
    private float nextTime;

    public override bool IsEligible(SkillContext<T> ctx, IDataLayer<T> dl)
        => Time.time >= nextTime;

    public void StartCooldown() => nextTime = Time.time + cooldown;
}

// 3. 创建 SO 资产
// 右键 → Create → Tech-Cosmos → Skill → Character

// 4. 配置
// - 设置 SkillType: Active
// - 设置 TriggerEvent: "OnAttack"
// - 添加 Condition: CharacterCooldownCondition (cooldown = 3)
// - 添加 Mechanism: CharacterDamageMechanism (damage = 10, type = Physical)
// - 设置 Data: damageMult = 1.5
```

### Inspector 配置界面

```
Character Skill (ScriptableObject)
├─ Skill Type: [Active ▼]
├─ Trigger Event: "OnAttack"
├─ Skill Name: "火球术"
├─ Skill Description: "发射一个火球造成伤害"
├─ ── 条件层 ──
├─ Conditions:
│  └─ [CharacterCooldownCondition]
│     └─ Cooldown: 3
├─ ── 机制层 ──
├─ Mechanisms:
│  └─ [CharacterDamageMechanism]
│     ├─ Base Damage: 10
│     └─ Damage Type: Physical
├─ ── 数值层 ──
└─ Data:
   ├─ damageMult: 1.5
   └─ manaCost: 30
```

---

## API 参考

### 核心接口

```csharp
// 技能接口
public interface ISkill<T> where T : class, IUnit<T>
{
    IBaseLayer<T> BaseLayer { get; }
    IConditionLayer<T> ConditionLayer { get; }
    IInformationLayer<T> InformationLayer { get; }
    IMechanismLayer<T> MechanismLayer { get; }
    IDataLayer<T> DataLayer { get; }
    IExecuteLayer<T> ExecuteLayer { get; }
}
```

### 工厂方法

```csharp
// 从 SkillData 创建技能
var skill = SkillFactory<Character>.CreateSkill(skillData);

// 从 SO 创建技能
var skill = skillAsset.CreateSkill() as ISkill<Character>;
```

### 事件系统

```csharp
// 订阅事件
unitEvent.Subscribe("OnAttack", (ctx) => {
    Debug.Log($"{ctx.caster} 攻击了 {ctx.target}");
});

// 触发事件
unitEvent.Trigger("OnAttack", new SkillContext<Character>(this, target));
```

### 条件组合

```csharp
// 使用运算符
var canCast = hasMana & isAlive & !isSilenced;

// 使用条件池（推荐）
var canCast = ConditionPool<T>.RentAnd(hasMana, isAlive);

// 归还条件
ConditionPool<T>.Return(canCast as AndCondition<T>);
```

### 数据操作

```csharp
// 设置静态值
dataLayer.SetValue("damageMult", 1.5f);

// 设置动态公式
dataLayer.SetFormula("criticalDamage", (ctx) => {
    return Random.value < ctx.caster.CriticalChance ? 2.0f : 1.0f;
});

// 获取值
float dmg = dataLayer.GetValue<float>("damageMult", context);
```

---

## 最佳实践

### 1. 机制设计

- 保持机制单一职责（伤害、治疗、Buff 分别独立）
- 使用 `[SerializeField]` 暴露可配置参数
- 通过 `DataLayer` 获取动态数值，避免硬编码

### 2. 条件优化

- 使用 `ConditionPool` 复用条件对象
- 对于频繁检查的条件使用 `CachedCondition`
- 复杂条件组合用运算符 `&` `|` `!`

### 3. 性能优化

- 机制列表中用 `for` 循环代替 `foreach`
- 事件系统使用缓存数组
- 条件对象池化

### 4. 代码生成

- 生成的 `.g.cs` 文件加入 `.gitignore`
- 每次添加新机制后重新生成
- 自定义菜单分类使用 `[MechanismMenu]`

### 5. 数据管理

- 静态配置用 `[SkillDataField]`
- 动态参数用 `[SkillDataEntry]`
- 运行时临时数据直接操作 `DataLayer`

---

## 依赖

- Unity 2020.3+
- 无外部依赖

## 许可证

MIT License