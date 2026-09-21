# character-camera-pipeline Specification

## Purpose
定义角色本地相机从 committed PresentationCommand、CameraSequenceRequest、CameraShakeRequest、响应策略和目标绑定到 CharacterSimulationPresentationRuntime、CameraFramePlan 与 ICameraRigAdapter 的唯一表现链路。
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

系统 MUST 让 BTSMTL 自定义节点和 Timeline 相机表达只提交强类型相机请求，包括 `CameraSequenceRequest`、`CameraShakeRequest`、`CameraResponseRequest`、`CameraTargetSelectionRequest` 或读取 `CameraBasisSnapshot`。绑定动作实例的一次性触发 MUST 由 TreeClip 相机特殊 Node 提交；Timeline 中唯一的相机效果轨道 MUST 以窗口采样提交持续型强类型相机请求，窗口生命周期跟随时间轴时间。系统 MUST 不保留 CameraStateTrack、CameraResponseTrack、CameraCueTrack、CameraCueClip 或任何 Timeline 触发型 Cue 表达，MUST NOT 按效果类型拆分多条效果轨道。每个已公开 Camera Graph node MUST 由唯一 Graph emitter/typed binding 提供，保留 Graph/Node authoring identity、端口与 Source Map；Float32 与 Fixed Target MUST 按同一 domain 语义将其提交为现有 PresentationCommand。BTSMTL 节点、Timeline clip、Camera binding 和 Action operation MUST NOT 直接控制 Cinemachine、Unity Camera、camera Transform 或 virtual camera priority，也 MUST 不把 Camera runtime state 写入 Character/World simulation state。缺失字段、未知 binding 或 Target 未实现 MUST 在 prepare/composition 明确失败，不得跳过或使用 runtime fallback。Camera 只消费 TreeClip 相机特殊 Node、唯一效果轨道窗口采样或系统业务直提。

#### Scenario: BTSMTL 请求瞄准相机

- **WHEN** Aim 状态中的 RequestCameraSequence node 通过 Character Simulation Compiler 编译
- **THEN** Graph/Camera owner MUST 生成带稳定 Source Map 的 `CameraSequenceRequest(Aim)` domain request
- **AND** Target leaf MUST通过 PresentationCommand 提交该请求
- **AND** 节点 MUST NOT调用 `CinemachineFreeLook`、`Camera.main` 或 scene camera object

#### Scenario: TreeClip Node 触发技能特写

- **WHEN** TreeClip 执行到带正式相机特殊 Node 的技能特写时点
- **THEN** Node MUST 输出 `CameraSequenceRequest(SkillCloseup)` 并携带稳定 action/cycle/event 身份
- **AND** 系统 MUST NOT 通过 Timeline 触发型相机 Clip 采样表达该触发

#### Scenario: 唯一效果轨道窗口采样

- **WHEN** 时间轴进入唯一相机效果轨道的某个资源窗口
- **THEN** 窗口采样 MUST 提交该资源对应的持续型强类型相机请求
- **AND** 动作取消时窗口 MUST 继续按时间轴合同自然退出，MUST NOT 随动作实例立即消失或突跳

#### Scenario: Camera node 缺少目标配置

- **WHEN** SetCameraTarget node 缺少正式 target identity或包含未知 target kind
- **THEN** Compiler preflight MUST报告 node source identity并拒绝生成 Program
- **AND** runtime MUST不把该 node 当成 Success 或选择默认 CameraTarget

#### Scenario: Gameplay节点不是相机入口

- **WHEN** Logic TreeClip 提交攻击属性或其它非相机 Gameplay 节点输出
- **THEN** Camera MUST NOT 订阅、解释该输出或伪造镜头请求
- **AND** 相机触发仍 MUST 来自 TreeClip 相机特殊 Node 或唯一效果轨道窗口
#### Scenario: Fixed Target 编译 Camera operation

- **WHEN** Fixed Graph artifact 与 domain binding 包含当前 operation-set version 的 Camera operation
- **THEN** Fixed Target MUST输出与 Float32 相同语义的强类型 PresentationCommand
- **AND** Camera request MUST不进入 deterministic CharacterState、WorldState或Snapshot

### Requirement: 相机范围必须保持单角色

相机 runtime MUST由单个 Character 的 Presentation Runtime 拥有。相机 MAY使用同帧明确提供的敌人、Boss 或多个目标完成锁定和构图，但本要求不包含队伍切人、主控 Actor 切换、跨角色接管相机或换人生命周期。来源中的 `ChangeAvatar`、`SwitchIn`、`SwitchOut` 等字段和事件在没有后续正式设计前 MUST不被解释为本框架的换人能力；若保留来源字段，必须标明其当前没有运行时消费者。

#### Scenario: 角色与敌人共同取景

