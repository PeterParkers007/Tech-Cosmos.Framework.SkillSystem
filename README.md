# 🎮 TechCosmos 技能系统框架 (TechCosmos Skill System)

> **Unity 版本**: 2021.3+ | **语言**: C# 9.0+

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

> **v2.2.0+** 已内置完整 Buff 子系统（原 GBF），无需单独安装 `Tech-Cosmos.Framework.BuffSystem`。

---

## 1. 概述

TechCosmos 技能系统是一个专为 **Unity** 设计的、基于 **分层架构** 与 **领域驱动设计 (DDD)** 的 **模块化**、**数据驱动** 技能框架。它将技能的执行流程抽象为六个独立的层级，并**内置完整的 Buff/Debuff 子系统**（原 Generic Buff Framework），通过泛型和代码生成技术，在保持高度灵活性的同时提供了出色的编辑器体验。

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

### ✨ 九大核心特性

1. **🏗️ 六层分层架构**
   - 基础层 → 信息层 → 条件层 → 机制层 → 数据层 → 执行层
   - 每层职责清晰，可独立扩展

2. **🧩 泛型 + 非泛型双轨系统**
   - 泛型基类保证类型安全
   - 非泛型基类支持 Unity `[SerializeReference]` 多态序列化

3. **🛠️ 强大的编辑器工具**
   - 自定义 SkillDataSO 编辑器窗口（独立窗口 + Inspector 双模式）
   - 机制/条件多态选择抽屉 (PropertyDrawer)
   - TriggerEvent 枚举可视化编辑器
   - 公式编辑器（支持语法高亮、字段引用插入、公式检查）

4. **🤖 自动化代码生成**
   - 根据 `IUnit<T>` 实现自动生成 `SkillDataSO<T>` 子类
   - 自动生成封闭泛型机制类和条件类
   - 自动扫描并更新 `TriggerEventType` 枚举

5. **📊 数据层与公式系统**
   - 支持静态值、引用值、表达式、自定义公式四种类型
   - 自定义公式支持 `caster.Attack * 1.5 + target.MaxHealth * 0.1` 语法
   - 运行时公式求值器，支持括号、运算符优先级

6. **⚡ 性能优化机制**
   - `ConditionPool<T>` 条件对象池
   - `UnitEvent<T>` 缓存监听器数组，避免委托链 GC
   - `CachedCondition<T>` 条件结果缓存

7. **🔒 必要数据自动同步 (RequiredData)**
   - `[RequiredData]` 特性声明机制/条件所需的数据项
   - 编辑器自动创建/清理必要数据条目
   - 锁定必要数据防止误删（显示🔒图标）
   - 类型冲突检测与报错
   - 支持声明公式类型数据
   - 支持限制可切换的类型白名单 (AllowedTypes)
   - 显示数据项的依赖来源（来源机制/条件名称）

8. **🎨 创建技能脚本窗口**
   - 可视化选择目标 Unit 类型
   - 支持搜索过滤
   - 自动生成机制/条件模板代码

9. **✨ 内置 Buff 子系统（GBF）**
   - `BuffSystem<T>` 管理单位身上所有增益/减益
   - 支持 `BuffDataSO` 数据驱动配置与 `SimpleBuff<T>` 运行时快速施加
   - 效果执行器 + 执行模式（一次性 / 周期性）
   - 属性修改、Action 事件分发、标签查询与驱散
   - 与技能机制（`ApplyBuffMechanism` / `RemoveBuffMechanism`）和条件（`HasBuffCondition`）深度集成
   - 独立 Buff 编辑器与效果代码生成器

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
// 声明该机制所需的数据项（编辑器会自动创建/同步）
[RequiredData("DamageMultiplier", typeof(float), DefaultValue = "1.5", Description = "伤害倍率")]
// 也可声明公式类型的数据项
[RequiredData("DamageFormula", typeof(float), IsFormula = true,
    FormulaType = FormulaValue.FormulaType.Custom,
    CustomFormula = "caster.Attack * 1.5",
    Description = "伤害计算公式")]
// 限制只允许切换为 float 或 FormulaValue 类型
[RequiredData("CriticalChance", typeof(float), DefaultValue = "0.1",
    Description = "暴击率",
    AllowedTypes = new[] { typeof(float), typeof(FormulaValue) })]
