# Proposal: 增加角色本地相机管线

## Why

当前项目已经把角色运动、动画和 Timeline 输出收口到管线中，但相机还停留在比较独立的 runtime 形态：

- `ThirdPersonCameraController` 直接持有 `CinemachineFreeLook`，曾承担输入、target 和相机空间求解等混合职责。
- 相机已有若干临时模型，但还没有进入 `CharacterPipeline` 的正式 stage，也缺少 Stage 统一裁决影响源的边界。
- `PresentationOutput` 目前只有通用 `PresentationCue`，缺少相机状态请求、响应策略、相机 modifier 和 pose plan。
- Timeline 和 Graph 的现行 spec 已经允许产出 Camera cue，但没有定义 BTSMTL 相机自定义节点、Timeline 相机轨道和 runtime 相机裁决边界。
- 技能特写、瞄准、锁定、禁用 look 响应、FOV kick、shake、recoil 等动作相机表现如果直接调用 Cinemachine，会形成 Timeline、节点、MonoBehaviour 多条控制路径。

这个 demo 的业务目标是第三人称动作客户端，镜头是手感和动作展示的核心。相机需要像 motion 和 animation 一样，由上游提交强类型意图，再由本地 runtime 统一裁决和输出。

## What Changes

本变更规划一条正式的本地相机管线：

- 新增 `character-camera-pipeline` 能力，定义相机是 local-only presentation domain。
- BTSMTL 自定义节点和 Timeline 相机轨道只提交 `CameraStateRequest`、`CameraCue`、`CameraResponsePolicy`、`CameraTargetRequest` 或读取 `CameraBasisSnapshot`。
- `CharacterCameraStage` 成为角色管线中唯一相机 runtime 边界，运行在 `PresentationFrame`，不进入网络同步和 correction。
- 相机状态第一阶段使用有限集合：`FreeLook`、`Aim`、`LockOn`、`ActionFocus`、`SkillCloseup`。
- 相机响应使用 response policy 表达，不叫输入锁定；输入继续采集，当前相机状态决定是否响应 look。
- 相机链路采用和 motion、animation 同构的方式：`CameraStateRequest / CameraCue -> CameraStateResolver -> CameraModifierResolver -> CameraPosePlan -> CameraRigAdapter`。Stage 只生成 adapter 输入，最终相机位置、旋转、orbit、damping 和遮挡交给 Cinemachine。
- `CharacterPresentationStage` 必须输出统一的 `PresentationRootPose`；默认 camera anchor 必须以该表现根姿态和正式绑定偏移生成每个表现帧的 follow point，不能继续读取 logic root 子节点的离散世界坐标。
- Cinemachine 只作为 adapter 实现，不能成为相机状态机、动作生命周期或 BTSMTL 请求的事实来源。
- `CameraBasisSnapshot` 可以被 Action、Motion 或 Graph 读取；一旦用于瞄准、突进方向或目标选择，必须固化成对应 gameplay fact，而不是同步相机状态本身。

## Non-Goals

- 不做相机网络同步。
- 不做服务端相机、camera rollback 或远端 camera replay。
- 不做通用相机公式编辑器或动态插件注册表。
- 不让 BTSMTL 节点、Timeline clip 或 Action 直接控制 Cinemachine。
- 不新增 `Camera.main`、`FindObjectOfType`、场景搜索或隐藏 fallback。
- 不改变命中、伤害、目标归属和 motion correction 的权威边界。
- 不新增测试；实现阶段只做 OpenSpec 校验和必要工具检查，端到端由用户验证。

## 当前代码事实

- `Assets/GameScripts/Main/Runtime/Camera` 已经是正式 Camera 模块目录。
- `ThirdPersonCameraController` 当前应收口为 Cinemachine FreeLook adapter，只写 FreeLook axis、Follow/LookAt target、lens 和 Brain，并读取 Cinemachine 输出 basis。
- Camera influence 必须以 `CameraStateRequest`、`CameraCue`、`CameraResponsePolicy`、`CameraTargetRequest` 等管线数据进入 `CharacterCameraStage`，不能作为 controller direct sink。
- `CharacterPipeline.PresentationFrame` 当前只调用 `CharacterPresentationStage.Update()`，然后清理 transient frame。
- `PresentationOutput` 当前只有 animation contributions、animation playback plans、animation snapshot 和通用 `PresentationCue`。
- `TimelinePlaybackScheduler` 当前会采样 animation、root motion、motion warp、action window、action cue；尚无正式 camera track/sample。
- `CharacterGraphContext` 已经有 Pipeline Blackboard 和 ActionContext，可作为相机 target key、action-scoped 请求和 basis 读取的正式上下文入口。
- Active change `add-character-presentation-interpolation` 正在规划 visual root / logic root 分离，并明确“相机跟随 visual root 还是 logic root”留给后续单独规划。

