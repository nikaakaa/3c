# Design: 角色本地相机管线

## Context

项目已经形成稳定模式：

```text
上游提交强类型来源
Runtime 统一裁决
Modifier 做有限修正
Stage 输出最终计划
Adapter 应用到 Unity 对象
```

motion 链路是：

```text
MotionContribution -> MotionResolver -> MotionModifier -> MotionIntent -> CharacterController.Move
```

animation 链路是：

```text
AnimationContribution -> CharacterAnimationLayerRuntime -> AnimationLayerPlaybackPlan -> AnimancerAnimationPresenter
```

相机应使用同构链路：

```text
CameraStateRequest / CameraCue
  -> CameraStateResolver
  -> CameraModifierResolver
  -> CameraPosePlan
  -> CameraRigAdapter
```

## Goals

- 让 FreeLook、Aim、LockOn、ActionFocus、SkillCloseup 成为正式有限 camera mode。
- 让 BTSMTL 和 Timeline 能编排相机状态、响应策略、目标请求和表现 cue。
- 让相机响应策略表达“采集但不响应”，避免使用误导性的 input lock。
- 让相机作为 local-only presentation domain，不污染 Motion、Action、SyncFacts 和网络 correction。
- 让 Cinemachine 只承担最终输出和 Unity 镜头能力，不承担业务状态机。

## Non-Goals

- 不同步 camera mode、FOV、肩位、shake 状态或 blend 进度。
- 不让 PresentationFrame 重新运行 BTSMTL、Timeline 或 ActionRuntime。
- 不恢复旧相机控制路径或场景搜索。
- 不做所有相机类型的通用编辑器；第一阶段只服务第三人称动作 demo。

## Proposed Architecture

### 1. Camera model

第一阶段新增有限模型：

```text
CameraMode
  FreeLook
  Aim
  LockOn
  ActionFocus
  SkillCloseup

CameraStateRequest
  mode
  priority
  weight
  blendIn
  blendOut
  targetKey
  sourceId
  sourceActionInstanceId
  interruptPolicy

CameraResponsePolicy
  lookResponse
  manualOrbitWeight
  pitchResponseWeight
  yawResponseWeight

CameraCue
  cueId
  cueType
  intensity
  duration
  priority
  sourceId
  sourceActionInstanceId

CameraTargetRequest
  targetKey
  anchorKey
  aimPointKey
  preferredBoneKey

CameraBasisSnapshot
  planarForward
  planarRight
  lookDirection
  aimPoint
  yaw
  pitch

CameraPosePlan
  followPoint
  aimPoint
  fieldOfView
  lookDelta
  responsePolicy
  debug
```

这些类型属于相机和角色 presentation contract，不是网络包，也不是 Cinemachine 数据结构。

### 2. BTSMTL authoring surface

BTSMTL 自定义节点只提交请求或读取事实：

```text
RequestCameraStateNode
EmitCameraCueNode
SetCameraResponseNode
SetCameraTargetNode
ReadCameraBasisNode
```

节点不持有 scene camera，不调用 Cinemachine，不做 `Camera.main` 搜索。

业务含义：

- 普通移动状态可以不提交相机请求，由 runtime 使用 FreeLook base state。
- Aim 状态每帧提交 `CameraStateRequest(Aim)` 和对应 response policy。
- 技能 Timeline 某段提交 `CameraStateRequest(SkillCloseup)`、`LookResponse = Suppressed`、`ManualOrbitWeight = 0`。
- 命中帧提交 `CameraCue(Shake)` 或 `CameraCue(FOVKick)`。
- 射击或突进动作启动时通过 `ReadCameraBasisNode` 读取 basis，并把方向固化进 Action/Motion 数据。

### 3. Timeline camera tracks

Timeline 可新增相机轨道，但轨道只输出 pipeline 数据：

```text
CameraStateTrack / CameraStateClip
CameraCueTrack / CameraCueClip
CameraResponseTrack / CameraResponseClip
CameraTargetTrack / CameraTargetClip
```

Timeline 不直接控制 Cinemachine priority、FreeLook axis、FOV 或 Transform。它只采样时间窗口并提交 typed output。

### 4. CharacterCameraStage

`CharacterCameraStage` 在 `CharacterPipeline.PresentationFrame` 中运行，职责是：