// 标记自动生成封闭泛型类
[AutoGenerateMechanism(typeof(Character))]
// 可选：编辑器菜单分类
[MechanismMenu("⚔ 伤害", DisplayName = "伤害机制", Priority = 1)]
public class DamageMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float defensePenetration = 0f;

    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        // 从数据层获取动态值
        float casterAttack = dataLayer.GetValue<float>("Attack", context);
        float targetDefense = dataLayer.GetValue<float>("Defense", context);
        float damageMultiplier = dataLayer.GetValue<float>("DamageMultiplier", context);
        float criticalChance = dataLayer.GetValue<float>("CriticalChance", context);

        // 计算伤害
        float damage = (baseDamage + casterAttack * damageMultiplier)
                        - Mathf.Max(0, targetDefense - defensePenetration);

        // 暴击判定
        if (Random.value < criticalChance)
        {
            damage *= 2f;
            Debug.Log("[DamageMechanism] 暴击！");
        }

        damage = Mathf.Max(0, damage);
        Debug.Log($"[DamageMechanism] 造成 {damage} 点伤害");
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
   - 数据层：必要数据会自动出现（如 DamageMultiplier、DamageFormula、CriticalChance）

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

// 添加条件（使用 ConditionPool 对象池）
var healthCheck = new FuncCondition<Character>(ctx => ctx.caster.Health > 0);
var cooldownCheck = new CooldownCondition<Character>(3f);

// 使用运算符组合条件（自动从对象池租用）
skillData.AddCondition(healthCheck & cooldownCheck);

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
    Expression,  // 表达式: 类似 Reference
    Custom       // 自定义: 支持完整公式语法
}
```

自定义公式语法示例：

```
caster.Attack * 1.5 + target.MaxHealth * 0.1
(caster.Attack - target.Defense) * 2.0
caster.Level * 10 + 100
```

### 5.6 必要数据系统 [RequiredData]

通过 `[RequiredData]` 特性，机制或条件可以声明其依赖的数据项。编辑器会自动：

- 在数据层创建缺失的必要数据条目
- 删除不再需要的旧数据条目
- 锁定必要数据条目防止误删（显示🔒图标）
- 检测并报告类型冲突
- 显示数据项的依赖来源（来源机制/条件名称）
- 支持限制可切换的类型白名单

```csharp
// 基础声明
[RequiredData("DamageMultiplier", typeof(float), DefaultValue = "1.5", Description = "伤害倍率")]

// 公式类型声明
[RequiredData("DamageFormula", typeof(float), IsFormula = true,
    FormulaType = FormulaValue.FormulaType.Custom,
    CustomFormula = "caster.Attack * 1.5",
    Description = "伤害计算公式")]

// 限制类型白名单
[RequiredData("CriticalChance", typeof(float), DefaultValue = "0.1",
    Description = "暴击率",
    AllowedTypes = new[] { typeof(float), typeof(FormulaValue) })]
```

### 5.7 事件系统

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

### 5.8 Buff 系统

Buff 子系统已合并进 SkillSystem，推荐通过 `UnitBase<T>` 使用（已内置 `BuffSystem<T>` 与标签同步）。

#### 架构

```
BuffSystem<T> (管理层)
    ↓ 管理
IBuff<T> (接口层)
    ↓ 实现
BaseBuff<T> / ConfigurableBuff<T> / SimpleBuff<T>
    ↓ 包含
BuffEffectExecuter (执行层)
    ├── BuffEffect (效果)
    └── ExecutionMode (一次性 / 周期性)
```

#### 核心类型

| 类型 | 说明 |
|------|------|
| `BuffSystem<T>` | 管理单位身上所有 Buff，提供叠层、驱散、属性修正、Action 分发 |
| `BuffDataSO` | ScriptableObject 配置：时长、标签、叠层策略、修改器、效果执行器 |
| `ConfigurableBuff<T>` | 从 `BuffDataSO` 构建的运行时 Buff |
| `SimpleBuff<T>` | 技能机制使用的轻量运行时 Buff（按 id / 时长 / 修改器施加） |
| `IBuffHost<T>` | Buff 宿主接口，提供 `BuffSystem<T>` 与 `TagContainer` |
| `ApplyBuffMechanism<T>` | 技能机制：施加 Buff（支持 `BuffDataSO` 或运行时参数） |
| `RemoveBuffMechanism<T>` | 技能机制：按名称或标签移除 Buff |
| `HasBuffCondition<T>` | 条件：检查是否拥有指定 Buff 且层数达标 |

#### 快速使用

```csharp
// 推荐：继承 UnitBase，已内置 BuffSystem
public class Hero : UnitBase<Hero>
{
    protected override string[] GetSupportedEvents() => new[] { "OnAttack" };
}

