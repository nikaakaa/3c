# character-camera-pipeline Specification

## Purpose
定义角色本地相机从 committed PresentationCommand、状态请求、表现 cue、响应策略和目标绑定到 CharacterSimulationPresentationRuntime、CameraPosePlan 与 ICameraRigAdapter 的唯一表现链路。
## Requirements
### Requirement: Camera 必须是本地表现管线

系统 MUST将角色相机保持为local-only Presentation/Committer port。Camera runtime state、mode、FOV、orbit、shake、recoil、blend progress和Cinemachine priority MUST不进入CharacterSimulationState、WorldSimulationState、SimulationIngress、SimulationWorldSnapshot或model output port。明确需要复制的表现事件 MUST先由Program产生稳定EventId的GameplayFact或PresentationCommand，再由具体Model Egress按自己的显式coverage消费；Camera resolver state本身 MUST不成为同步事实。

#### Scenario: 本地技能特写

- **WHEN** committed command 请求 SkillCloseup
- **THEN** Camera port MUST在 PresentationFrame 本地切换或混合镜头
- **AND** model output adapter MUST不发送当前 Camera state

#### Scenario: 可复制表现事件

- **WHEN** 某个Model显式支持复制已提交的表现事件
- **THEN** model Egress MAY消费对应GameplayFact或PresentationCommand
- **AND** Presentation camera resolver 的本地状态 MUST不被复制

### Requirement: BTSMTL 和 Timeline 必须只提交相机请求
系统 MUST 让 BTSMTL 自定义节点和 Timeline 相机轨道只提交强类型相机输出，包括 `CameraStateRequest`、`CameraCue`、`CameraResponsePolicy`、`CameraTargetRequest` 或读取 `CameraBasisSnapshot`。BTSMTL 节点、Timeline clip 和 compiled Action operation MUST NOT 直接控制 Cinemachine、Unity Camera、camera Transform 或 virtual camera priority。

#### Scenario: BTSMTL 请求瞄准相机
- **WHEN** Aim 状态节点运行
- **THEN** 节点 MUST 提交 `CameraStateRequest(Aim)`
- **AND** 节点 MUST NOT 调用 `CinemachineFreeLook`、`Camera.main` 或 scene camera object

#### Scenario: Timeline 触发技能特写
- **WHEN** Timeline camera clip 采样到 SkillCloseup 窗口
- **THEN** clip MUST 输出 `CameraStateRequest(SkillCloseup)` 或等价 sample
- **AND** Timeline MUST NOT 直接修改 Cinemachine virtual camera priority

### Requirement: CharacterSimulationPresentationRuntime 必须是相机 runtime 唯一边界
系统 MUST使用 `CharacterSimulationPresentationRuntime` 作为角色相机 runtime 的唯一公开编排边界。该协调器 MAY拥有不可被 Host、Network adapter 或 Gameplay 代码直接访问的内部 `CharacterCameraPresentationRuntime`；内部 Camera Runtime MUST唯一拥有 Camera State/Response/Target/Cue lifecycle、resolver、look input、bind offset 和 `ICameraRigAdapter` 调用。协调器 MUST在 `PresentationFrame` 中使用同一 `CharacterBodyPresentationFrame` 的 visible pose 推进 Animation 与 Camera，并把 Camera 结果交给 rig adapter。Camera capability MUST通过 Factory 的完整显式 binding 创建，MUST不决定 Body clock 策略。系统 MUST不保留 Camera MonoBehaviour 自主 `LateUpdate`、外部 Camera resolver 调用或无相机 Actor 分配 Camera 容器的路径。

#### Scenario: Local Owner 推进相机
- **WHEN** `CharacterPresentationFrameTarget` 调用唯一 `ICharacterPresentationRuntime.Present`
- **THEN** 协调器 MUST先取得本帧唯一 Body visible pose
- **AND** 内部 Camera Runtime MUST使用该 pose、已提交 camera command、target binding 和 look input 生成并应用 CameraPosePlan

