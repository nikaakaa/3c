## Context
当前工作区里有两套层级叠在一起：

- 基础移动：`PlayerLocomotionController -> BasicLocomotionPipeline -> BasicLocomotionStateMachine -> LocomotionStateGraphConfigSO -> BasicLocomotionAnimancerPresenter`
- FullBody Action：`PlayerFullBodyActionController -> DodgeFullBodyActionModule -> DodgeActionRuntime -> ActionInterruptArbiter -> ActionAnimationAnimancerPresenter`
- 外层 HFSM：`FullBodyHfsmTreeDefinitionSO -> FullBodyHfsmStateTreeBuilder/Driver` 只把 Locomotion 和 Action 包成统一路径，并没有成为真实 transition 权威。

这导致 transition 权威分散：

- `Idle / MoveStart / MoveLoop / MoveStop` 的 transition 在 Locomotion 图。
- `Action.Dodge` 是否可进入在 Action interrupt policy。
- `Locomotion <-> Action` 在 FullBody HFSM 的 `ActionActive` 条件。
- `Action.Dodge.Directional` / `Backstep` 动画转换在独立 Action Animation Profile。

这些模块单独看都有抽象，但整体是浅模块堆叠：理解一次 Shift Dash 需要跨输入缓冲、Locomotion facts、Action runtime、Action policy、FullBody HFSM、Action animation profile 和 motion executor 多处跳转。删除其中任意一层不会让复杂度消失，只会把复杂度散回调用者，说明当前 seam 没有提供足够 leverage 和 locality。

## Goals / Non-Goals
- Goals:
  - 建立一个统一、可配置、层级化的角色逻辑状态机作为 FullBody base layer 行为唯一权威。
  - 让 Locomotion 四阶段、Dodge、后续 Roll/Jump/Attack 等都使用同一种状态节点、transition、条件和输出模型。
  - 让动画转换配置跟随逻辑状态可见，并在逻辑状态确定后由动画外观层消费。
  - 删除或退役 Locomotion/Dodge/FullBody 缝合路径，不保留第二状态权威。
  - 保持逻辑状态机不直接依赖 Animancer、CharacterController、InputAction、Cinemachine 或场景对象。
  - 保留现有可用行为：普通 Walk/Run、MoveStart/MoveLoop/MoveStop、Directional Dodge、Backstep Dodge、Directional 完成后 Run latch、Idle 后重置。
- Non-Goals:
  - 不新增 Roll、Jump、Attack、Hit、Death 的具体动作内容。
  - 不实现 UpperBody、Facial、IK、Additive 或并行表现状态层。
  - 不实现图形化状态机编辑器。
  - 不改网络协议、预测、回滚或 Fantasy DTO。
  - 不删除诊断日志，除非用户后续明确要求。

## Decisions
- Decision: 使用一个 `CharacterStateMachineDefinitionSO` 或等价资产作为逻辑状态机根入口。
  - Reason: 设计者必须先看到一棵完整逻辑状态树，再配置状态 transition、运动和动画表现。多个散资产手工拼装会继续制造分裂路径。
  - Replaces: `FullBodyHfsmTreeDefinitionSO` + `LocomotionStateGraphConfigSO` + `FullBodyActionSetSO` + `FullBodyActionAnimationSetSO` 这种分散入口。

- Decision: 状态节点使用通用模型，不再按 Locomotion 或 Dodge 定制状态机类。
  - Reason: `MoveStart`、`MoveStop`、`Dodge` 都是逻辑状态；差异应由状态输出和 transition 条件配置表达，而不是由不同 runtime 类表达。
  - Implementation note: `BasicMovementPhase`、`ActionStateId` 可以迁移为稳定状态 ID 或标签，但不再作为不同子框架的分界。

- Decision: transition 是状态机的一等配置。
  - Reason: 设计者需要在同一个逻辑图里看到 `MoveLoop -> MoveStop`、`MoveStop -> MoveStart`、`Any/Locomotion -> Dodge`、`Dodge -> MoveLoop/Idle` 这类切换。
  - Implementation note: 现有 `ActionInterruptArbiter` 的优先级、抗性、时间窗口可以迁移成 transition 条件 evaluator 或 transition policy 块，但不得继续作为状态图外部的第二目标选择器。