// 代码施加 Buff（数据驱动）
var buffData = Resources.Load<BuffDataSO>("Buffs/Haste");
hero.BuffSystem.AddBuff(new ConfigurableBuff<Hero>(hero, buffData, caster));

// 属性修正
float attack = hero.EvaluateStat("Attack", baseAttack);

// 向 Buff 分发 Action（如受伤、治疗）
hero.BuffSystem.DispatchAction("OnDamaged", attacker, hero, damage, "Physical");

// 驱散带标签的 Buff
hero.BuffSystem.DispelByTags("Debuff", "Poison");
```

#### 叠层策略

| `ModifierStackPolicy`（技能机制） | 对应 `BuffStackPolicy` |
|----------------------------------|------------------------|
| `Stack` | `StackAndRefresh` |
| `RefreshDuration` | `ExtendDuration` |
| `Replace` | `Replace` |
| `Ignore` | 已存在则跳过施加 |

---

## 6. 编辑器工具

### 6.1 技能编辑器窗口 (Skill Editor Window)

 **打开方式**：`Tech-Cosmos → SkillSystem → Skill Editor Window`

 **主要功能**：

- ✅ 可视化编辑所有技能属性
- ✅ 条件和机制的多态选择
- ✅ 数据层可视化编辑（带类型着色）
- ✅ 公式编辑器（语法帮助 + 字段引用插入 + 公式检查）
- ✅ 必要数据自动同步与锁定
- ✅ 类型冲突检测与报错
- ✅ 类型切换白名单控制
- ✅ 数据依赖来源显示
- ✅ 自动保存和缓存优化

### 6.2 机制选择抽屉 (MechanismDrawer)

- 自动按分类分组显示
- 支持 "Switch"（切换）、"Copy"（复制 JSON）、"Remove"（删除）操作
- 类型安全筛选（只显示可实例化的非泛型子类）

### 6.3 条件选择抽屉 (ConditionDrawer)

- 自动按分类分组显示
- 支持 "Switch"（切换）、"Remove"（删除）操作
- 类型安全筛选

### 6.4 公式值选择抽屉 (FormulaValueDrawer)

- 可视化编辑四种公式类型
- 引用路径支持下拉菜单选择字段
- 操作符选择（乘/加/设）
- 一键升级为自定义公式

### 6.5 触发事件枚举编辑器

 **打开方式**：`Tech-Cosmos → SkillSystem → TriggerEvent Enum Editor`

- 可视化管理所有触发事件类型
- 支持添加/删除/重命名事件
- 自动生成 `TriggerEventType.cs` 文件

### 6.6 创建技能脚本窗口

 **打开方式**：`Tech-Cosmos → SkillSystem → Create Skill Script`

- 可视化选择目标 Unit 类型
- 支持搜索过滤
- 自动生成机制/条件模板代码
- 支持命名空间和类名自定义

### 6.7 Buff 编辑器窗口 (Buff Editor Window)

**打开方式**：`Tech-Cosmos → SkillSystem → Buff Editor Window`

- 可视化编辑 `BuffDataSO`：基础信息、标签、叠层策略
- 配置属性修改器（支持公式）
- 配置 Action 响应与效果执行器
- 支持从选中资产快速打开

### 6.8 Buff 效果与执行模式生成

| 菜单 | 功能 |
|------|------|
| `Tech-Cosmos → SkillSystem → Create Buff Script` | 创建自定义 Buff 效果 / 执行模式脚本 |
| `Tech-Cosmos → SkillSystem → Generate BuffEffect Classes` | 生成封闭泛型 BuffEffect 类 |
| `Tech-Cosmos → SkillSystem → Generate ExecutionMode Classes` | 生成封闭泛型 ExecutionMode 类 |
| `Tech-Cosmos → SkillSystem → Generate All BuffEffect Classes` | 一键生成全部 Buff 相关类 |
| `Tech-Cosmos → SkillSystem → Buff Enum Editor` | 管理 BuffModifyType / BuffActionType / BuffTag 枚举 |

### 6.9 Graph 节点图编辑器（Shader Graph 风格）

**打开方式**：
- `Tech-Cosmos → SkillSystem → Graph Editor`
- 选中 `SkillDataSO` / `BuffDataSO` 后：`Tech-Cosmos → SkillSystem → Open Graph Editor`

**技能图节点**：
| 节点 | 说明 |
|------|------|
| 触发器 | 触发事件、技能类型、名称 |
| 条件门 | 内嵌条件树编辑（AND/OR/NOT） |
| 机制 #N | 每个机制一块，可右键添加 |
| 时间轴 | Timeline 配置分支 |

**Buff 图节点**：
| 节点 | 说明 |
|------|------|
| Buff 根节点 | 名称、时长、叠层、标签 |
| 修改器 #N | 属性修改器列表项 |
| 执行器 #N | BuffEffectExecuter 列表项 |
| Action #N | 事件响应列表项 |

**操作**：
- 滚轮缩放、拖拽平移画布（与 Shader Graph 相同）
- 右键画布添加节点
- 节点位置自动保存到资产的 `graphLayout` 字段
- 节点内直接编辑序列化字段，与列表编辑器数据同步

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
| `[RequiredData]` | Class | 声明机制/条件所需的数据项，支持公式类型和类型白名单 |
| `[DataEntryType]` | Class/Struct | 标记可添加到数据层的自定义类型 |
| `[MechanismMenu]` | Class | 自定义编辑器菜单分类和显示名 |

### 7.3 RequiredDataAttribute 完整参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `Key` | string | 数据键名（对应 DataLayer.GetValue 的 key） |
| `ValueType` | Type | 数据类型 |
| `DefaultValue` | string | 默认值（字符串形式，float 默认 0，int 默认 0，string 默认 ""，bool 默认 false） |
| `Description` | string | 描述文字（显示在数值层条目标题和依赖来源中） |
| `IsFormula` | bool | 是否是公式类型（设为 true 则自动创建 FormulaValue） |
| `FormulaType` | FormulaType | 公式类型（IsFormula=true 时生效） |
| `StaticValue` | float | 公式的静态默认值（IsFormula=true 时生效） |
| `ReferencePath` | string | 公式的引用路径（IsFormula=true 且 FormulaType=Reference 时生效） |
| `CustomFormula` | string | 公式的自定义表达式（IsFormula=true 且 FormulaType=Custom 时生效） |
| `AllowedTypes` | Type[] | 允许切换到的类型白名单（为 null 或空数组表示允许所有类型） |

### 7.4 菜单功能一览

```
Tech-Cosmos/SkillSystem/
├── Generator/
│   ├── Generate ALL Classes          (生成所有代码)
│   ├── Generate Mechanism Classes     (仅生成机制)
│   ├── Generate Condition Classes    (仅生成条件)
│   ├── Generate SkillDataSO Only     (仅生成数据配置)
│   └── Clear All Generated           (清理生成文件)
├── Skill Editor Window               (技能编辑器)
├── Buff Editor Window                (Buff 编辑器)
├── Create Buff Script                (创建 Buff 脚本)
├── Generate All BuffEffect Classes   (生成 Buff 效果类)
├── Buff Enum Editor                  (Buff 枚举编辑器)
├── Graph Editor                      (技能/Buff 节点图编辑器)
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