- **WHEN** 单个角色的序列需要角色与敌人共同构图
- **THEN** 相机 MUST消费同帧明确的多目标输入
- **AND** MUST不改变相机所有权或创建跨角色控制链

#### Scenario: 来源包含换人事件

- **WHEN** 来源资源包含 ChangeAvatar 或 SwitchIn/Out 名称
- **THEN** 本变更 MUST只记录来源身份和未决依赖
- **AND** MUST不自动生成换人请求、主控切换或第二角色相机 runtime

### Requirement: CharacterSimulationPresentationRuntime 必须是相机 runtime 唯一边界

系统 MUST使用 `CharacterSimulationPresentationRuntime` 作为角色相机runtime的唯一公开编排边界。该协调器 MAY拥有不可被Host、Network adapter或Gameplay代码直接访问的内部 `CharacterCameraPresentationRuntime`；内部 Camera Runtime MUST唯一拥有 Camera Sequence/Response/Target/Effect lifecycle、resolver、look input、bind offset 和 `ICameraRigAdapter` 调用。协调器 MUST在`PresentationFrame`中使用同一 `CharacterBodyPresentationFrame` 的visible pose推进Animation与Camera，并把Camera结果交给rig adapter。Camera capability MUST通过Factory的完整显式binding创建，MUST不决定Body clock策略。系统 MUST不保留Camera MonoBehaviour自主`LateUpdate`、外部Camera resolver调用或无相机Actor分配Camera容器的路径。

#### Scenario: Local Owner推进相机

- **WHEN** `CharacterPresentationFrameTarget`调用唯一 `ICharacterPresentationRuntime.Present`
- **THEN** 协调器 MUST先取得本帧唯一Body visible pose
- **AND** 内部Camera Runtime MUST使用该pose、已提交camera command、target binding和look input生成并应用CameraFramePlan

#### Scenario: 无相机 Simulated Actor

- **WHEN** Factory创建一个完整模拟但不拥有本地相机的Actor
- **THEN** MUST不创建Camera Runtime、request容器或resolver
- **AND** 该Actor仍 MUST使用其显式Body clock策略

#### Scenario: 无Camera组合收到Camera命令

- **WHEN** observed或simulated无Camera Actor收到Camera PresentationCommand
- **THEN** 唯一协调器 MUST报告明确配置错误
- **AND** MUST不搜索场景相机或创建默认Camera Runtime

#### Scenario: 禁止双驱动

- **WHEN** camera rig已由内部Camera Runtime驱动
- **THEN** Host、Network adapter和旧相机控制器 MUST不再修改同一个follow、aim、FOV或priority状态

### Requirement: Camera Sequence 必须使用注册的有限算法组合

系统 MUST 使用 Profile 注册的默认 Sequence 与强类型 stage 计算构图。FramePlanner MUST 保留基础轨道与手动偏移的明确合成；SequenceTransition MUST 处理进入、退出、Cut、BlendIn、BlendOut，History MUST 按正式时间保持连续状态。字段必须具有明确单位、坐标空间、消费者和依赖，未知 stage、未闭合公式或无消费者字段 MUST 明确拒绝，不得以默认值或静默忽略发布。

#### Scenario: 默认镜头接收鼠标输入

- **WHEN** 默认轨道已提供基础角度且本帧收到有效 Look
- **THEN** 系统 MUST 按响应权叠加手动角度，不得被基础轨道覆盖
- **AND** 普通推进 MUST 保持已定义的连续历史，只有明确 Reset 才重建相应状态

#### Scenario: 作者配置无消费者字段

- **WHEN** 一个字段被填写但当前算法未实现其语义
- **THEN** 系统 MUST 定位字段并拒绝该配置，或在已确认迁移中删除该字段及全部配置入口
- **AND** MUST 不把拷贝进 payload 或创建绑定对象当作字段已经生效

#### Scenario: 默认序列

- **WHEN** 本帧没有 active camera sequence request
- **THEN** resolver MUST 使用 Profile 的正式默认 Sequence
- **AND** 该默认状态 MUST NOT 需要 BTSMTL 每帧显式提交

#### Scenario: 技能特写覆盖瞄准

- **WHEN** 同一帧存在 Aim Sequence 和更高优先级 SkillCloseup Sequence
- **THEN** resolver MUST 选择或混合到 SkillCloseup Sequence
- **AND** debug MUST 能追踪获胜请求的 source id 或 action instance id

### Requirement: 相机响应策略必须和输入采集分离
系统 MUST 将输入采集和相机响应分离。`UnityCharacterSimulationInputAdapter` MUST继续采集 look 输入；Presentation camera resolver MUST根据 `CameraResponseRequest` 决定是否消费 look delta。系统 MUST使用 `Full`、`Suppressed`、`Weighted` 或等价有限响应模式表达响应权，MUST NOT将技能特写这类表现需求实现为停止采集输入。

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

