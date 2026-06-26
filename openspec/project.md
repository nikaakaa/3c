# Project Context

## Purpose

本项目是求职向 Gameplay 客户端程序 demo。目标不是完整 PvPvE 产品、MMO、纯网络框架或通用编辑器产品，而是展示第三人称动作客户端能力：输入响应、角色控制、相机、动作状态、动画表现、战斗窗口、受击反馈、调试可视化，以及在最小服务端权威压力下保持手感。

当前真实重心是先把 Taco authoring 底座打干净，再用它承载 StateMachine、Timeline、Tree、Action 等玩法创作数据。Gameplay runtime 和网络演示要建立在这条干净数据链路上，不从旧 SO/config 分裂路径恢复。

## Current State

- 当前 active changes：无。
- `openspec/specs/` 当前包含已归档 current spec：
  - `taco-componentized-node-authoring`
  - `taco-graph-core`
  - `taco-input-action-node-authoring`
  - `taco-runnable-timeline-node`
  - `taco-sm-node-authoring`
- 客户端主目录是 `3cDemo/Client/3C_Client`。
- 当前脚本主模块是 `Camera`、`Rendering`、`Taco`；`Charactor` 只剩很薄的目录骨架，角色 gameplay runtime 还没有重新搭完整。
- 服务端 `3cDemo/Server` 只保留 Fantasy 骨架，不再保留旧 FrameSyncAuthority 业务。
- `Ref` 是参考代码来源，不是运行时依赖。

## Tech Stack

- Unity 2022.3.62f2c1。
- C#、UI Toolkit、GraphView。
- URP 14、Cinemachine 2.10、Unity Input System、Unity Timeline。
- Taco / TreeDesigner / Timeline 本地代码。
- Fantasy.Unity / Fantasy.Net 骨架。
- OpenSpec 用于能力规划和归档。

## Architecture

### Gameplay Client Direction

- 对外作品口径是 `Network-aware Third Person Action Combat Prototype`。
- 第一目标是 Gameplay 客户端纵切，不是完整网络产品。
- 客户端主链路：`Input -> Action Request -> State/Graph Decision -> Timeline/Animation Presentation -> GameplayWindow Facts -> Prediction Presentation -> Server Result -> Correction Smoothing`。
- Timeline 只产出动作事实和表现轨道，例如 AttackWindow、IFrameWindow、ParryWindow、ArmorWindow、CancelWindow、CostEvent、SpawnHitboxEvent、VFX/SFX/Camera Cue。
- Timeline 不直接宣称命中成立；命中、伤害、目标归属必须由服务端或权威 gameplay solver 裁决。

### Taco Authoring Direction

- Taco 是当前 authoring 基座，不是必须照搬的 runtime。
- `BaseGraph` 是图数据和结构编辑底座，`BaseTree` 继续作为当前可打开的 Unity asset / editor 入口。
- `BaseNode` 是节点 authoring entity，可以承载 `NodeModule`。
- 字段扫描走 `NodeFieldAccessor`，同时支持节点字段和模块字段。
- Port 系统继续使用 Taco 原生 `PropertyPort` / `PropertyEdge`，连接身份使用稳定 `PortId`。
- 不新增 `WorkbenchPortDescriptor`、并行注册表或并行 WorkbenchTree。
- `StateMachineGraph : BaseTree`，`StateMachineNode` 表达父级行为图进入状态机图的入口，`StateNode` 表达状态机图内普通状态和状态行为边界。
- `Enter`、`AnyState`、`Exit` 是 StateMachineGraph 层级控制节点，不是普通状态模块。
- `StateNode` 可引用普通 `SubTree` 或 `StateBehaviorSubTree`；普通 `SubTree` 只执行 `RootNode`，`StateBehaviorSubTree` 使用 `OnEnter`、`RootNode`、`OnExit` 表达状态生命周期。
- Transition 是 edge 语义，不新增 `TransitionNode`。
- `TimelineNode : RunnableNode`，用于 Graph 驱动 Timeline；Timeline asset 仍是数据资产。
- Taco 原有 `TreeTrack / TreeClip / TimelineRunningTree` 可以保留为 Timeline 驱动 Tree 的链路，但不替代 Graph 驱动 TimelineNode。

### Network Boundary

- 求职目标是 Gameplay 客户端程序，不是 Network Engineer。
- 网络只做最小压力场景：两个玩家争夺一个怪物或目标点，玩家可互相打断，服务端裁决结果。
- 本地玩家可以预测移动、转向、闪避、攻击启动、动画、特效和镜头表现。
- 远端玩家使用服务器快照和插值，不复制完整本地预测。
- 服务端裁决位置真值、动作真值、窗口真值、命中、伤害、目标归属、怪物状态和局内事件。
- PvP 命中使用服务端权威加局部 combat rewind，只回溯 pose、hurtbox、action window，不回滚整个世界。
- 不做全局帧同步、不做完整 rollback、不做客户端权威。

## Code Organization

- `Assets/Scripts/Taco/Scripts`：Taco 基础工具、反射、通用属性。
- `Assets/Scripts/Taco/TreeDesigner/Scripts`：Graph、Tree、Node、Edge、PropertyPort、ExposedProperty。
- `Assets/Scripts/Taco/TreeDesigner/Editor`：节点图窗口、节点视图、端口视图、搜索和 inspector。
- `Assets/Scripts/Taco/Timeline/Scripts`：Timeline asset、Track、Clip、Playable、TimelineNode。
- `Assets/Scripts/Camera`：第三人称相机模型、solver、runtime adapter。
- `Assets/Scripts/Rendering`：动作表现相关后处理和 VFX runtime。
- `3cDemo/Server`：Fantasy skeleton，只作为后续最小权威服务端基础。

## Conventions

- 生成代码尽量少写注释，只有关键复杂边界写少量注释。
- 不做 fallback 配置、兼容镜像、临时桥接路径或双主线。
- 旧数据、旧路径、旧命名确认不用就直接删除。
- 修改代码不用 MCP 写文件；Unity MCP 只用于查看状态、console 或编辑器操作。
- 永远不要运行 Unity batchmode。
- 文档读取必须显式 UTF-8。
- 默认不新增测试，除非用户明确要求。
- 用户负责 Unity 端到端验证；不要把手动验证写进 OpenSpec task。

## Cleanup Rules

- 旧 Workbench 路径不恢复。
- 旧 locomotion 特化 SO/config 不恢复。
- 旧 action SO、footphase profile、bodyclaim policy、AnimationPresentationPolicy 等如果脱离节点/模块/Timeline 继续作为当前数据源，应迁移或删除。
- `Ref` 中代码只能复制进正式模块后改名归属，不能作为运行时依赖。
- archive 只查历史，不作为当前实现目标。

## Open Questions

- 动态 `List<PropertyPort>` 的通用编辑器 UI 还需要继续收口。
- Gameplay runtime 的角色动作链路还没有从 authoring 数据正式落地。
- 最小网络压力场景还没有开始实现。