### 8.7 ConditionPool<T>

```csharp
public static class ConditionPool<T> where T : class, IUnit<T>
{
    // 租用 And 条件
    public static Condition<T> RentAnd(Condition<T> a, Condition<T> b);
    public static Condition<T> RentAnd(Condition<T> a, Condition<T> b, Condition<T> c);

    // 租用 Or 条件
    public static Condition<T> RentOr(Condition<T> a, Condition<T> b);
    public static Condition<T> RentOr(Condition<T> a, Condition<T> b, Condition<T> c);

    // 租用 Not 条件
    public static Condition<T> RentNot(Condition<T> condition);

    // 归还到池中
    public static void Return(AndCondition<T> condition);
    public static void Return(OrCondition<T> condition);
    public static void Return(NotCondition<T> condition);
}
```

### 8.8 RequiredDataAttribute

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RequiredDataAttribute : Attribute
{
    public string Key { get; }                    // 数据键名
    public Type ValueType { get; }                // 数据类型
    public string DefaultValue { get; set; }       // 默认值（字符串形式）
    public string Description { get; set; }        // 描述文字
    public bool IsFormula { get; set; }            // 是否是公式类型
    public FormulaValue.FormulaType FormulaType { get; set; }  // 公式类型
    public float StaticValue { get; set; }         // 公式的静态默认值
    public string ReferencePath { get; set; }      // 公式的引用路径
    public string CustomFormula { get; set; }      // 公式的自定义表达式
    public Type[] AllowedTypes { get; set; }       // 允许切换到的类型白名单
}
```

### 8.9 BuffSystem<T> 与 IBuffHost<T>

```csharp
// Buff 宿主（UnitBase 已实现）
public interface IBuffHost<T> : IBuffHost where T : class
{
    BuffSystem<T> BuffSystem { get; }
}

