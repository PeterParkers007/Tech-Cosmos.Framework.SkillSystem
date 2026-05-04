# 🎮 TechCosmos 技能系统框架 (TechCosmos Skill System)

> **版本**: 1.0 | **Unity 版本**: 2021.3+ | **语言**: C# 9.0+

---

## 📑 目录

- [1. 概述](#1-概述)
- [2. 核心特性](#2-核心特性)
- [3. 架构设计](#3-架构设计)
- [4. 快速开始](#4-快速开始)
- [5. 核心概念](#5-核心概念)
- [6. 编辑器工具](#6-编辑器工具)
- [7. 代码生成系统](#7-代码生成系统)
- [8. API 参考](#8-api-参考)
- [9. 高级用法](#9-高级用法)
- [10. 最佳实践](#10-最佳实践)
- [11. 项目结构](#11-项目结构)
- [12. 常见问题](#12-常见问题)
- [13. 更新日志](#13-更新日志)

---

## 1. 概述

TechCosmos 技能系统是一个专为 **Unity** 设计的、基于 **分层架构** 与 **领域驱动设计 (DDD)** 的 **模块化**、**数据驱动** 技能框架。它将技能的执行流程抽象为六个独立的层级，通过泛型和代码生成技术，在保持高度灵活性的同时提供了出色的编辑器体验。

### 🎯 设计哲学

| 原则 | 说明 |
|------|------|
| **关注点分离** | 每个层级只负责自己的职责 |
| **数据驱动** | 技能配置完全通过 ScriptableObject 管理 |
| **泛型类型安全** | 编译时确保类型正确性 |
| **编辑器优先** | 强大的自定义 Inspector 和代码生成 |
| **性能优化** | 对象池、缓存数组、零 GC 分配 |

---

## 2. 核心特性

### ✨ 六大核心特性

1. **🏗️ 六层分层架构**
   - 基础层 → 信息层 → 条件层 → 机制层 → 数据层 → 执行层
   - 每层职责清晰，可独立扩展

2. **🧩 泛型 + 非泛型双轨系统**
   - 泛型基类保证类型安全
   - 非泛型基类支持 Unity `[SerializeReference]`

3. **🛠️ 强大的编辑器工具**
   - 自定义 SkillDataSO 编辑器窗口
   - 机制/条件多态选择抽屉 (PropertyDrawer)
   - TriggerEvent 枚举可视化编辑器
   - 公式编辑器（支持语法高亮、字段引用插入）

4. **🤖 自动化代码生成**
   - 根据 `IUnit<T>` 实现自动生成 `SkillDataSO<T>` 子类
   - 自动生成封闭泛型机制类和条件类
   - 自动扫描并更新 `TriggerEventType` 枚举

5. **📊 数据层与公式系统**
   - 支持静态值、引用值、表达式、自定义公式
   - 自定义公式支持 `caster.Attack * 1.5 + target.MaxHealth * 0.1`
   - 运行时公式求值器，支持括号、运算符优先级

6. **⚡ 性能优化机制**
   - `ConditionPool<T>` 条件对象池
   - `UnitEvent<T>` 缓存监听器数组
   - `CachedCondition<T>` 条件结果缓存

---

## 3. 架构设计

### 3.1 六层架构总览

```
┌─────────────────────────────────────────────────┐
│                  ISkill<T>                       │
│  ┌───────────────────────────────────────────┐  │
│  │         IBaseLayer<T> (基础层)             │  │
│  │  负责：触发事件注册、技能激活入口           │  │
│  │  实现：ActiveBaseLayer / PassiveBaseLayer  │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │       IInformationLayer<T> (信息层)        │  │
│  │  负责：技能名称、描述等元数据               │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │       IConditionLayer<T> (条件层)          │  │
│  │  负责：前置条件检查                         │  │
│  │  支持：AND/OR/NOT 组合条件                  │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │       IMechanismLayer<T> (机制层)          │  │
│  │  负责：技能具体效果执行                     │  │
│  │  支持：函数式机制 + 对象式机制              │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │         IDataLayer<T> (数据层)             │  │
│  │  负责：动态数据存储与公式求值               │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │        IExecuteLayer<T> (执行层)           │  │
│  │  负责：协调条件检查和机制执行               │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

### 3.2 数据流图

```
Unit 触发事件
    │
    ▼
UnitEvent<T>.Trigger(eventName, context)
    │
    ▼
BaseLayer<T>.Trigger(context)
    │
    ▼
ExecuteLayer<T>.Execute(context)
    │
    ├──► ConditionLayer<T>.CheckCondition(context)
    │       │
    │       ├──► Condition<T>[0].IsEligible()
    │       ├──► Condition<T>[1].IsEligible()
    │       └──► ...
    │
    └──► [条件通过] MechanismLayer<T>.Mechanism(context)
            │
            ├──► FuncMechanism(context)  // 函数式机制
            └──► Mechanism<T>.Execute()   // 对象式机制
                    │
                    └──► DataLayer<T>.GetValue<T>()  // 公式求值
```

### 3.3 类层次结构

```
ConditionBase (非泛型, [Serializable])
    └── Condition<T> (泛型抽象类)
            ├── AndCondition<T>      // 逻辑与
            ├── OrCondition<T>       // 逻辑或
            ├── NotCondition<T>      // 逻辑非
            ├── CachedCondition<T>   // 缓存装饰器
            ├── FuncCondition<T>     // 函数式条件
            └── CooldownCondition<T> // 冷却条件

MechanismBase (非泛型, [Serializable])
    └── Mechanism<T> (泛型抽象类)
            └── 用户自定义机制...

SkillDataSO (非泛型基类)
    └── SkillDataSO<T> (泛型基类)
            └── 自动生成的 CharacterSkillDataSO 等

ISkill<T>
    └── Skill<T> (具体实现)
```

---

## 4. 快速开始

### 4.1 安装

1. 将整个 `TechCosmos.SkillSystem` 文件夹复制到你的 Unity 项目的 `Assets` 目录下
2. 确保项目使用 `.NET Standard 2.1` 或更高版本
3. 等待 Unity 编译完成

### 4.2 第一步：创建你的第一个 Unit

```csharp
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

// 1. 标记这个类需要生成 SkillDataSO
[GenerateSkillDataSO(MenuName = "Tech-Cosmos/Skill/Character")]
public class Character : MonoBehaviour, IUnit<Character>
{
    // 2. 标记需要暴露到数据层的字段
    [SkillDataField(Category = "战斗属性", DisplayName = "攻击力")]
    public float Attack = 10f;

    [SkillDataField(Category = "战斗属性", DisplayName = "防御力")]
    public float Defense = 5f;

    [SkillDataField(Category = "生命属性", DisplayName = "最大生命值")]
    public float MaxHealth = 100f;

    private float _health;
    public float Health
    {
        get => _health;
        set => _health = Mathf.Clamp(value, 0, MaxHealth);
    }

    // 3. 声明支持的事件
    public string[] GetSupportedEvents() => new[] { "OnAttack", "OnDamaged", "OnHeal" };

    // 4. 事件触发逻辑
    public void TriggerEvent(string eventName, SkillContext<Character> context)
    {
        // 事件系统会自动分发给订阅的技能
    }

    public void AddSkill(ISkill<Character> skill) { /* 添加到 UnitEvent */ }
    public void RemoveSkill(ISkill<Character> skill) { /* 移除 */ }

    private void Awake()
    {
        Health = MaxHealth;
    }
}
```

### 4.3 第二步：生成代码

```
菜单栏 → Tech-Cosmos → SkillSystem → Generator → Generate ALL Classes
```

这会自动生成：
- `Assets/Generated/Mechanisms/` - 机制封闭泛型类
- `Assets/Generated/Conditions/` - 条件封闭泛型类
- `Assets/Generated/SkillDataSO/` - 技能数据配置类

### 4.4 第三步：创建自定义机制

```csharp
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

[Serializable]
// 标记自动生成封闭泛型类
[AutoGenerateMechanism(typeof(Character))]
// 可选：编辑器菜单分类
[MechanismMenu("⚔ 伤害", DisplayName = "伤害机制", Priority = 1)]
public class DamageMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float attackMultiplier = 1.5f;
    [SerializeField] private float defensePenetration = 0f;

    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        // 从数据层获取动态值
        float casterAttack = dataLayer.GetValue<float>("Attack", context);
        float targetDefense = dataLayer.GetValue<float>("Defense", context);

        // 计算伤害
        float damage = (baseDamage + casterAttack * attackMultiplier)
                        - Mathf.Max(0, targetDefense - defensePenetration);

        damage = Mathf.Max(0, damage);

        Debug.Log($"[DamageMechanism] 造成 {damage} 点伤害");

        // 这里调用实际的伤害系统
        // context.Target.TakeDamage(damage);
    }
}
```

### 4.5 第四步：配置技能

1. 在 Project 窗口右键 → `Create → Tech-Cosmos → Skill → Character`
2. 选择生成的 `NewCharacterSkill` 资产
3. 使用 **技能编辑器** (`Tech-Cosmos → SkillSystem → Skill Editor Window`) 配置：
   - 基础信息：技能名称、触发事件
   - 条件列表：冷却条件、生命值条件等
   - 机制列表：选择刚创建的伤害机制
   - 数据层：添加公式值

### 4.6 第五步：运行时创建技能

```csharp
// 方式一：从 SkillDataSO 创建
SkillDataSO<Character> characterSkillSO = Resources.Load<SkillDataSO<Character>>("Skills/CharacterSkill");
ISkill<Character> skill = SkillFactory<Character>.CreateSkill(characterSkillSO);

// 方式二：代码构建
var skillData = new SkillData<Character>
{
    SkillType = SkillType.Active,
    TriggerEvent = "OnAttack",
    SkillName = "重击",
    SkillDescription = "造成 150% 攻击力的伤害"
};

// 添加条件
skillData.AddCondition(new CooldownCondition<Character>(3f));
skillData.AddCondition(new FuncCondition<Character>(ctx => ctx.caster.Health > 0));

// 添加机制
skillData.AddMechanism(ctx =>
{
    Debug.Log($"执行重击技能: {ctx.caster} → {ctx.target}");
});

// 创建技能
ISkill<Character> codeSkill = SkillFactory<Character>.CreateSkill(skillData);
```

---

## 5. 核心概念

### 5.1 IUnit<T> 接口

所有参与技能系统的实体必须实现此接口：

```csharp
public interface IUnit<T> where T : class, IUnit<T>
{
    // 返回该 Unit 支持的事件列表
    string[] GetSupportedEvents();

    // 触发指定事件
    void TriggerEvent(string eventName, SkillContext<T> context);

    // 管理技能
    void AddSkill(ISkill<T> skill);
    void RemoveSkill(ISkill<T> skill);
}
```

### 5.2 SkillContext<T>

技能执行的上下文，包含施法者和目标信息：

```csharp
public struct SkillContext<T> where T : class, IUnit<T>
{
    public T caster;        // 施法者
    public T target;        // 目标
    public Vector3 targetPos; // 目标位置

    // 支持隐式转换为非泛型上下文
    public static implicit operator SkillContextBase(SkillContext<T> ctx) => ...;
}
```

### 5.3 技能类型

```csharp
public enum SkillType
{
    Active,   // 主动技能（由事件触发）
    Passive   // 被动技能（常驻效果）
}
```

### 5.4 条件系统

条件系统支持逻辑组合，使用运算符重载：

```csharp
// AND 条件：所有条件都满足
var andCondition = conditionA & conditionB;

// OR 条件：任一条件满足
var orCondition = conditionA | conditionB;

// NOT 条件：取反
var notCondition = !conditionA;

// 复杂组合
var complex = (conditionA & conditionB) | !conditionC;
```

内置条件类型：

| 条件类 | 说明 |
|--------|------|
| `AndCondition<T>` | 逻辑与，所有子条件满足才通过 |
| `OrCondition<T>` | 逻辑或，任一子条件满足即通过 |
| `NotCondition<T>` | 逻辑非，对子条件取反 |
| `CachedCondition<T>` | 缓存装饰器，避免重复计算 |
| `FuncCondition<T>` | 函数式条件，直接传入 Lambda |
| `CooldownCondition<T>` | 冷却时间条件 |

### 5.5 数据层与公式系统

数据层支持四种公式类型：

```csharp
public enum FormulaType
{
    Static,      // 静态值: 直接返回 .staticValue
    Reference,   // 引用值: 从上下文路径取值 + 操作符运算
    Expression,  // 表达式: 类似 Reference，使用 multiplier 和 offset
    Custom       // 自定义: 支持完整公式语法
}
```

自定义公式语法示例：

```
caster.Attack * 1.5 + target.MaxHealth * 0.1
(caster.Attack - target.Defense) * 2.0
caster.Runtime.Level * 10 + 100
```

### 5.6 事件系统

`UnitEvent<T>` 提供高性能的事件订阅/发布：

```csharp
// 创建事件系统
var unitEvent = new UnitEvent<Character>("OnAttack", "OnDamaged", "OnHeal");

// 订阅事件
unitEvent.Subscribe("OnAttack", context =>
{
    Debug.Log($"{context.caster} 攻击了 {context.target}");
});

// 触发事件
unitEvent.Trigger("OnAttack", new SkillContext<Character>(attacker, defender));

// 查询订阅者数量
int count = unitEvent.GetSubscriberCount("OnAttack");
```

---

## 6. 编辑器工具

### 6.1 技能编辑器窗口 (Skill Editor Window)

**打开方式**：`Tech-Cosmos → SkillSystem → Skill Editor Window`

**主要功能**：

- ✅ 可视化编辑所有技能属性
- ✅ 条件和机制的多态选择
- ✅ 数据层可视化编辑（带类型着色）
- ✅ 公式编辑器（语法帮助 + 字段引用插入）
- ✅ 自动保存和缓存优化

### 6.2 机制选择抽屉 (MechanismDrawer)

- 自动按分类分组显示
- 支持 "Switch"（切换）、"Copy"（复制 JSON）、"Remove"（删除）操作
- 类型安全筛选（只显示可实例化的非泛型子类）

### 6.3 触发事件枚举编辑器

**打开方式**：`Tech-Cosmos → SkillSystem → TriggerEvent Enum Editor`

- 可视化管理所有触发事件类型
- 支持添加/删除/重命名事件
- 自动生成 `TriggerEventType.cs` 文件

### 6.4 创建技能脚本窗口

 **打开方式**：`Tech-Cosmos → SkillSystem → Create Skill Script`

- 可视化选择目标 Unit 类型
- 自动生成机制/条件模板代码
- 支持命名空间和类名自定义

---

## 7. 代码生成系统

### 7.1 生成流程

```
[标记 Attribute]
    │
    ├── [GenerateSkillDataSO]  在 IUnit 实现类上
    ├── [SkillDataField]       在字段上
    ├── [AutoGenerateMechanism] 在泛型 Mechanism 子类上
    ├── [AutoGenerateCondition] 在泛型 Condition 子类上
    └── [GenerateMechanismsFor] 在 IUnit 实现类上（批量生成）
            │
            ▼
    菜单: Generate ALL Classes
            │
            ▼
    生成以下文件:
    ├── Assets/Generated/Mechanisms/*.g.cs
    ├── Assets/Generated/Conditions/*.g.cs
    └── Assets/Generated/SkillDataSO/*.g.cs
```

### 7.2 可用的特性标记 (Attributes)

| 特性 | 目标 | 作用 |
|------|------|------|
| `[GenerateSkillDataSO]` | Class (IUnit 实现) | 标记生成 SkillDataSO 子类 |
| `[GenerateMechanismsFor]` | Class (IUnit 实现) | 为其生成所有无指定目标类型的机制 |
| `[AutoGenerateMechanism]` | Class (泛型 Mechanism) | 指定目标 Unit 类型或全局生成 |
| `[AutoGenerateCondition]` | Class (泛型 Condition) | 指定目标 Unit 类型 |
| `[SkillDataField]` | Field/Property | 暴露字段到数据层 |
| `[SkillDataEntry]` | Field/Property | 添加额外的数据条目 |
| `[DataEntryType]` | Class/Struct | 标记可添加到数据层的自定义类型 |
| `[MechanismMenu]` | Class | 自定义编辑器菜单分类 |

### 7.3 菜单功能一览

```
Tech-Cosmos/SkillSystem/
├── Generator/
│   ├── Generate ALL Classes          (生成所有代码)
│   ├── Generate Mechanism Classes     (仅生成机制)
│   ├── Generate Condition Classes    (仅生成条件)
│   ├── Generate SkillDataSO Only     (仅生成数据配置)
│   └── Clear All Generated           (清理生成文件)
├── Skill Editor Window               (技能编辑器)
├── TriggerEvent Enum Editor          (枚举编辑器)
├── Create Skill Script               (创建技能脚本)
└── Documentation                     (文档)
```

---

## 8. API 参考

### 8.1 SkillFactory<T>

```csharp
public static class SkillFactory<T> where T : class, IUnit<T>
{
    // 从 SkillData 数据对象创建
    public static ISkill<T> CreateSkill(SkillData<T> data);

    // 从 SkillDataSO 资产创建
    public static ISkill<T> CreateSkill(SkillDataSO<T> skillDataSO);
}
```

### 8.2 SkillData<T>

技能的纯数据表示，用于运行时创建：

```csharp
public class SkillData<T> where T : class, IUnit<T>
{
    public SkillType SkillType;          // 主动/被动
    public string TriggerEvent;          // 触发事件名
    public List<Condition<T>> Conditions; // 条件列表
    public string SkillName;             // 技能名称
    public string SkillDescription;      // 技能描述
    public List<Action<SkillContext<T>>> FuncMechanisms;  // 函数式机制
    public List<MechanismBase> Mechanisms;  // 对象式机制
    public Dictionary<string, object> Data;  // 数据层

    // 添加/移除机制
    public void AddMechanism(Action<SkillContext<T>> mechanism);
    public void AddMechanism(Mechanism<T> mechanism);
    public void ClearMechanism();

    // 添加/移除条件
    public void AddCondition(Condition<T> condition);
    public void ClearCondition();

    // 数据操作
    public void SetValue<TValue>(string key, TValue value);
    public TValue GetValue<TValue>(string key);
}
```

### 8.3 ConditionBase / Condition<T>

```csharp
// 非泛型基类 (可用于 SerializeReference)
public abstract class ConditionBase
{
    public abstract bool IsEligible(object context, IDataLayerBase dataLayer);
}

// 泛型基类
public abstract class Condition<T> : ConditionBase where T : class, IUnit<T>
{
    public abstract bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer);

    // 运算符重载
    public static Condition<T> operator &(Condition<T> left, Condition<T> right);  // AND
    public static Condition<T> operator |(Condition<T> left, Condition<T> right);  // OR
    public static Condition<T> operator !(Condition<T> condition);                 // NOT
}
```

### 8.4 MechanismBase / Mechanism<T>

```csharp
// 非泛型基类
public abstract class MechanismBase
{
    public abstract void ExecuteBase(object context, IDataLayerBase dataLayer);
    public virtual string GetDisplayName();
}

// 泛型基类
public abstract class Mechanism<T> : MechanismBase where T : class, IUnit<T>
{
    public virtual void Execute(SkillContext<T> context, IDataLayer<T> dataLayer);
    public virtual void SkillBack(ISkill<T> skill);
}
```

### 8.5 FormulaEvaluator

```csharp
public static class FormulaEvaluator
{
    // 泛型版本
    public static float Evaluate<T>(SkillContext<T> context, string formula)
        where T : class, IUnit<T>;

    // 非泛型版本
    public static float Evaluate(SkillContextBase context, string formula);
}
```

### 8.6 UnitEvent<T>

```csharp
public class UnitEvent<T> where T : class, IUnit<T>
{
    public void Subscribe(string eventName, Action<SkillContext<T>> action);
    public void Unsubscribe(string eventName, Action<SkillContext<T>> action);
    public void Trigger(string eventName, SkillContext<T> skillContext);
    public int GetSubscriberCount(string eventName);
    public bool HasEvent(string eventName);
    public void ClearEvent(string eventName);
    public void ClearAllEvents();
}
```

---

## 9. 高级用法

### 9.1 条件组合与对象池

```csharp
// 使用运算符组合条件（自动使用对象池）
var healthCheck = new FuncCondition<Character>(ctx => ctx.caster.Health > 0);
var manaCheck = new FuncCondition<Character>(ctx => ctx.caster.Mana > 10);

// 自动从 ConditionPool 租用
Condition<Character> combined = healthCheck & manaCheck;

// 使用完毕后归还（通常在使用者内部处理）
// 或使用 using 模式
```

### 9.2 自定义条件缓存

当条件计算开销较大时，使用 `CachedCondition<T>` 包装：

```csharp
var heavyCondition = new FuncCondition<Character>(ctx =>
{
    // 模拟耗时计算
    return Vector3.Distance(ctx.caster.Position, ctx.target.Position) < 5f;
});

// 包装为缓存条件，相同上下文直接返回缓存结果
var cached = new CachedCondition<Character>(heavyCondition);
```

### 9.3 动态公式与数据引用

在 `SkillDataSO` 编辑器中配置自定义公式：

```
// 伤害公式：攻击力 * 倍率 - 防御力 * 0.5
caster.Attack * 2.0 - target.Defense * 0.5

// 治疗公式：最大生命值 * 0.1 + 魔法攻击 * 0.3
caster.MagicAttack * 0.3 + target.MaxHealth * 0.1

// 复合公式：(攻击力 + 防御力) * 等级系数
(caster.Attack + caster.Defense) * caster.Level * 0.1
```

在机制中获取公式值：

```csharp
public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
{
    float damage = dataLayer.GetValue<float>("DamageFormula", context);
    // damage 已经是计算后的结果
}
```

### 9.4 技能回调

机制可以实现 `SkillBack` 方法，在技能被移除或角色死亡时执行清理：

```csharp
public class BuffMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    [SerializeField] private float attackBonus = 10f;

    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        // 施加增益
        // context.target.Attack += attackBonus;
    }

    public override void SkillBack(ISkill<T> skill)
    {
        // 移除增益
        // skill.BaseLayer.context.target.Attack -= attackBonus;
    }
}
```

### 9.5 异步/协程机制

Unity 协程可以在机制中使用：

```csharp
public class DOTMechanism<T> : Mechanism<T> where T : class, IUnit<T>, IUnit<T>
{
    [SerializeField] private float duration = 3f;
    [SerializeField] private float tickInterval = 0.5f;

    public override async void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        // 方式一：使用 MonoBehaviour 辅助启动协程
        var helper = new GameObject("DOTHelper").AddComponent<MonoBehaviourHelper>();
        helper.StartCoroutine(DOTCoroutine(context, helper));
    }

    private IEnumerator DOTCoroutine(SkillContext<T> context, MonoBehaviourHelper helper)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 造成持续伤害
            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
        Object.Destroy(helper.gameObject);
    }
}
```

---

## 10. 最佳实践

### 10.1 项目组织建议

```
Assets/
├── TechCosmos.SkillSystem/     ← 框架代码（只读）
├── _Game/
│   ├── Scripts/
│   │   ├── Units/              ← IUnit 实现类
│   │   ├── Mechanisms/         ← 自定义机制
│   │   ├── Conditions/         ← 自定义条件
│   │   └── DataEntryTypes/     ← 自定义数据类型
│   └── Resources/
│       └── Skills/             ← SkillDataSO 资产
└── Generated/                  ← 自动生成的代码（.gitignore）
```

### 10.2 性能优化建议

1. **尽量使用函数式机制**：`Action<SkillContext<T>>` 比 `Mechanism<T>` 更轻量
2. **缓存频繁访问的数据**：在 `CachedCondition<T>` 中缓存昂贵计算
3. **使用对象池**：频繁创建/销毁的条件对象使用 `ConditionPool<T>`
4. **批量生成代码后清理**：生成的 `.g.cs` 文件可加入 `.gitignore`

### 10.3 设计建议

1. **每个机制只做一件事**：如 `DamageMechanism`、`HealMechanism`、`BuffMechanism`
2. **条件小而专注**：如 `HealthAboveCondition`、`HasBuffCondition`
3. **数据公式与机制分离**：数值计算放在数据层公式中，逻辑放在机制中
4. **使用 SkillDataField 暴露字段**：让策划可以调整数值而不修改代码

### 10.4 版本控制建议

```gitignore
# 自动生成的代码
Assets/Generated/

# SkillDataSO 资产可选择是否提交
# Assets/_Game/Resources/Skills/*.asset
```

---

## 11. 项目结构

```
TechCosmos.SkillSystem/
│
├── Runtime/                           # 运行时核心代码
│   ├── Interfaces/
│   │   ├── IUnit.cs                   # 核心 Unit 接口
│   │   ├── ISkill.cs                  # 技能组合接口
│   │   └── ISkillLayer.cs             # 层接口基类
│   │   ├── IBaseLayer.cs              # 基础层接口
│   │   ├── IConditionLayer.cs         # 条件层接口
│   │   ├── IDataLayer.cs              # 数据层接口
│   │   ├── IExecuteLayer.cs           # 执行层接口
│   │   ├── IInformationLayer.cs       # 信息层接口
│   │   └── IMechanismLayer.cs         # 机制层接口
│   │
│   ├── Core/
│   │   ├── Skill.cs                   # 技能组合实现
│   │   ├── SkillContext.cs            # 技能上下文
│   │   ├── SkillContextBase.cs        # 非泛型上下文
│   │   ├── SkillData.cs               # 技能纯数据类
│   │   ├── SkillDataSO.cs             # ScriptableObject 配置基类
│   │   ├── SkillFactory.cs            # 技能工厂
│   │   ├── SkillHolder.cs             # 技能持有者
│   │   └── SkillType.cs               # 技能类型枚举
│   │
│   ├── Layers/
│   │   ├── BaseLayer.cs               # 基础层抽象
│   │   ├── ActiveBaseLayer.cs         # 主动技能基础层
│   │   ├── PassiveBaseLayer.cs        # 被动技能基础层
│   │   ├── ConditionLayer.cs          # 条件层实现
│   │   ├── DataLayer.cs               # 数据层实现
│   │   ├── ExecuteLayer.cs            # 执行层实现
│   │   ├── InformationLayer.cs        # 信息层实现
│   │   └── MechanismLayer.cs          # 机制层实现
│   │
│   ├── Conditions/                    # 条件系统
│   │   ├── Condition.cs               # 泛型条件基类
│   │   ├── ConditionBase.cs           # 非泛型条件基类
│   │   ├── AndCondition.cs            # AND 组合
│   │   ├── OrCondition.cs             # OR 组合
│   │   ├── NotCondition.cs            # NOT 组合
│   │   ├── CachedCondition.cs         # 缓存装饰器
│   │   ├── FuncCondition.cs           # 函数式条件
│   │   ├── CooldownCondition.cs       # 冷却条件
│   │   └── ConditionPool.cs           # 条件对象池
│   │
│   ├── Mechanisms/                    # 机制系统
│   │   └── Mechanism.cs               # 泛型/非泛型机制基类
│   │
│   ├── Data/                          # 数据系统
│   │   ├── FormulaEvaluator.cs        # 公式求值器
│   │   └── FormulaValue.cs            # 公式值类型
│   │
│   ├── Events/                        # 事件系统
│   │   └── UnitEvent.cs               # Unit 事件发布/订阅
│   │
│   └── Attributes/                    # 标记特性
│       ├── DataEntryTypeAttribute.cs
│       ├── GenerateSkillDataSOAttribute.cs
│       ├── SkillDataFieldAttribute.cs
│       ├── SkillDataEntryAttribute.cs
│       ├── AutoGenerateMechanismAttribute.cs
│       ├── AutoGenerateConditionAttribute.cs
│       ├── GenerateMechanismsForAttribute.cs
│       └── MechanismMenuAttribute.cs
│
└── Editor/                            # 编辑器工具
    ├── Drawers/
    │   ├── ConditionDrawer.cs         # 条件多态选择抽屉
    │   ├── MechanismDrawer.cs         # 机制多态选择抽屉
    │   └── FormulaValueDrawer.cs      # 公式值编辑抽屉
    │
    ├── Windows/
    │   ├── SkillDataSOEditorWindow.cs # 技能编辑器主窗口
    │   ├── SkillDataSOEditor.cs       # 技能 Inspector 编辑器
    │   ├── CreateSkillScriptWindow.cs # 创建脚本窗口
    │   └── TriggerEventEnumEditor.cs  # 枚举编辑器
    │
    └── Generators/
        ├── MechanismCodeGeneratorV2.cs # 机制代码生成器
        ├── SkillDataSOGenerator.cs    # SkillDataSO 代码生成器
        ├── SkillSystemGeneratorMenu.cs # 生成器菜单
        └── TriggerEventEnumGenerator.cs # 枚举生成器
```

---

## 12. 常见问题

### Q1: 为什么生成的机制类没有出现在菜单中选择？

**A**: 确保：
1. 运行了 `Generate ALL Classes`
2. 机制类不是抽象的
3. 机制类有无参构造函数
4. 机制类不是泛型类型定义
5. 机制类标记了 `[Serializable]`

### Q2: 自定义公式中的字段引用找不到？

**A**: 确认：
1. 字段标记了 `[SkillDataField]` 特性
2. 数据类型是基础类型（float, int, Vector3 等）
3. 公式语法正确：`caster.FieldName` 或 `target.FieldName`

### Q3: 如何在运行时动态修改技能数据？

**A**:
```csharp
// 通过 ISkill.DataLayer 修改
skill.DataLayer.SetValue<float>("DamageMultiplier", 2.0f);

// 直接修改 SkillDataSO（会影响所有引用该资产的技能）
skillDataSO.SetValue<float>("DamageMultiplier", 2.0f);
```

### Q4: 如何处理技能的序列化/保存？

**A**: `SkillDataSO` 继承自 `ScriptableObject`，自动支持 Unity 的序列化。机制和条件通过 `[SerializeReference]` 支持多态序列化。

### Q5: 框架的性能开销如何？

**A**: 框架在设计上考虑了性能：
- 事件系统使用缓存的数组，避免 LINQ 和委托链 GC
- 条件对象池减少 GC 压力
- 公式求值使用手动实现的解析器，无外部依赖
- 建议将频繁执行的逻辑使用函数式机制而非对象式机制

---

## 13. 更新日志

### v1.0.0
- ✅ 六层分层架构
- ✅ 泛型/非泛型双轨系统
- ✅ 完整的编辑器工具链
- ✅ 自动化代码生成系统
- ✅ 公式求值器
- ✅ 条件对象池和缓存系统
- ✅ 事件系统优化

---

## 📝 许可证

MIT License

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---