1. 读取当前 `PresentationOutput` 中的相机请求、cue 和 response policy。
2. 读取上一轮 logic tick 已确定的 motion result、action lifecycle、target context 和 camera basis。
3. 运行 `CameraStateResolver`，从 FreeLook base state 和所有请求中选出当前 camera mode。
4. 运行 response policy 合并，决定本表现帧是否响应 look delta。
5. 解析 follow anchor、aim point 和 target key。
6. 生成 `CameraPosePlan`，只表达 follow、aim、lens 和 look response 后的输入。
7. 按有限顺序维护相机 cue，第一阶段只让 FOV kick 写入 lens 修正；shake、recoil 和 collision correction 保留为 adapter/Cinemachine 可消费的表现意图。
8. 通过 `ICameraRigAdapter` 应用到 Cinemachine 或其它 camera rig。
9. 输出 `CameraDebugSnapshot` 和下一帧可读的 `CameraBasisSnapshot`。

它不调用 `CharacterController.Move`，不写 `StrictGameplayOutput`，不写 `SyncFacts`，不直接发送网络消息，也不计算 Unity Camera 的最终 position、rotation、orbit radius、shoulder offset 或 collision distance。

### 5. Response policy

输入采集和输入响应必须分离：

```text
InputStage 始终采集 look / move / action
CameraResponsePolicy 决定 CameraStage 是否消费 look
MotionContribution 决定 MotionStage 是否消费 locomotion
ActionRuntime 决定 action request 是响应、忽略还是 buffer
```

相机第一阶段 response mode：

```text
Full
Suppressed
Weighted
```

技能特写的语义是：

```text
LookResponse = Suppressed
ManualOrbitWeight = 0
```

不是锁输入。玩家输入仍可被 InputStage 采集和记录，只是当前 camera mode 不响应。

### 6. Camera basis and gameplay facts

相机状态本身不参与同步，但 basis 可以被玩法采样：

```text
CameraBasisSnapshot -> ActionActivationRequest / ActionContext / MotionContribution
```

一旦 basis 影响射击方向、突进方向、目标选择或角色 yaw，它必须被固化成对应玩法事实。后续网络、debug 或服务端校验读取的是这些事实，而不是实时 camera state。

### 7. Follow target and presentation interpolation

相机 follow anchor 必须来自正式配置或 camera target plan。它可以选择 logic root、visual root、骨骼点或 target context，但选择必须显式表达。

实现时需要和 `add-character-presentation-interpolation` 协调：

- 若跟随 logic root，镜头更贴近碰撞和判定真值，但视觉可能更硬。
- 若跟随 visual root，镜头更平滑，但强 correction 时需要明确贴合策略。
- 若跟随独立 camera anchor，表现最灵活，但 prefab 绑定要求更高。

第一阶段不允许缺失配置后偷偷回退到 `transform` 或 `Camera.main`。

当前本地角色默认 camera anchor 采用统一表现根姿态方案：

```text
previous/current logic pose + interpolation alpha
  -> PresentationRootPose
      -> visual root bind offset
      -> camera anchor bind offset
```

`CharacterPresentationStage` 是 `PresentationRootPose` 的唯一计算入口。`CharacterCameraStage` 只消费该姿态，并用初始化时从正式 logic root 与 camera anchor 绑定计算出的局部偏移生成 follow point。camera anchor 的场景 Transform 继续表达 authoring 绑定，但其 logic-tick 世界坐标不再作为每个表现帧的 follow 真值。显式 `CameraTargetRequest.AnchorKey` 仍使用请求提供的正式世界点，不被默认 anchor 规则重解释。

强制 correction 时沿用表现层现有 `alpha = 1` 语义，使 visual root 和 camera follow point 在同一表现帧贴合。表现根姿态缺失时必须暴露配置或调度错误，不得回退读取 logic anchor 世界坐标。

### 8. Cinemachine adapter

`CharacterCameraStage` 依赖 `ICameraRigAdapter`，不直接依赖 `CinemachineFreeLook` 或 `CinemachineVirtualCamera`。

推荐实现：

```text
CinemachineCameraRigAdapter
  Apply(CameraPosePlan plan)
```

adapter 可以内部使用：

- 单 FreeLook rig 驱动 FreeLook / Aim / LockOn / ActionFocus。
- 专用 virtual camera 驱动 SkillCloseup。
- CinemachineBrain blend。
- FreeLook axis、Follow / LookAt target、FOV、lens、noise、priority。