// Buff 管理系统
public class BuffSystem<T> where T : class
{
    public event Action<IBuff<T>> OnBuffAdded;
    public event Action<IBuff<T>> OnBuffRemoved;

    public void BuffUpdate(float deltaTime);
    public void AddBuff(IBuff<T> buff);
    public void RemoveBuffsByName(string buffName);
    public void DispelByTags(params string[] tags);
    public IBuff<T> FindBuffByName(string buffName);
    public float GetModifiedValue(string modifyType, float baseValue, BuffModifyContext<T> context = null);
    public void DispatchAction(string actionName, T caster, T target, float value = default, string damageType = default);
}

// 数据驱动 Buff 配置
[CreateAssetMenu(menuName = "SkillSystem/Buff Data")]
public class BuffDataSO : ScriptableObject { ... }
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

// 使用完毕后归还
ConditionPool<Character>.Return(combined as AndCondition<Character>);
```

### 9.2 自定义条件缓存

当条件计算开销较大时，使用 `CachedCondition<T>` 包装：

```csharp
var heavyCondition = new FuncCondition<Character>(ctx =>
{
    return Vector3.Distance(ctx.caster.Position, ctx.target.Position) < 5f;
});

// 包装为缓存条件，相同上下文直接返回缓存结果
var cached = new CachedCondition<Character>(heavyCondition);
```

### 9.3 动态公式与数据引用

在 `SkillDataSO` 编辑器中配置自定义公式：

```
caster.Attack * 2.0 - target.Defense * 0.5
caster.MagicAttack * 0.3 + target.MaxHealth * 0.1
(caster.Attack + caster.Defense) * caster.Level * 0.1
```

### 9.4 使用 RequiredData 声明公式类型数据

```csharp
[RequiredData("HealAmount", typeof(float), IsFormula = true,
    FormulaType = FormulaValue.FormulaType.Custom,
    CustomFormula = "caster.MagicAttack * 0.5 + target.MaxHealth * 0.2",
    Description = "治疗量公式")]
[AutoGenerateMechanism(typeof(Character))]
[MechanismMenu("💚 治疗", DisplayName = "治疗机制")]
public class HealMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        // 公式会自动求值
        float healAmount = dataLayer.GetValue<float>("HealAmount", context);
        // context.Target.Heal(healAmount);
    }
}
```

### 9.5 使用 AllowedTypes 限制类型切换

```csharp
// 只允许 float 和 FormulaValue 类型
[RequiredData("DamageMultiplier", typeof(float), DefaultValue = "1.0",
    Description = "伤害倍率",
    AllowedTypes = new[] { typeof(float), typeof(FormulaValue) })]

// 只允许 int 和 float 类型
[RequiredData("StackCount", typeof(int), DefaultValue = "1",
    Description = "堆叠层数",
    AllowedTypes = new[] { typeof(int), typeof(float) })]
```

### 9.6 技能回调

机制可以实现 `SkillBack` 方法：

```csharp
public class ApplyBuffMechanism<T> : Mechanism<T> where T : class, IUnit<T>
{
    public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
    {
        // 通过 buffData 或 buffId 施加增益
    }