### Requirement: Camera Effect owner 必须按固定顺序裁决表现

系统 MUST 将 Override、Zoom、Stretch、Shake、Shot 和环境约束作为职责明确的求解阶段。各 effect owner MUST 拥有自身资源、时间、空间、组合和退出语义，CameraEffectEvaluator 只负责顺序及生命周期转发。所有影响镜头位姿的效果 MUST 在最终环境约束与 RigAdapter 应用之前完成。尚未闭合的资源或消费者 MUST 明确拒绝；补齐实现之前不得仅移除错误。

#### Scenario: 受击效果接近墙体

- **WHEN** 一个已支持效果改变镜头期望位置
- **THEN** 该结果 MUST 继续进入统一环境约束，再交给 RigAdapter
- **AND** 效果 MUST 不通过改变角色或 follow/aim Transform 伪造镜头位移

#### Scenario: 命中帧震屏

- **WHEN** TreeClip 内的相机特殊 Node 提交正式 CameraShakeRequest
- **THEN** 内部 Camera Runtime MUST 保留请求的生命周期、顺序和 debug 来源，并交由对应 owner 求值
- **AND** 未闭合的 Shake MUST 明确拒绝，不得改变角色或目标伪造效果

#### Scenario: FOV 效果与特写同帧存在

- **WHEN** FOV 效果与专用特写请求在同帧有效
- **THEN** 效果 owner MUST 按正式固定顺序和权重修正计划
- **AND** MUST 不由各效果分别写入 Cinemachine lens 争夺结果

### Requirement: Cinemachine 必须是 CameraRigAdapter 实现细节

系统 MUST 由 Character Presentation 内部的 Planner、Transition、History 与效果/环境约束阶段求解 CameraFramePlan，通过 ICameraRigAdapter 的 CinemachineCameraRigAdapter 实现应用位姿、镜头参数并回读 CameraRigResult/CameraBasisSnapshot。Cinemachine 组件 MUST 不重新裁决业务目标、手动输入、动作生命周期或叠加一套未声明的 orbit/damping。Brain 推进必须由正式相机输出时序唯一负责；业务节点和 Timeline MUST 不直接写 Camera 或虚拟相机状态。

#### Scenario: 默认序列输出到 Cinemachine

- **WHEN** Adapter 收到最终 CameraFramePlan
- **THEN** 它 MUST 应用该计划并推进明确绑定的 Brain，回读实际输出
- **AND** MUST 不重新运行独立 FreeLook 输入或另一套跟随平滑

#### Scenario: Shot 使用专用 virtual camera

- **WHEN** 已支持的 CameraFramePlan 声明需要专用 Shot 承载
- **THEN** Adapter MAY 按正式计划切换明确绑定的 virtual camera 承载
- **AND** priority、进入和退出生命周期 MUST 由 Presentation runtime 控制，不得另加业务裁决或重复混合

### Requirement: Camera debug 必须解释状态和输出

系统 MUST 从同一 Runtime 状态提供原始/消费 Look、响应权、基准角和手动偏移、限幅、目标与请求来源、generation/action/cycle、时间域、blend、Reset、效果贡献、环境修正前后和最终 RigResult，并提供实际采用的资源/内容版本、绑定实例/代际与作者来源导航。诊断 MUST 不另算另一套相机；记录/回放 MUST 使用正式相机初始状态和输入合同，不依赖旧 Controller、整包 Projection 或隐式场景搜索。

#### Scenario: 排查 look 不响应

- **WHEN** 作者查看当前相机诊断
- **THEN** 诊断 MUST 能区分未采集、被响应抑制、已限幅、正在过渡或实际输出无效
- **AND** MUST 提供对应帧号与配置/请求来源

#### Scenario: 排查技能后镜头残留

- **WHEN** 技能 action instance 已结束
- **THEN** debug MUST 显示该 action-scoped camera request 是否已经清理或正在正式退出
- **AND** 当前镜头 MUST 能追踪到仍然有效的请求来源与退出原因

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
- **WHEN** 有效 `CameraTargetSelectionRequest.AnchorKey` 解析出正式世界点
- **THEN** Presentation camera resolver MUST使用该显式 follow point
- **AND** 系统 MUST NOT 把默认 camera anchor 绑定规则隐式应用到该世界点

#### Scenario: 表现根姿态缺失
- **WHEN** 默认 camera anchor 需要生成 follow point但当前没有有效 body sample
- **THEN** 内部 CharacterCameraPresentationRuntime MUST报告明确错误并停止生成该帧相机计划
- **AND** 系统 MUST NOT 回退读取 logic anchor 世界坐标、visual root Transform 或场景搜索结果