#### Scenario: 无相机 Simulated Actor
- **WHEN** Factory 创建一个完整模拟但不拥有本地相机的 Actor
- **THEN** MUST不创建 Camera Runtime、request 容器或 resolver
- **AND** 该 Actor 仍 MUST使用其显式 Body clock 策略

#### Scenario: 无 Camera 组合收到 Camera 命令
- **WHEN** observed 或 simulated 无 Camera Actor 收到 Camera PresentationCommand
- **THEN** 唯一协调器 MUST报告明确配置错误
- **AND** MUST不搜索场景相机或创建默认 Camera Runtime

#### Scenario: 禁止双驱动
- **WHEN** camera rig 已由内部 Camera Runtime 驱动
- **THEN** Host、Network adapter 和旧相机控制器 MUST不再修改同一个 follow、aim、FOV 或 priority 状态

### Requirement: CameraStateResolver 必须使用有限状态仲裁
系统 MUST 使用有限 camera mode 和稳定仲裁规则决定当前相机状态。第一阶段 camera mode MUST 至少覆盖 `FreeLook`、`Aim`、`LockOn`、`ActionFocus` 和 `SkillCloseup`。Resolver MUST 支持 priority、weight、source identity、action instance lifecycle 和 blend 参数。系统 MUST NOT 使用动态脚本公式或场景对象 priority 作为相机状态真相。

#### Scenario: 默认 FreeLook
- **WHEN** 本帧没有 active camera state request
- **THEN** resolver MUST 使用 `FreeLook` 作为 base state
- **AND** 该默认状态 MUST NOT 需要 BTSMTL 每帧显式提交

#### Scenario: 技能特写覆盖瞄准
- **WHEN** 同一帧存在 `Aim` 和更高优先级 `SkillCloseup`
- **THEN** resolver MUST 选择或混合到 `SkillCloseup`
- **AND** debug MUST 能追踪获胜请求的 source id 或 action instance id

### Requirement: 相机响应策略必须和输入采集分离
系统 MUST 将输入采集和相机响应分离。`UnityCharacterSimulationInputAdapter` MUST继续采集 look 输入；Presentation camera resolver MUST根据 `CameraResponsePolicy` 决定是否消费 look delta。系统 MUST使用 `Full`、`Suppressed`、`Weighted` 或等价有限响应模式表达响应权，MUST NOT将技能特写这类表现需求实现为停止采集输入。

#### Scenario: 技能特写不响应 look
- **WHEN** 当前 camera mode 为 `SkillCloseup`
- **AND** response policy 为 `Suppressed`
- **THEN** Unity input adapter MUST仍然采集 look 输入
- **AND** Presentation camera resolver MUST不把该 look 输入用于手动 orbit

#### Scenario: 瞄准降低手动旋转权重
- **WHEN** 当前 camera mode 为 `Aim`
- **AND** response policy 为 `Weighted`
- **THEN** Presentation camera resolver MUST按 manual orbit weight 消费 look 输入
- **AND** 输入数据本身 MUST 保持可被 input history 或 action request 使用

### Requirement: CameraBasisSnapshot 必须作为可采样事实暴露
系统 MUST 暴露稳定的 `CameraBasisSnapshot` 或等价事实给 Graph、Action 和 Motion 使用。该 snapshot MUST 至少表达 planar forward、planar right、look direction、aim point、yaw 和 pitch。若相机 basis 被用于技能瞄准、突进方向、目标选择或角色 yaw，系统 MUST 将采样结果固化为对应 Action、Motion 或 Gameplay fact，而不是依赖后续实时 camera state。

#### Scenario: 射击动作采样相机方向
- **WHEN** 玩家启动射击动作
- **THEN** 动作逻辑 MUST 读取当前 `CameraBasisSnapshot`
- **AND** 射击方向 MUST 写入 action activation、action context 或等价 gameplay fact

#### Scenario: 相机之后继续转动
- **WHEN** 技能已经固化 aim direction
- **AND** 玩家随后旋转相机
- **THEN** 已提交技能事实 MUST NOT 随实时 camera state 变化