    public override void SkillBack(ISkill<T> skill)
    {
        // 技能回滚时移除增益，清理资源
    }
}
```

---

## 10. 最佳实践

### 10.1 项目组织建议

```
Assets/
├── TechCosmos.SkillSystem/     ← 框架代码（只读，含内置 Buff 子系统）
├── _Game/
│   ├── Scripts/
│   │   ├── Units/              ← IUnit / UnitBase 实现类
│   │   ├── Mechanisms/         ← 自定义机制
│   │   ├── Conditions/         ← 自定义条件
│   │   ├── BuffEffects/        ← 自定义 Buff 效果
│   │   └── DataEntryTypes/     ← 自定义数据类型
│   └── Resources/
│       ├── Skills/             ← SkillDataSO 资产
│       └── Buffs/              ← BuffDataSO 资产
└── Generated/                  ← 自动生成的代码（建议 .gitignore）
    └── SkillSystem/            ← Buff 枚举与效果生成输出
```

### 10.2 性能优化建议

1. **尽量使用函数式机制**：`Action<SkillContext<T>>` 比 `Mechanism<T>` 更轻量
2. **使用 `CachedCondition<T>`**：缓存昂贵计算
3. **使用对象池**：频繁创建/销毁的条件对象使用 `ConditionPool<T>`
4. **批量生成代码后清理**：生成的 `.g.cs` 文件可加入 `.gitignore`

### 10.3 设计建议

1. **每个机制只做一件事**：如 `DamageMechanism`、`HealMechanism`、`ApplyBuffMechanism`
2. **条件小而专注**：如 `HealthAboveCondition`、`HasBuffCondition`
3. **Buff 优先用 BuffDataSO**：复杂效果走配置；简单测试可用 `SimpleBuff` 或 `ApplyBuffMechanism` 参数
4. **数据公式与机制分离**：数值计算放在数据层公式中，逻辑放在机制中
5. **使用 `[SkillDataField]` 暴露字段**：让策划可以调整数值而不修改代码
6. **使用 `[RequiredData]` 声明依赖**：让编辑器自动管理必要数据项
7. **使用 `AllowedTypes` 限制类型切换**：防止策划切换为不兼容的数据类型

### 10.4 版本控制建议

```gitignore
# 自动生成的代码
Assets/Generated/

# SkillDataSO / BuffDataSO 资产可选择是否提交
# Assets/_Game/Resources/Skills/*.asset
# Assets/_Game/Resources/Buffs/*.asset
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
│   │   ├── ISkillLayer.cs             # 层接口基类
│   │   ├── IBaseLayer.cs              # 基础层接口
│   │   ├── IConditionLayer.cs         # 条件层接口
│   │   ├── IDataLayer.cs              # 数据层接口
│   │   ├── IExecuteLayer.cs           # 执行层接口
│   │   ├── IInformationLayer.cs       # 信息层接口
│   │   └── IMechanismLayer.cs         # 机制层接口
│   │
│   ├── Core/
│   │   ├── Skill.cs                   # 技能组合实现
│   │   ├── SkillContext.cs            # 技能上下文│   │   ├── SkillContextBase.cs        # 非泛型上下文
│   │   ├── SkillData.cs               # 技能纯数据类
│   │   ├── SkillDataSO.cs             # ScriptableObject 配置基类
│   │   ├── SkillDataSOExtensions.cs   # SkillDataSO 扩展方法
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
│   │   └── FormulaEvaluator.cs        # 公式求值器
│   │
│   ├── Events/                        # 事件系统
│   │   └── UnitEvent.cs               # Unit 事件发布/订阅
│   │
│   ├── Buff/                          # Buff 子系统（原 GBF）
│   │   ├── GBF/                       # 核心 Buff 引擎
│   │   │   ├── BuffSystem.cs          # Buff 管理器
│   │   │   ├── IBuff.cs               # Buff 接口
│   │   │   ├── BaseBuff.cs            # Buff 基类
│   │   │   ├── ConfigurableBuff.cs    # 数据驱动 Buff
│   │   │   ├── BuffDataSO.cs          # Buff 配置资产
│   │   │   ├── BuffEffect.cs          # Buff 效果
│   │   │   └── BuffEffectExecuter.cs  # 效果执行器
│   │   ├── SimpleBuff.cs              # 运行时轻量 Buff
│   │   ├── IBuffHost.cs               # Buff 宿主接口
│   │   └── TagContainer.cs            # 标签容器
│   │
│   └── Attributes/                    # 标记特性
│       ├── RequiredDataAttribute.cs    # 必要数据声明（支持公式类型和类型白名单）
│       ├── DataEntryTypeAttribute.cs   # 自定义数据类型标记
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
    ├── Generators/
    │   ├── MechanismCodeGeneratorV2.cs # 机制/条件代码生成器
    │   ├── SkillDataSOGenerator.cs    # SkillDataSO 代码生成器
    │   ├── SkillSystemGeneratorMenu.cs # 生成器菜单
    │   └── TriggerEventEnumGenerator.cs # 枚举生成器
    │
    └── Buff/                            # Buff 编辑器
        ├── BuffEditorWindow.cs        # Buff 配置编辑器
        ├── BuffEffectCodeGenerator.cs # Buff 效果代码生成
        ├── CreateBuffEffectWindow.cs  # 创建 Buff 脚本
        ├── BuffEnumEditorWindow.cs    # Buff 枚举编辑器
        └── BuffEffectExecuterDrawer.cs
    │
    └── Graph/                           # 节点图编辑器 (GraphView)
        ├── TechCosmosGraphEditorWindow.cs
        ├── Core/TechGraphCore.cs
        ├── Skill/SkillGraphView.cs
        ├── Buff/BuffGraphView.cs
        └── Styles/TechGraphStyles.uss