- Decision: 状态输出和 adapter 分离。
  - Reason: 逻辑状态机负责决定状态、输出纯数据命令和事实；具体移动、动画播放、输入读取、相机应用由 adapter 执行。
  - Allowed adapters: 输入缓冲 adapter、运动执行 adapter、Animancer 播放 adapter、相机 look/resolve adapter。
  - Not allowed: adapter 反向决定逻辑状态，或直接绕过状态机消费 Dodge/Action 请求。

- Decision: 动画转换配置接在逻辑状态之后。
  - Reason: 工业实践里状态机先决定逻辑状态，再由动画层根据状态和变体播放 transition；动画配置不是另一个动作系统。
  - Implementation note: 每个状态或状态变体可绑定 Animancer `TransitionAssetBase`、TransitionLibrary key、fade、speed、start time 和调试名。动画外观层消费这些配置，不参与是否进入状态的判断。

- Decision: 实现阶段允许大删。
  - Reason: 现有缝合路线已经形成错误抽象，继续兼容会把旧 seam 固化。
  - Deletion candidates: `DodgeActionRuntime`、`DodgeFullBodyActionModule`、`FullBodyHfsmStateTreeBuilder/Driver`、`LocomotionStateGraphConfigSO`、`BasicLocomotionStateMachine`、`FullBodyActionSetSO`、`FullBodyActionAnimationSetSO`、独立 Action Animation Profile 入口，以及依赖这些入口的测试和资产。
  - Keep only if: 类型被降级为纯数据、adapter 或临时迁移脚本，且不再拥有状态切换权威。

## Proposed Runtime Order
1. 输入 adapter 写入 Move/Look 快照和本地预输入请求。
2. 统一状态机读取纯数据 context：移动意图、当前状态时间、预输入、动画可退出事实、运动 facts、当前状态标签。
3. 统一状态机按 transition 优先级和条件切换到下一逻辑状态。
4. 当前逻辑状态产出纯数据输出：运动命令、动画转换请求、状态事实、请求消费意图、Run latch 等。
5. 组装层按输出调用运动执行 adapter、Animancer adapter、输入缓冲消费和相机 adapter。
6. adapter 反馈只读事实，例如动画 normalized time / can exit，下一帧再进入状态机 context。

## Risks / Trade-offs
- Risk: 大删会短期破坏已有 Dodge 和 Locomotion 演示。
  - Mitigation: 先建立最小统一状态树并覆盖现有 Walk/Run/Dodge 行为，再删除旧路径；每个删除步骤都有静态验证和测试。

- Risk: 统一状态节点模型过度抽象，变成巨型万能配置。
  - Mitigation: 第一版只覆盖当前必要节点和条件；输出块按运动、动画、请求消费、事实写入拆分，避免把 Unity 对象塞进状态机。

- Risk: 动画配置贴近状态后，逻辑层可能重新依赖 Animancer。
  - Mitigation: 状态机只保存稳定 animation binding 或 transition asset 引用的配置数据；运行时 evaluator 不读取 Animancer state，动画进度通过纯数据 fact 回传。

- Risk: 现有 OpenSpec active changes 与本变更冲突。
  - Mitigation: 实施前先暂停或废弃被取代的 active changes；归档时按统一状态机更新最终 specs。

## Migration Plan
1. 停止继续实现缝合路线 active changes，确认本变更为后续实现基线。
2. 新建统一状态机配置、节点、transition、条件、输出和验证模型。
3. 用统一配置重建当前最小状态树：`FullBody/Locomotion/Idle`、`MoveStart`、`MoveLoop`、`MoveStop`、`FullBody/Action/Dodge`。
4. 把现有 Locomotion transition 和 Dodge entry/exit 规则迁移到同一张 transition 表。
5. 把 Directional/Backstep 运动参数、Run latch、请求消费和动画转换迁移为 `Dodge` 状态输出/变体输出。
6. 改造运行时组装层，只 tick 统一状态机，再提交输出到运动和动画 adapter。
7. 删除旧 Locomotion/Dodge/FullBody 缝合类、旧配置资产和旧测试。
8. 更新路线文档和最终 specs。

## Open Questions
- 统一状态机类型命名暂定为 `CharacterStateMachine`；实现前可以按现有命名空间最终确定。
- 第一版是否保留 UnityHFSM 作为内部执行内核，还是用项目自有小型解释器，需要实现时按配置表达能力和删除成本选择；无论选择哪个，外部 authoring 只能看到统一状态机模型。