### Requirement: CameraTarget 必须来自正式上下文
系统 MUST 让相机 follow、aim、lock-on 和 skill closeup target 来自正式绑定、target request、Pipeline Blackboard、ActionContext 或等价 runtime context。系统 MUST NOT 使用 `Camera.main`、`FindObjectOfType`、无声明 scene search 或隐藏 fallback 补齐目标。

#### Scenario: 锁定目标缺失
- **WHEN** `LockOn` 请求引用的 target key 不存在
- **THEN** Presentation camera resolver MUST按正式缺失策略报告或降级该请求
- **AND** 系统 MUST NOT 自动搜索最近敌人或任意场景对象作为 fallback

#### Scenario: 跟随 visual root
- **WHEN** 相机配置要求跟随 visual root 或 camera anchor
- **THEN** Host 或 camera target plan MUST 显式提供该绑定
- **AND** 缺失绑定 MUST 报告配置错误

### Requirement: Camera modifier 必须按有限顺序裁决相机表现意图
系统 MUST 将 shake、FOV kick、recoil、collision correction 和类似表现修正作为 camera modifier 或 cue 进行生命周期和顺序裁决。Modifier MUST 在 `CameraStateResolver` 选定基础 camera state 后，按固定有限顺序作用于 `CameraPosePlan`。Presentation camera resolver MUST只生成 rig adapter 可消费的 follow point、aim point、lens/FOV、look response 和 cue 意图。系统 MUST NOT让 modifier 绕过该边界修改 Cinemachine 或 Unity Camera，也 MUST NOT让 resolver 自行计算 Unity Camera position、rotation、orbit radius、shoulder offset 或 collision distance。

#### Scenario: 命中帧震屏
- **WHEN** Timeline 或 Graph 提交 `CameraCue(Shake)`
- **THEN** 内部 CharacterCameraPresentationRuntime MUST保留该 cue 的生命周期、顺序和 debug 来源
- **AND** adapter 或 Cinemachine noise/impulse 配置 MUST 负责实际震屏表现
- **AND** Presentation runtime MUST NOT通过扰动 Follow/LookAt target 伪造震屏

#### Scenario: FOV kick 与 SkillCloseup 同帧存在
- **WHEN** 当前 mode 为 `SkillCloseup`
- **AND** 本帧存在 `FOVKick` cue
- **THEN** CameraModifierResolver MUST 按固定顺序叠加 FOV 修正
- **AND** debug MUST 能显示 FOV 来源

### Requirement: Cinemachine 必须是 CameraRigAdapter 实现细节
系统 MUST通过 `ICameraRigAdapter` 的正式实现 `ThirdPersonCameraController` 将 `CameraPosePlan` 应用到 Unity 相机系统。CharacterSimulationPresentationRuntime、BTSMTL 节点、Timeline clip 和 compiled Action operation MUST NOT直接依赖 Cinemachine 组件作为业务状态机。Adapter MAY使用 `CinemachineFreeLook`、virtual camera priority、FreeLook axis、Follow/LookAt、lens、noise 和 CinemachineBrain blend 实现输出。Adapter MUST NOT持有独立于 Presentation runtime 的 camera influence stack、target resolver 或动作生命周期裁决。

#### Scenario: FreeLook 输出到 Cinemachine
- **WHEN** `CameraPosePlan` 表达 FreeLook follow point、aim point、FOV 和裁决后的 look delta
- **THEN** Cinemachine adapter MAY 更新 FreeLook axis、Follow、LookAt 和 lens
- **AND** Cinemachine MUST 负责最终相机位置、旋转、orbit 和 damping
- **AND** FreeLook 是否生效 MUST来自 Presentation runtime 的计划而不是 Cinemachine 自己的业务判断

#### Scenario: SkillCloseup 使用专用 virtual camera
- **WHEN** `CameraPosePlan` 表达 SkillCloseup
- **THEN** adapter MAY 提升专用 virtual camera priority
- **AND** priority 的生命周期 MUST由 Presentation runtime 控制

### Requirement: Camera debug 必须解释状态和输出
系统 MUST 提供或预留 camera debug 数据，说明当前 camera mode、active requests、active cues、source identity、action instance、priority、blend progress、response policy、target 来源、basis 和输出 pose plan。Debug MUST 服务于动作镜头、输入响应和技能取消排查。

