## Context
当前基础移动链路已经形成：

```text
PlayerLocomotionController
  -> BasicLocomotionPipeline
  -> BasicLocomotionStateMachine
  -> MovementCommand
  -> IBasicLocomotionMotionExecutor
  -> MovementAnimationContext
  -> BasicLocomotionAnimancerPresenter
```

Action 侧已有 `ActionInterruptArbiter`、`ActionInterruptPolicySetSO` 和 `ActionRuntimeStateTracker`，但它们目前只是纯数据仲裁与状态事实地基，不负责输入消费、动作生命周期、动画播放或运动输出。

Shift FullBody 动作是第一个需要把输入、仲裁、动作状态、动画表现和运动执行串起来的动作。该动作使用现有 `Action.Dodge` 作为稳定动作 ID，但语义包含两个分支：有方向输入时向当前移动方向冲刺，无方向输入时后闪。它必须沿用现有地基，不能新增一条绕过当前 locomotion pipeline、motion executor 或 Action 仲裁的旁路。

本次复查 BBB 参考实现后，最终架构口径需要更明确：BBB 并不是让 WASD 状态机和 FullBody 动作状态机作为两个平级路径同时控制 base layer。它的 Idle/MoveStart/MoveLoop/Stop、Dodge、Roll、Jump、Fall、Vault 等属于同一个 FullBody 主状态机。泛用全身接管动作通过 `OverrideState` 进入同一个主状态机，而不是新建第二个角色运动入口。

工业上常见做法也类似：输入先写入 intent/request buffer，玩法层或 ability/action 层仲裁，FullBody 主行为域决定当前 base layer 状态，运动只通过 Character Movement、MotionDriver 或等价统一出口提交；动画图、Animancer Presenter、Animation Blueprint 或 Montage 只消费命令并反馈播放进度。UpperBody、Facial、IK、Additive hit reaction 等表现层是未来问题，不属于当前层级 HFSM 变更。

配置资产上，工业实践通常不是把所有字段塞进一个巨大资产，也不是让设计者在多个互不相干的资产里手工拼装。更常见的是一个 Action/Ability 主定义作为编辑入口，内部按职责引用或内嵌 motion、interrupt、animation、cost/cooldown 等段落。这样运行时边界仍然清晰，设计者也能从一个入口看出 Dodge 是否配置完整。

## Goals / Non-Goals
- Goals:
  - 用最小可测闭环实现 Shift FullBody 动作规划。
  - 按一次 Shift 触发动作，不需要按住。
  - 有方向输入时向输入方向冲刺，无方向输入时后闪。
  - 方向冲刺完成后进入 Run 档位，不需要继续按住 Shift。
  - 角色完全停下并回到 Idle 后重置 Run latch，下次普通移动回到 Walk。
  - 无方向后闪不强制进入 Run。
  - 本变更不实现 cooldown；动作结束并回到 `Action.None` 后，再次按下 Shift 必须能重新触发该 FullBody 动作。
  - 收束为单一 FullBody 行为域：基础 Locomotion 局部状态图可以作为模块存在，但 Dodge/FullBody Action 不得成为独立平级移动路径。
  - 保持当前只实现 FullBody 层级主树，不在本变更中引入并行表现状态层。
  - 动作逻辑只输出语义变体和运动意图，不写死具体可琳动画。
  - 通过动作动画 Profile 配置具体动画资源，支持以后替换动画套件。
  - 最终提供明确的 FullBody 装配闭环，避免运动参数、打断策略和动画表现散落成设计者必须手工同步的多个游离配置。
  - 保持位移权威在统一运动出口或等价运动执行端口。
- Non-Goals:
  - 不实现完整动作 Timeline、cancel window、hitbox、IK、VFX/SFX 或连招。
  - 不新增独立 `Action.Sprint`，本变更只维护一个 Shift FullBody 动作。
  - 不把该动作建模为 Locomotion phase 或 gait。
  - 不让基础移动状态图接管该动作。
  - 不保留两套互相独立的 base layer 状态路径。
  - 不复制 BBB 运行时代码或依赖 `BBBNexus` 命名空间。
  - 不实现 UpperBody、Facial、IK、Additive 或等价并行表现状态层。