### Requirement: Camera Timeline控制曲线必须作为typed Curve Channel编辑

CameraSequenceClip与CameraResponseClip的Weight、Ease In与Ease Out曲线 MUST通过显式registered ChannelId进入Timeline Curve Editor。每条curve MUST继续由对应 Camera Clip 唯一拥有，使用 ClipNormalized 时间域和`[0,1]` bounded value domain，并通过 Camera Clip 正式 mutation API 原子替换。Camera Runtime MUST继续只消费既有 compiled request 与 presentation policy；Curve Editor、Catalog 与C#作者API MUST不直接控制 Cinemachine、virtual camera priority、Camera Transform 或创建第二个 Camera influence stack。

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

### Requirement: 相机资源与运行绑定必须按领域提供

Camera MUST 提供正式相机资源、必要转换/引用检查和只读运行绑定，由角色装配调用。现有只接收 Profile 的 CharacterCameraProjectionBuilder 中仍有消费者的处理 MUST 按领域保留，payload 只表达相机需要的数据，不携带角色总 Program、非相机目录或整包 Projection。Editor-only 资源处理 MUST 不直接搬进 Player；需要加工的资源继续走原独立资源流程，不新增 Camera-only 临时发布入口、运行时补构建或另一个总包。

技能 MUST 保持独立编译，控制和原生 Pose 不进入相机数据。资源/绑定身份变化 MUST 不重新打开已正确的 FramePlanner、轨道与鼠标叠加、History/Transition、Effect、Collision、Adapter、同帧 Body 和诊断算法。

#### Scenario: 角色装配安装相机

- **WHEN** 角色领域装配已取得 Profile、相机资源和明确环境输入
- **THEN** 它 MUST 调用 Camera 的正式只读绑定入口并处理真实失败
- **AND** MUST 不等待角色总 Program/整包 Projection 重发布

#### Scenario: 只修改相机资源参数

- **WHEN** 相机资源参数变化而技能请求种类、来源和时间合同未变
- **THEN** 仅该资源处理及 Camera 内容/绑定身份 MUST 按真实依赖更新
- **AND** MUST 不强制重建角色所有技能、Pose、网络 Pipeline 或其它资源

### Requirement: 相机请求生命周期必须保留稳定身份和同权裁决

请求 MAY 来自 TreeClip Node 瞬态提交、唯一 Timeline 效果轨道窗口采样或系统业务直提，三者提交同一种 typed request 并进入同一裁决域。Sequence、Response、Target 和效果请求 MUST 使用明确的 source、generation、action instance、cycle 与 event 身份，按各自裁决域的稳定规则处理同权候选。自然结束、取消、事件撤销、Owner 销毁与业务目标失效 MUST 进入正式退出合同，不能靠字典插入顺序或通用 Weight=0 代替。相机不拥有独立网络回滚日志。

#### Scenario: 同权请求发生替换

- **WHEN** 两个请求 priority 与 weight 相同
- **THEN** resolver MUST 按声明的身份规则得出稳定结果并记录原因
- **AND** 旧请求退出 MUST 不误删新 generation 或新 cycle 的请求

### Requirement: 相机环境约束必须独立于平台查询实现

相机环境约束 MUST 消费最终期望计划、显式物理场景、正式碰撞配置、近裁剪保护体、自身/层/触发器过滤、delta 与 Reset，并输出安全计划及原因。抽象求解 MUST 不直接查找 Unity 场景对象；Unity 查询实现只返回环境事实。系统 MUST 处理起点重叠、转角移动、缩回、恢复和无合法空间，不能以未约束位置作为静默补齐。

#### Scenario: 镜头离开遮挡物

- **WHEN** 环境重新允许更远的期望位置
- **THEN** 系统 MUST 按正式恢复规则推进距离，且最终保护体仍满足本帧约束
- **AND** MUST 不改写角色跟随目标来实现避障

### Requirement: 相机必须提供实际采用与替换结果

Camera MUST 在采用前完整解析必需资源与 Rig/目标/物理上下文，失败时返回精确资源或请求来源与原因，不宣称新绑定已采用。成功采用 MUST 发布实际资源/内容版本和绑定实例/代际。Reset/替换 MUST 通过同一领域生命周期停止旧调用、重置声明的镜头历史和释放旧资源，旧实例结果不得写入新绑定。Preview 只能调用这些入口并读取结果，不自算相机、不伪造已采用。

#### Scenario: 新绑定缺少资源

- **WHEN** 候选绑定缺少正式效果资源或明确 Shot prefab
- **THEN** Camera MUST 报告对应身份与失败，实际采用状态 MUST 保持真实
- **AND** MUST 不以旧 Projection、旧 DLL 或资源名称占位作为新结果
