## MODIFIED Requirements

### Requirement: BTSMTL 和 Timeline 必须只提交相机请求

系统 MUST 让 BTSMTL 自定义节点和 Timeline 相机表达只提交强类型相机请求，包括 `CameraSequenceRequest`、`CameraShakeRequest`、`CameraResponseRequest`、`CameraTargetSelectionRequest` 或读取 `CameraBasisSnapshot`。绑定动作实例的一次性触发 MUST 由 TreeClip 相机特殊 Node 提交；Timeline 中唯一的相机效果轨道 MUST 以窗口采样提交持续型强类型相机请求，窗口生命周期跟随时间轴时间。系统 MUST 不保留 CameraStateTrack、CameraResponseTrack、CameraCueTrack、CameraCueClip 或 `ActionCueClip(CueType: Camera)` 等触发型 Timeline 相机表达，MUST NOT 按效果类型拆分多条效果轨道。每个已公开 Camera Graph node MUST 由唯一 Graph emitter/typed binding 提供，保留 Graph/Node authoring identity、端口与 Source Map；Float32 与 Fixed Target MUST 按同一 domain 语义将其提交为现有 PresentationCommand。BTSMTL 节点、Timeline clip、Camera binding 和 Action operation MUST NOT 直接控制 Cinemachine、Unity Camera、camera Transform 或 virtual camera priority，也 MUST 不把 Camera runtime state 写入 Character/World simulation state。缺失字段、未知 binding 或 Target 未实现 MUST 在 prepare/composition 明确失败，不得跳过或使用 runtime fallback。

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

#### Scenario: Fixed Target 编译 Camera operation

- **WHEN** Fixed Graph artifact 与 domain binding 包含当前 operation-set version 的 Camera operation
- **THEN** Fixed Target MUST输出与 Float32 相同语义的强类型 PresentationCommand
- **AND** Camera request MUST不进入 deterministic CharacterState、WorldState或Snapshot


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

## ADDED Requirements

### Requirement: Fixed 回放必须区分 Fixed Body 与 Camera 视觉确定性

`CameraInitialState` MUST 只表示 Camera Presentation FramePlanner 的初始 yaw/pitch offset；相机位置、目标、roll、镜头历史和效果历史 MUST 由正式初始 Body、Profile/DefaultSequence、显式目标绑定、空效果状态或明确 Reset 边界派生。它 MUST NOT 被解释为完整相机世界快照。

Fixed replay MUST checkpoint 正式 Body，按 trace 逐 tick 重放 `SimulationInput`。被 Fixed 模型消费的 camera basis MUST 进入 trace payload；缺失 basis 的旧 trace MAY 按 trace heading 注入确定 basis，但注入规则 MUST 进入内容 hash。标准 replay MUST 使用显式 one-fixed-tick-per-presentation-frame drive 和 logic-locked presentation clock。

Camera Presentation 的视觉 basis MAY 继续由本地 live look 驱动。系统 MUST NOT 把缺少逐帧 camera look/basis/effect-clock 证据的视觉镜头声明为确定性回放结果；需要比较视觉镜头时 MUST 提供正式逐帧 basis trace、输入抑制或明确的镜头回放合同。

#### Scenario: 恢复 Fixed replay 初始朝向

- **WHEN** trace 声明 camera heading 且 LocalOwner Camera runtime 存在
- **THEN** replay MUST 通过正式 Presentation `SetCameraInitialState` 写入 Planner 初始角
- **AND** 缺少 Camera runtime MUST 失败，不得静默使用场景相机

#### Scenario: 重放被模型消费的 camera basis

- **WHEN** Fixed trace 帧包含 camera basis input
- **THEN** Fixed 模型 MUST 重放 trace 中的 basis，不得读取 live Camera 替换
- **AND** body replay hash MUST 与逐 tick input/body evidence 对应

#### Scenario: 视觉镜头确定性声明

- **WHEN** replay 只固定初始 heading 而 Look 仍为本地 live input
- **THEN** 文档 MUST 只宣称 fixed Body/model replay 确定
- **AND** Camera visual basis MUST 保持未证明或改用正式镜头回放合同

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

### Requirement: 相机必须提供实际采用与替换结果

Camera MUST 在采用前完整解析必需资源与 Rig/目标/物理上下文，失败时返回精确资源或请求来源与原因，不宣称新绑定已采用。成功采用 MUST 发布实际资源/内容版本和绑定实例/代际。Reset/替换 MUST 通过同一领域生命周期停止旧调用、重置声明的镜头历史和释放旧资源，旧实例结果不得写入新绑定。Preview 只能调用这些入口并读取结果，不自算相机、不伪造已采用。

#### Scenario: 新绑定缺少资源

- **WHEN** 候选绑定缺少正式效果资源或明确 Shot prefab
- **THEN** Camera MUST 报告对应身份与失败，实际采用状态 MUST 保持真实
- **AND** MUST 不以旧 Projection、旧 DLL 或资源名称占位作为新结果

### Requirement: 相机环境约束必须独立于平台查询实现

相机环境约束 MUST 消费最终期望计划、显式物理场景、正式碰撞配置、近裁剪保护体、自身/层/触发器过滤、delta 与 Reset，并输出安全计划及原因。抽象求解 MUST 不直接查找 Unity 场景对象；Unity 查询实现只返回环境事实。系统 MUST 处理起点重叠、转角移动、缩回、恢复和无合法空间，不能以未约束位置作为静默补齐。

#### Scenario: 镜头离开遮挡物

- **WHEN** 环境重新允许更远的期望位置
- **THEN** 系统 MUST 按正式恢复规则推进距离，且最终保护体仍满足本帧约束
- **AND** MUST 不改写角色跟随目标来实现避障

### Requirement: 相机请求生命周期必须保留稳定身份和同权裁决

请求 MAY 来自 TreeClip Node 瞬态提交、唯一 Timeline 效果轨道窗口采样或系统业务直提，三者提交同一种 typed request 并进入同一裁决域。Sequence、Response、Target 和效果请求 MUST 使用明确的 source、generation、action instance、cycle 与 event 身份，按各自裁决域的稳定规则处理同权候选。自然结束、取消、事件撤销、Owner 销毁与业务目标失效 MUST 进入正式退出合同，不能靠字典插入顺序或通用 Weight=0 代替。相机不拥有独立网络回滚日志。

#### Scenario: 同权请求发生替换

- **WHEN** 两个请求 priority 与 weight 相同
- **THEN** resolver MUST 按声明的身份规则得出稳定结果并记录原因
- **AND** 旧请求退出 MUST 不误删新 generation 或新 cycle 的请求