## Decisions
- Decision: 最终架构使用一个 FullBody 主行为域承载基础移动和全身动作。
  - Reason: Dodge、Roll、Jump、Vault 这类动作会接管 base layer、朝向、位移和退出后的移动状态；如果把它们做成和 WASD Locomotion 平级的第二套状态机，就会出现两个系统同时决定角色是否移动、播放什么 base layer 动画、何时回 Idle/Run 的分裂路径。
  - Implementation note: 现有 `BasicLocomotionStateMachine` 可以继续作为 Locomotion 子图或局部 phase 解析模块存在，但它必须受 FullBody 行为域调度。Dodge 可以是独立类、独立配置资产和独立测试夹具，但它的进入、退出、base layer 动画命令和位移输出必须归属于同一个 FullBody 主行为域。
  - BBB reference: BBB 的 `PlayerStateRegistry` 把 Idle、MoveStart、MoveLoop、Stop、Dodge、Roll、Override 等注册到同一个主 `StateMachine`；本变更只借这个 FullBody 主树口径，不把其他身体层纳入当前范围。

- Decision: 本变更只处理 FullBody 层级主树。
  - FullBody 主层：Idle、MoveStart、MoveLoop、MoveStop、Dodge、Jump、Fall、Vault、Death、Override 等会接管 base layer 或平面位移的状态。
  - Reason: 当前项目还没有并行表现层抽象；把 UpperBody/Facial/IK/Additive 写进本变更会制造未审批范围和状态权威歧义。
  - Future note: 后续若要接入 UpperBody、Facial、IK 或 Additive 状态层，必须另开 OpenSpec，并说明它们如何通过动画 layer/mask/weight 合成而不参与 FullBody owner 选择。

- Decision: 输入、仲裁、运动、动画的端口保持单向。
  - Pipeline: input/request buffer -> intent/action arbitration -> FullBody state/domain -> motion command/facts -> unified motion executor -> animation command/profile -> presenter/progress feedback。
  - Reason: 这样动作逻辑可以模块化测试，动画资源可以替换，运动权威仍然只有一个出口。
  - Implementation note: Presenter 可以播放直接 clip、Animancer transition 或 transition library entry，但它不决定动作是否允许、不切换 FullBody 状态、不写 Transform。

- Decision: 配置资产采用“装配入口聚合、职责边界分层”的 authoring 模式。
  - Reason: 工业上常见做法是有一个可审计装配入口能追踪动作逻辑和表现配置闭环，但运行时边界不会把动画 Profile 变成动作逻辑或状态树权威。
  - Implementation note: 当前 `DodgeActionConfigSO` 和 `ActionInterruptPolicySetSO` 属于动作逻辑入口；`ActionAnimationProfileSO` 属于动作动画绑定入口；角色 FullBody 主调度入口或等价装配点显式引用状态树、动作逻辑集和动作动画绑定集。Locomotion 的 Walk/Run 状态图和 TransitionLibrary 仍属于 Locomotion 配置，不并入 Dodge 主资产。
  - BBB reference: BBB 的 `PlayerSO` 聚合 Brain、LocomotionAnims、Dodging、Rolling、Action 等模块；不是每个模块完全混在一个类里，也不是运行时到处硬找散资产。

- Decision: Shift FullBody 动作使用 `Action.Dodge` 作为稳定动作 ID，不新增 `Action.Sprint`。
  - Reason: 用户需求是一个 FullBody 动作的两个分支，而不是两个动作。沿用一个 Action ID 能避免分裂路径。
  - Alternatives considered: 新增 `Action.Sprint` 并保留 `Action.Dodge`。该方案会把同一个 Shift 动作拆成两个变更和两套生命周期，不符合当前范围。

- Decision: 第一版只有两个变体。
  - `Directional`: Shift 请求发生时存在移动意图，方向取当前相机相对世界移动方向，动作完成后进入 Run 档位。
  - `Backstep`: Shift 请求发生时没有移动意图，方向取角色后方或等价当前 facing 反方向，动作完成后不强制进入 Run。
  - Reason: 满足当前用户需求，同时保留以后扩 4 向/8 向的空间。

- Decision: `Directional` 开始时立即把角色朝向转到冲刺方向，`Backstep` 保持当前朝向并向角色后方移动。
  - Reason: Directional 完成后要进入 Run 档位，朝向、位移方向和后续奔跑方向需要一致；Backstep 的动作语义是“向自己身后退”，不应该被相机朝向改变。
  - Alternatives considered: Directional 不转向或用插值慢转。该方案容易让冲刺位移和后续 Run 动画方向错位。Backstep 使用相机 forward 反方向会让镜头旋转影响后闪方向，手感不稳定。

- Decision: 动画通过 action animation key/profile 解析。
  - `Action.Dodge.Directional`
  - `Action.Dodge.Backstep`
  - Reason: 动作逻辑不绑定可琳 clip；可琳动画不好时只替换 profile，不修改 Dodge 规则。

- Decision: Shift 不再作为基础移动 held Run 输入。
  - Reason: Shift pressed 必须先进入输入缓冲和 Action 仲裁，否则输入层会提前决定 Run 结果，绕过 FullBody 动作。
  - Implementation note: Run 档位由动作完成后的 run latch 或等价移动事实表达，不由 Shift held 表达。该 latch 在角色完全停下并回到 Idle 后重置。