#### Scenario: 排查技能后镜头残留
- **WHEN** 技能 action instance 已结束
- **THEN** debug MUST 能显示 action-scoped camera request 是否已经清理
- **AND** 当前 camera mode MUST 能追踪到仍然 active 的请求来源

#### Scenario: 排查 look 不响应
- **WHEN** 玩家移动鼠标但相机没有 orbit
- **THEN** debug MUST 能显示当前 response policy 是否为 `Suppressed` 或低权重 `Weighted`

### Requirement: 默认相机跟随必须使用统一表现根姿态
系统 MUST让 `CharacterBodyPresentationRuntime` 基于正式 previous/current `CharacterBodySample` 和其显式 Body clock 策略生成唯一 `CharacterBodyPresentationFrame` 与 visible pose。默认 camera anchor MUST作为相对初始 logic body 的绑定偏移保存，并由内部 `CharacterCameraPresentationRuntime` 使用同一 visible pose 生成 follow point。系统 MUST不让默认相机在表现帧直接读取 logic body 或 camera anchor 子节点的离散世界坐标，也 MUST不维护第二份 pose 插值历史。

#### Scenario: 渲染帧高于 logic tick
- **WHEN** 两个 logic tick 之间执行多个 PresentationFrame
- **THEN** visual root 和默认 camera follow point MUST使用同一个插值 body pose
- **AND** CameraRigAdapter MUST 在每个表现帧收到连续的 follow point
- **AND** 相机 MUST NOT 因 logic anchor 未更新而交替冻结和跳变

#### Scenario: 强制位置校正
- **WHEN** 表现层因正式 motion correction 使用贴合策略
- **THEN** 同一插值 body pose MUST同时驱动 visual root 和默认 camera follow point
- **AND** Presentation runtime MUST NOT使用旧 logic anchor 世界坐标产生不同步的第二次贴合

#### Scenario: 显式相机目标
- **WHEN** 有效 `CameraTargetRequest.AnchorKey` 解析出正式世界点
- **THEN** Presentation camera resolver MUST使用该显式 follow point
- **AND** 系统 MUST NOT 把默认 camera anchor 绑定规则隐式应用到该世界点

#### Scenario: 表现根姿态缺失
- **WHEN** 默认 camera anchor 需要生成 follow point但当前没有有效 body sample
- **THEN** 内部 CharacterCameraPresentationRuntime MUST报告明确错误并停止生成该帧相机计划
- **AND** 系统 MUST NOT 回退读取 logic anchor 世界坐标、visual root Transform 或场景搜索结果

### Requirement: Camera Timeline控制曲线必须作为typed Curve Channel编辑

CameraStateClip与CameraResponseClip的Weight、Ease In与Ease Out曲线 MUST通过显式registered ChannelId进入Timeline Curve Editor。每条curve MUST继续由对应Camera Clip唯一拥有，使用ClipNormalized时间域和`[0,1]` bounded value domain，并通过Camera Clip正式mutation API原子替换。Camera Runtime MUST继续只消费既有compiled request与presentation policy；Curve Editor、Catalog与Agent MUST不直接控制Cinemachine、virtual camera priority、Camera Transform或创建第二个Camera influence stack。

#### Scenario: 编辑Camera Ease In

- **WHEN** 作者在Camera Track展开Ease In channel并移动key
- **THEN** Editor MUST在该Camera Clip起止帧内显示并提交完整curve
- **AND** Camera compile/presentation链 MUST沿既有入口消费修改结果

#### Scenario: Camera与Animation曲线同时展开

- **WHEN** Timeline同时显示Camera Track与Animation Track的CURVES分组
- **THEN** 两者 MUST复用同一Curve Lane交互与frame geometry
- **AND** 每条curve MUST继续使用自己的owner mutation、ChannelId和runtime consumer

#### Scenario: Curve Editor尝试直接控制Cinemachine

- **WHEN** 作者修改Camera Weight或Ease channel
- **THEN** Curve Editor MUST只修改Camera Clip authoring
- **AND** MUST不直接访问Cinemachine组件或写Camera Transform