```

---

## 12. 常见问题

### Q1: 为什么生成的机制类没有出现在菜单中选择？

**A**: 确保：
1. 运行了 `Generate ALL Classes`
2. 机制类不是抽象的
3. 机制类有无参构造函数
4. 机制类不是泛型类型定义（是封闭泛型）
5. 机制类标记了 `[Serializable]`

### Q2: 自定义公式中的字段引用找不到？

**A**: 确认：
1. 字段标记了 `[SkillDataField]` 特性
2. 数据类型是基础类型（float, int, Vector3 等）
3. 公式语法正确：`caster.FieldName` 或 `target.FieldName`

### Q3: RequiredData 声明后编辑器没有自动创建数据项？

**A**: 确保：
1. 运行了 `Generate ALL Classes` 重新生成
2. 在技能编辑器中打开了对应的 SkillDataSO
3. 机制/条件已添加到技能中

### Q4: 如果两个不同的机制/条件对同一个 key 声明了不同类型的 RequiredData 会怎样？

**A**: 编辑器会自动检测类型冲突，并在 Console 中输出详细的错误信息。

### Q5: 如何限制数据项只能切换为特定类型？

**A**: 使用 `AllowedTypes` 参数：
```csharp
[RequiredData("MyValue", typeof(float), 
    AllowedTypes = new[] { typeof(float), typeof(FormulaValue) })]
```

### Q6: 框架的性能开销如何？

**A**: 框架在设计上考虑了性能：
- 事件系统使用缓存的数组，避免 LINQ 和委托链 GC
- 条件对象池减少 GC 压力
- 公式求值使用手动实现的解析器，无外部依赖

### Q7: 还需要单独安装 BuffSystem（GBF）包吗？

**A**: **不需要**。v2.2.0 起 Buff 子系统已内置在 SkillSystem 中：
- Runtime：`Runtime/Buff/GBF/`
- Editor：`Editor/Buff/`
- 若项目中仍有 `Tech-Cosmos.Framework.BuffSystem` 目录，请删除以避免重复编译

### Q8: 如何在技能中施加 Buff？

**A**: 两种方式：
1. **数据驱动**：创建 `BuffDataSO`，在 `ApplyBuffMechanism` 中引用 `buffData` 字段
2. **运行时参数**：配置 `buffId`、`duration`、`tags`、`modifiers`，由 `SimpleBuff<T>` 自动构建

也可在代码中直接调用 `unit.BuffSystem.AddBuff(...)`。

### Q9: Graph 节点图编辑器怎么用？

**A**：
1. 选中 `SkillDataSO` 或 `BuffDataSO` 资产
2. 菜单 `Tech-Cosmos → SkillSystem → Open Graph Editor`
3. 在画布上右键可添加机制 / 修改器 / 执行器等节点
4. 节点内编辑与列表编辑器数据实时同步，布局保存在 `graphLayout` 字段