## 决策和 Tradeoff

### 方案 A：继续让相机 MonoBehaviour 自主运行

- 优点：改动最小，当前 `ThirdPersonCameraController` 已经能驱动 FreeLook。
- 缺点：动作、Timeline、BTSMTL 相机表现会通过外部调用散开；技能取消、特写残留、响应禁用和 debug 很难统一。
- 业务取舍：适合临时可玩，不适合展示动作客户端工程能力。

### 方案 B：BTSMTL 直接做相机执行器

- 优点：作者自由度最高，状态转换可以完全写在图里。
- 缺点：blend、FOV、肩位、遮挡、输入响应、Cinemachine 参数和生命周期清理会进入图执行细节，手感规则很难稳定。
- 业务取舍：会把相机算法变成图逻辑，后续每个技能都可能走出不同路径。

### 方案 C：BTSMTL 编排请求 + CameraStage 统一裁决

- 优点：相机状态和动作表现可由 BTSMTL/Timeline 编排，runtime 仍保持统一 resolver、modifier、adapter 链路；和 motion、animation 架构同构。
- 缺点：需要新增模型、节点、Timeline 轨道、CameraStage、debug 和 Cinemachine adapter 边界。
- 业务取舍：最适合当前 demo。动作作者能调镜头表现，工程侧能保证没有分裂路径。

本 proposal 选择方案 C。

### 方案 D：所有动作相机都用 Cinemachine virtual camera priority

- 优点：Unity 调参直观，技能特写和镜头切换实现快。
- 缺点：如果 priority 变化散在场景组件或 Timeline clip 中，业务状态会被 Cinemachine 场景对象吞掉。
- 业务取舍：可以作为 adapter 内部手段，但状态选择和生命周期必须仍由 `CharacterCameraStage` 管。

## 与现有 Spec 的关系

- `character-pipeline-runtime` 已要求 Timeline 和 Graph 产出的本地 camera cue 写入 presentation output，本变更补齐 camera output 的强类型模型和 stage 边界。
- `character-animation-pipeline` 已要求 Timeline 轨道只输出 pipeline 数据，不直接控制最终表现；本变更对 Camera 采用同一原则。
- `character-motion-semantics` 已要求最终 Transform 由 MotionStage 结算；本变更不允许相机直接移动或旋转角色，只允许 basis 被采样后转成 motion/action fact。
- `character-network-sync-domain-contract` 已允许 PresentationSyncDomain 表达 camera shake 且默认 local-only；本变更进一步说明 Camera runtime state 不写入 SyncFacts，只有明确需要记录或复制的表现事件才使用 PresentationSyncDomain。
- `add-character-presentation-interpolation` 正在规划 visual root；本变更会要求相机 follow anchor 使用正式绑定或 camera target plan，不能用隐藏 fallback。
- `add-character-presentation-interpolation` 已提供 previous/current logic sample 和 correction-aware interpolation alpha；本变更复用同一表现根姿态，不在 CameraStage 建立第二份插值历史。

没有发现和现行 spec 的直接矛盾；现行缺口是缺少相机本地管线、BTSMTL 相机 authoring surface、CameraStage 统一裁决和 Cinemachine adapter 边界。

## Impact

- 影响 `Runtime/Camera` 的模型、solver、runtime adapter。
- 影响 `Runtime/Character/Pipeline` 的 PresentationOutput、GraphContext、PipelineHost 和 PresentationFrame 调度。
- 影响 BTSMTL 自定义节点和 Timeline 轨道 authoring。
- 影响 Corin 或后续角色 prefab 的相机 rig 显式绑定。