- Decision: 本变更不实现 cooldown，但必须允许动作结束后再次按 Shift 重新触发。
  - Reason: cooldown 系统之后会专门留变更处理；当前“只能 Shift 一次”是 bug，不是设计。
  - Implementation note: 动作退出到 `Action.None` 时不得把 current step 写进 tracker resistance，也不得让 input buffer 旧请求或 action state 残留导致后续新 Shift 永久被拒绝。

- Decision: 距离、时长、优先级、抗性和旋转策略由配置资产提供，代码只提供保守 fallback。
  - Reason: 这些是手感参数，工业实践中通常归设计调参所有；fallback 只保证缺配置时可测、可运行。
  - Initial defaults: Directional 可使用 duration 0.35s、distance 4m、priority 30、resistance 20、rotateToDirection true；Backstep 可使用 duration 0.30s、distance 2m-2.5m、priority 30、resistance 20、rotateToDirection false。实现阶段可在配置资产中调整，不写死具体角色资源。

- Decision: 位移通过动作运动命令或动作运动 facts 进入统一运动出口。
  - Reason: 保持项目既有“动画表现不直接移动角色”的边界。
  - Implementation note: 如果实现时发现必须让完整 Root Motion 驱动动作位移，必须停止并另建 OpenSpec 说明运动权威变化。

- Decision: 动作 active 期间相机 Look 继续响应。
  - Reason: 该动作是移动动作，不是 cinematic 或处决演出。工业上一般只压制角色基础移动位移和基础移动动画，不锁死玩家镜头。
  - Implementation note: Camera Resolve 可以继续走现有项目侧相机入口；Action runtime 不直接读取或控制 Cinemachine。

- Decision: `ActionRuntimeStateTracker` 继续只保存事实。
  - Reason: 现有规格明确 tracker 不自动退出、不消费输入、不播放动画。Dodge 生命周期应由新的薄层 action runner/driver 或等价模块协调。
  - Boundary update: 该 runner/driver 只能是 FullBody 行为域内部的状态执行模块，不能成为绕过 FullBody 主层的第二状态机。若当前实现已经形成独立路径，完成本变更前必须收束或明确改名/改边界。

## Risks / Trade-offs
- Risk: 第一版只有两个变体，方向冲刺使用同一个动画时，侧向或斜向输入可能观感不完美。
  - Mitigation: 位移方向和动画表现分离；以后可扩展为 4 向/8 向动画 key，不重写仲裁和输入消费。

- Risk: 当前可琳动画 clip 与期望位移距离/时长不匹配。
  - Mitigation: 动作运动配置独立于动画 clip，优先使用数值位移或烘焙 motion profile 修正，不让 clip 本身成为玩法位移权威。

- Risk: 动作 active 期间可能与基础移动同时推位移。
  - Mitigation: 动作 active 时必须暂停或覆盖基础移动输入驱动位移；第一版由动作 motion 输出主导，基础移动不产生额外平面输入位移。

- Risk: 为了快速跑通而保留 `DodgeActionRuntime`、`PlayerDodgeActionController` 等薄层对象时，后续维护者会把它们理解成独立于 FullBody 主层的第二套状态路径。
  - Mitigation: 文档和任务明确要求收束边界：这些对象要么成为 FullBody 主树 `Action` 分支叶子行为模块的内部实现，要么在后续实现中被重命名、合并或注册到统一 FullBody 行为域。

## Migration Plan
1. 将 Shift pressed 映射到现有 Dodge/FullBody 输入请求，停止 Shift held 直接驱动 Run。
2. 新增纯数据请求、变体、配置、动画 key 和运动参数模型。
3. 使用现有 `ActionInterruptArbiter` 判定 `Action.Dodge` 是否可进入。
4. accepted 后更新 `ActionRuntimeStateTracker`，并让 FullBody 行为域内部的 Dodge 状态/module 输出动画命令和运动命令。
5. 动画表现层通过 action animation profile 解析具体 clip/transition。
6. Directional 完成后设置 Run latch；Backstep 完成后不设置 Run latch。
7. 动作结束并回到 `Action.None` 后，新的 Shift pressed 请求必须能重新进入仲裁并再次触发动作。
8. 复查当前实现是否存在独立于 FullBody 主层的 Dodge runner/controller 路径；若存在，先收束成 FullBody 主树 `Action` 分支叶子行为模块边界，再标记任务完成。
9. 定向 EditMode 测试通过后，再做 prefab/scene 配置和手动验证。

## Open Questions
- 无。当前第一版按上述决策执行；后续若需要锁定目标侧闪、cooldown、耐力或完整 Root Motion，再另开小变更。
