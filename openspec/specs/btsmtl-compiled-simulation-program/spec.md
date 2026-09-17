# btsmtl-compiled-simulation-program Specification

## Purpose

旧 `CharacterSimulationProgram`、`ProgramCatalog`、整角色 `Projection` 和统一 Character Build 已退出当前架构。本文件保留名称用于迁移索引，但当前合同只规定图产物和领域运行数据的边界；它不再定义一个可加载的整角色 Program。

## Requirements

### Requirement: 当前编译只发布图产物

正式编译 MUST 只发布明确 Graph owner 的 canonical graph artifact。编译输入是图根、图节点、连接、端口、常量和图依赖；Character Definition、Ability、Control、Timeline、Pose、Camera、Motion、Effect 与 Equipment 不得作为整包输入被合并。

#### Scenario: 发布图产物

- **WHEN** 作者显式请求 Gameplay Graph 编译
- **THEN** 系统 MUST 发布带 graph identity、source revision、schema、hash 和 source map 的图产物
- **AND** MUST 不发布 `CharacterSimulationProgram`、`ProgramCatalog` 或整角色 `Projection`

### Requirement: Ability 不是独立编译产物

Ability MUST 通过正式入口引用图、Timeline 内容和运行所需的领域 binding。Ability 的 grant、参数、调用 identity、ActionInstance 和生命周期属于运行时合同；系统 MUST 不为 Ability 再建立独立 Compiler、Ability Program、Target Program 或角色级 Program wrapper。

#### Scenario: 运行时加载 Ability

- **WHEN** Session 或 ScenePlay 选择一个 Ability
- **THEN** 系统 MUST 解析其正式图引用、内容引用和 provider binding
- **AND** MUST 直接建立运行实例
- **AND** MUST 不读取 `.csim`、旧 Semantic IR cache 或旧 Program Reader

### Requirement: Timeline 与 Pose 不进入整角色编译

Timeline MUST 直接准备和调度正式 TimelineData、内容引用、资源 binding 与播放私有状态。Pose MUST 使用原生 FlowCanvas Graph 实例、Source、Constraint 和 Final Publication。两者 MUST 不生成 Character Program operation、Projection 内 Pose Image、Timeline IR 或 Preview 专用执行器。

#### Scenario: Ability 播放 Timeline

- **WHEN** Ability 提交 Timeline playback request
- **THEN** Timeline Runtime MUST 根据正式内容 identity、调用 identity 和参数创建播放实例
- **AND** 角色核心只负责 Step 的接受/丢弃、World/State 提交和完整快照组合
- **AND** Timeline 不得把自身状态复制进 Ability 或 Character 总状态容器

### Requirement: 角色运行由领域装配

`SimulationSessionHost` MUST 显式装配 Control、Ability、Timeline、Effect、Equipment、Presentation、Pipeline、Session Source 和 WorldSolver。`CharacterPipelineHost` MUST 只负责 Actor registration、binding、Presentation 和 diagnostics 接口，不得加载或生成整角色 Program/Projection。

#### Scenario: 创建正式 Session

- **WHEN** Session 准备一个 Actor roster
- **THEN** 系统 MUST 分别校验各领域 binding、版本、能力和状态 schema
- **AND** MUST 在同一 Session/Step 中组合这些领域
- **AND** MUST 不通过整角色 ProgramHash、LayoutHash 或 ProgramEpoch 作为总入口

### Requirement: 旧 Program 链路必须删除

当前实现和当前规范 MUST 不新增 `CharacterSimulationProgram`、`ProgramCatalog`、`ProgramEpoch`、旧 Program Reader、整角色 `.csim` 发布或 fallback。已存在的旧字段、路径、编译器、转换器、缓存和 wrapper 应按迁移任务删除；历史实施记录只保留事实，不得继续作为运行入口。

#### Scenario: 旧消费者残留

- **WHEN** 新实现仍引用旧整角色 Program/Projection
- **THEN** 该引用 MUST 被视为待清理的迁移残留
- **AND** 修复 MUST 直接接入对应领域 owner，不得新增兼容壳或双路径

### Requirement: 技能产物与角色领域状态必须独立发布和绑定

技能 MUST独立发布；Control、BodyMotion、Input、Effect、Equipment 配置与状态 MUST由各自模块拥有，角色绑定在 Session Active 前检查完整引用和能力。技能局部状态 MUST包含图流程、Blackboard、调用帧与计时；Timeline播放状态由正式Timeline Runtime独立拥有并参与同一角色恢复；角色快照 MUST组合所有领域的同次提交状态。纯 Pose 或资源问题 MUST不阻止合法技能构建，但非法角色资源绑定 MUST阻止对应角色运行。

#### Scenario: Pose配置缺失但技能合法
- **WHEN** 作者构建一个依赖完整的技能，而某角色的动画配置无效
- **THEN** 技能构建 MUST可以独立完成，角色实例准备仍 MUST报告动画绑定错误

#### Scenario: 多角色使用不同装备配置
- **WHEN** 相同技能分别绑定两个合法角色装备目录
- **THEN** 技能数据 MUST复用，装备状态和整体玩法一致性身份 MUST分别归各角色正式模块

### Requirement: Timeline内容必须作为独立直接数据交付

Timeline发布 MUST只保存正式轨道／片段字段、时间区间、稳定身份和资源／TreeClip图引用；内容校验、portable导出和目标数值准备 MUST不生成时间轴IR或操作表。技能发布只记录实际内容依赖，独立Timeline调用使用同一数据与运行入口。旧独立Timeline IR／Program构建入口 MUST迁移为内容发布，不能因此删除独立调用能力。

#### Scenario: 普通DotNet加载独立Timeline
- **WHEN** 调用方提供合法portable时间轴内容、资源和上下文
- **THEN** 正式Timeline Runtime MUST直接调度该内容，不加载Unity对象或生成临时Program
