# Change: 收口自研分层角色状态机运行时

## Why
当前角色主线实际使用项目自研的 `CharacterStateMachineRunner`，但文档仍把 UnityHFSM 写成后续优先方案，且现有 runner 同时承担 transition 解释、timeline 采样、动作位移输出、动画请求、TurnBack policy 和若干 runtime latch 输出，职责边界不够清晰。

需要明确：角色状态机是一棵统一的分层状态机；“统一”和“分层”不冲突。后续继续自研，但必须把底层做成更通用的状态图运行时，并把 motion、animation、timeline、input gate 和 diagnostics 从 runner 职责中收束到明确模块。

## What Changes
- **BREAKING**：角色 FullBody base layer 的正式状态机主线继续使用项目自研分层状态图运行时，不再以 UnityHFSM 作为后续角色业务状态机优先方案。
- 明确 UnityHFSM 可以作为参考或未来另行审批的 adapter 方向，但当前不得作为未审批替换路径接入正式角色状态机。
- 将 `CharacterStateMachineRunner` 的目标职责收窄为：解释状态图、选择 transition、维护 active state / state time / variant / pending transition、提供纯数据 snapshot/restore。
- 在自研运行时内部引入经典 `Enter / Tick / Exit` 生命周期接口，但接口只产出纯数据 frame 输出，不直接执行 Unity 副作用。
- 保持正式对外推进入口为 `Tick(context) -> CharacterStateMachineFrame`，不得把 Enter/Exit/Tick 暴露成三条外部执行管线。
- 将 timeline window 采样、状态输出解析、动作位移命令、动画请求、TurnBack motion policy 和 run latch 写入规划为 runner 外围模块或明确子职责。
- 收敛状态机动画字段：状态节点只保留动画语义 key、timeline binding key 或等价稳定 ID；具体 `Clip`、`TransitionAsset`、`TransitionLibraryKey`、fade、speed、start time 归属 Animancer / 动画配置入口。
- 统一术语：`FullBody/Locomotion/...` 和 `FullBody/Action/...` 是同一棵分层状态机的路径，不得再描述为并列状态机或缝合后的双权威。
- 更新项目文档和 agent 指南，使后续实现不会继续按 UnityHFSM 优先、BBB 旧聚合点或 Locomotion 局部图口径推进。
- 保持当前 FullBody pipeline、单 runner owner、motion executor、Animancer presenter、rollback snapshot 主线，不新增第二状态机、第二控制器或 fallback 配置。

## Impact
- Affected specs:
  - `unified-character-state-machine`
  - `fullbody-hfsm-state-tree`
  - `project-structure`
- Affected docs:
  - `AGENT.md`
  - `docs/agents/unityhfsm-usage-guide.md`
  - `docs/agents/action-fighting-prediction-rollback-guide.md`
  - `openspec/project.md`
- Affected code after approval:
  - `Assets/Scripts/Character/StateMachine/Model`
  - `Assets/Scripts/Character/StateMachine/Solver`
  - `Assets/Scripts/Character/Action/FullBody`
  - `Assets/Scripts/Character/Movement`
  - `Assets/Tests/Editor`
- Related active changes:
  - `refactor-state-machine-runtime-authority` 已约束唯一 runner owner，本变更不重复该目标，只收窄 runner 内部职责。
  - `refactor-character-runtime-adapter-layers` 已约束 Runtime/Model/Solver/Diagnostics 分层，本变更把状态机 runtime 纳入同一分层口径。
  - `refactor-fullbody-frame-pipeline` 和 `refactor-locomotion-frame-pipeline-mainline` 已定义 pipeline 顺序，本变更不得改变正式推进顺序。
