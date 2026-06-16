# Change: 将角色状态节点重构为能力模块模型

## Why
当前统一状态机已经收敛到一棵状态树和一个 runner owner，但节点数据仍是“万能节点”：每个节点都带 `output`、`animation`、`variants` 等字段，运行时又大量依赖 `Locomotion / Action` owner 分支判断。这样会让普通 Locomotion 节点暴露无效动画配置，也会让 Dodge、TurnBack、Attack 等能力被迫塞进互斥大类，后续每次重构都容易出现动画、位移、输入消费路径漂移。

需要把状态图关系和状态能力分开：节点关系保持同一种 `Node`，不同能力通过模块组合表达，运行时系统按模块产出 motion、animation、input、timeline、facts 等输出通道。

## What Changes
- **BREAKING**：`CharacterStateNodeDefinition` 不再长期作为包含所有能力字段的万能节点；节点核心只表达状态图关系、稳定 ID、父子路径、标签和模块列表。
- 将 `Locomotion / Action` 从互斥 owner 语义降级为由模块和输出通道派生出的事实，不再作为运行时分支权威。
- 引入状态能力模块模型：Locomotion phase、动作请求、动作位移、动画请求、timeline window、输入消费、run latch、TurnBack motion policy 等都作为模块或等价模块数据挂在节点上。
- 输出从 `Owner.IsAction` / `IsLocomotion` 分支改为按模块收集：motion outputs、animation outputs、input outputs、timeline facts、runtime facts。
- `gait` 继续保持运行时事实，不进入状态节点配置；`phase + gait` 仍由基础移动动画配置解析具体 locomotion 动画。
- 保持一个正式状态图、一个正式 runner owner、一个 FullBody pipeline，不新增第二状态机、第二控制器、第二播放路径或 fallback 配置。
- 规划现有资产迁移：默认状态机从万能字段迁移到模块列表，旧字段只允许作为一次性迁移来源，迁移后不得并行维护。

## Impact
- Affected specs:
  - `unified-character-state-machine`
  - `fullbody-hfsm-state-tree`
  - `character-runtime-blackboard`
  - `basic-locomotion-animation`
  - `action-animation-profile`
- Affected code after approval:
  - `Assets/Scripts/Character/StateMachine/Model`
  - `Assets/Scripts/Character/StateMachine/Config`
  - `Assets/Scripts/Character/StateMachine/Solver`
  - `Assets/Scripts/Character/Action/FullBody`
  - `Assets/Scripts/Character/Movement`
  - `Assets/Tests/Editor`
- Related active changes:
  - `refactor-character-hierarchical-state-runtime` 已完成 runner 职责收口，本变更在其基础上继续重构节点数据模型。
  - `refactor-locomotion-frame-pipeline-mainline`、`refactor-locomotion-adapter-modules` 与本变更有输出通道交集；实现阶段必须避免新增并行 Locomotion 主线。
  - `formalize-animation-playback-rollback-authority` 与本变更共享 animation fact / request 边界；本变更不改变 Animancer 作为表现 adapter 的职责。
