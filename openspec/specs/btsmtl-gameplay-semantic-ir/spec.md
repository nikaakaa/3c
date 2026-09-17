# btsmtl-gameplay-semantic-ir Specification

## Purpose

定义 BTSMTL Gameplay Graph 从作者数据到稳定图产物的编译边界。该规范只描述图自身的发现、校验、来源映射和数值降低，不再把 Character、Ability、Timeline、Pose、Control、Effect 或 Equipment 编成整角色 Program。

## Requirements

### Requirement: 只有图拥有编译入口

正式编译入口 MUST 接收一个明确的 Gameplay Graph 根及其可达子图、StateMachine、ConditionRuleGraph、Value Port 和 Graph-owned Blackboard 声明。CharacterPipelineDefinition 与 Ability 只提供装配、入口和引用，不得作为整角色编译根。TimelineData、AnimationClip、MotionCurve、Pose Graph、Control Module、GameplayEffect Profile 与 Equipment Profile 不得因为被引用就被展开进图产物。

#### Scenario: 编译一个 Ability 使用的图

- **WHEN** 作者从 Ability 入口请求其 Gameplay Graph 编译
- **THEN** 编译器只发现该图及其声明的图依赖
- **AND** 产物只包含图节点、边、常量、状态声明和来源映射
- **AND** Ability 本身不生成独立 Ability Program 或整角色 Program

#### Scenario: 图依赖越界

- **WHEN** 图编译发现 Character、Timeline、Pose、Control、Effect 或 Equipment 的运行对象或作者资产
- **THEN** 编译器 MUST 只记录稳定引用或报告越界
- **AND** MUST 不把该对象复制为图内 operation、state slot 或 producer

### Requirement: 图闭包必须稳定且可追溯

Graph artifact MUST 保存稳定 graph identity、source revision、canonical dependency order、节点/边来源、端口 identity、常量来源和能力声明。Graph discovery MUST 拒绝重复 identity、循环依赖、缺失 owner、缺失节点定义、端口冲突和未声明的跨域依赖。

#### Scenario: 图顺序变化

- **WHEN** 作者只调整节点布局或不改变业务 identity 的连接顺序
- **THEN** 编译器 MUST 按稳定 identity 生成 canonical graph bytes
- **AND** Source Map MUST 继续指向原节点和端口

#### Scenario: 图依赖缺失

- **WHEN** 图引用的子图、节点定义或端口声明不存在
- **THEN** 编译 MUST 失败并报告精确 source identity
- **AND** MUST 不读取旧缓存、默认节点或其它同名对象

### Requirement: 图产物不得变成角色运行时

Graph artifact MAY 被正式 Graph Runtime 读取，但 MUST 不成为 Character、Session、World 或 Presentation 的总运行入口。Runtime MUST 不从 authoring object 临时生成图副本，不从 stale artifact 回退作者对象，也 MUST 不通过图产物保存 Control、Timeline、Pose、World 或 Network Model 状态。

#### Scenario: 图产物过期

- **WHEN** Graph artifact 的 source revision、schema 或依赖 identity 与当前图不匹配
- **THEN** 对应 Graph Runtime 请求 MUST 明确失败
- **AND** Character Session MUST 不因此自动编译、猜测或替换其它领域数据

### Requirement: 数值降低只服务图的数值语义

图中的数值字面量 MUST 保留 canonical source literal 和类型来源。Float32 与 FixedQ32.32 MAY 按同一图语义分别降低数值节点，但结果 MUST 仍是图运行所需的数据，不得生成 Character Program、Character State ABI 或 Network Model 专属业务规则。Target 不支持图节点或数值类型时 MUST 在准备阶段明确失败。

#### Scenario: 同一图使用两个数值目标

- **WHEN** Float32 与 FixedQ32.32 为同一 Graph artifact 准备运行数据
- **THEN** 两者 MUST 保持相同节点、边和业务语义
- **AND** 只允许在数值表示、精度和 codec 层存在目标差异

### Requirement: Ability、Timeline 与 Pose 必须保持领域边界

Ability MUST 作为运行入口、引用集合和实例生命周期存在，不得再定义独立 Ability 编译链。Timeline MUST 直接消费正式 TimelineData、内容引用和播放私有状态，不生成 Timeline IR、Timeline operation 表或 Character Program operation。Pose MUST 使用正式 FlowCanvas 图实例和表现宿主，不生成 Pose IR、Pose ProgramImage 或第二套 Preview Runtime。

#### Scenario: Ability 调用 Timeline

- **WHEN** Ability 请求播放一个 Timeline
- **THEN** Ability 只提交调用 identity、参数和生命周期请求
- **AND** Timeline Runtime MUST 直接准备和调度正式 TimelineData
- **AND** 两者 MUST 不互相复制图、Timeline 内容或运行状态

#### Scenario: ScenePlay 观察 Ability

- **WHEN** ScenePlay 选择一个 Ability 并开始正式 Session
- **THEN** ScenePlay MUST 通过正式输入和请求入口启动 Ability
- **AND** ScenePlay MUST 不创建 Ability Preview Player、Timeline Preview Session 或 Pose Fixture
- **AND** Timeline UI 只能读取正式 binding、playback 和 completion 事实

### Requirement: 旧整角色编译载体不得恢复

当前主线 MUST 不新增或恢复 `CharacterSimulationProgram`、`SimulationProgramCatalog`、整角色 `Program/Projection` Build、`ProgramEpoch` adoption、`.csim` 整角色 artifact、旧 Program Reader 或兼容 fallback。历史文档 MAY 保留旧名称作为迁移证据，但不得作为当前实现入口、规范要求或运行时依赖。

#### Scenario: 旧入口仍被调用

- **WHEN** 新代码或新文档尝试通过整角色 Program/Projection 读取、构建或启动 Runtime
- **THEN** 该路径 MUST 被视为迁移残留并删除或改接正式领域入口
- **AND** MUST 不增加转发壳、兼容别名或空成功实现

### Requirement: 技能领域引用必须独立于角色目录

技能 IR MUST只保存实际使用的输入、状态、效果、装备或动作接口要求，不内联角色完整配置。绑定 MUST由已安装领域模块解析；未装备但已声明可用的装备能力 MUST仍可按正式授予和模块目录绑定，不能通过运行时目录扫描猜测技能。

#### Scenario: 技能请求装备变化
- **WHEN** 技能包含正式装备请求节点
- **THEN** 技能数据 MUST保存请求语义和引用要求，装备模块唯一拥有目录和状态
- **AND** 装备目录改变 MUST更新对应玩法身份而非复制全部技能数据