但 camera mode 选择、priority 来源、action lifecycle 和 response policy 必须来自 `CharacterCameraStage`。第三人称控制器只作为 Cinemachine adapter：写入 FreeLook axis、Follow/LookAt target 和 lens，读取 Cinemachine 输出 basis，不持有独立 influence stack 或相机空间解算。

## Runtime Order

```text
LogicTick
  NetworkReceiveStage
  InputStage
  CharacterBTSMTLPhase
    BTSMTL nodes submit camera requests
    Timeline camera tracks submit camera samples
  MotionStage
  NetworkSendStage

PresentationFrame
  CharacterPresentationStage
  CharacterCameraStage
    CameraStateResolver
    CameraModifierResolver
    CameraPosePlan
    CameraRigAdapter
  Frame cleanup
```

`CharacterCameraStage` 必须消费 `CharacterPresentationStage` 输出的 `PresentationRootPose`，不得重复维护 previous/current pose、重新计算 interpolation alpha 或读取 logic anchor 世界坐标驱动默认 follow。

## Lifecycle

相机请求可以是 frame-scoped、state-scoped 或 action-scoped：

- frame-scoped：当前 tick 产出，帧末自然清理。
- state-scoped：BTSMTL 状态持续提交，状态退出后消失。
- action-scoped：绑定 `ActionInstanceId`，action succeeded、cancelled、rejected 或 interrupted 后清理。

`CharacterCameraStage` 必须处理 action-scoped request 的残留清理。SkillCloseup、ActionFocus 这类状态不能在动作结束后继续占用 camera mode。

## Debug

第一阶段 debug 至少需要展示：

- 当前 camera mode。
- active state requests。
- active cues/modifiers。
- source id 和 action instance id。
- priority、weight、blend progress。
- response policy。
- follow target / aim target 来源。
- 当前 basis。
- 输出 pose plan 摘要。

这不是为了做复杂工具，而是为了让面试展示时能解释动作镜头为什么这样切、为什么某段技能不响应 look、为什么相机没有进入网络同步。

## Tradeoffs

### 相机状态放 BTSMTL 还是代码

完全写代码最稳定，但技能表现变化都要改 runtime。完全放 BTSMTL 最自由，但手感和 lifecycle 会散。本方案让 BTSMTL 表达请求，代码负责裁决并生成 Cinemachine adapter 输入，最终镜头求解交给 Cinemachine，兼顾创作自由和工程边界。

### SkillCloseup 使用单 rig 还是专用 vcam

单 rig 链路统一，debug 清楚，但复杂特写调参不如 Cinemachine vcam 直观。专用 vcam 适合大招和处决，但必须由 Stage 控制 priority 和生命周期，避免场景对象变成业务真相。第一阶段允许 adapter 内部混合使用。

### 相机 cue 写 PresentationOutput 还是 SyncFacts

本地 camera state、FOV、shake 和 blend 默认写 `PresentationOutput`，不进 SyncFacts。只有明确需要记录、回放或复制的表现事件才进入 `SyncFacts.Presentation`。业务上这样能保持本地手感自由，不让网络延迟污染镜头。

### follow logic root 还是 visual root

logic root 更权威，visual root 更顺。这个选择要在 camera target plan 中显式表达。第一阶段不做隐藏 fallback，避免调不清到底跟的是谁。

当前本地默认 camera anchor 选择表现根姿态。业务收益是角色与整屏镜头使用同一个表现时钟；代价是本地镜头相对 logic truth 保留与 visual root 相同的一个插值区间延迟。显式 target request 仍可提供其它正式目标语义，不通过读取默认 logic anchor 形成第二条路径。

## Risks

- 如果相机请求和旧 `LateUpdate` 同时驱动，会出现双控制；实现阶段必须移除或禁用旧自主驱动。
- 如果 Timeline camera track 直接改 Cinemachine，会违反本方案并形成分裂路径；实现阶段要通过代码审查和搜索清理。
- 如果 target key 没有正式来源，LockOn 和 SkillCloseup 会缺目标；实现阶段应报配置错误，而不是场景搜索。
- 如果 action-scoped request 没有清理，技能特写会残留；必须接入 action lifecycle。

## Open Questions

- Corin prefab 是否已有独立 camera anchor、visual root 和 aim target，需要实现阶段检查。
- LockOn 的目标选择系统尚未正式规划，本 change 只定义 target request 和 target key，不实现完整目标选择器。
- SkillCloseup 的默认打断策略需要按具体技能定：受击是否打断、强 correction 是否立即退出、玩家取消是否 blend out。
