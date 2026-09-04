## MODIFIED Requirements

### Requirement: BTSMTL 和 Timeline 必须只提交相机请求

系统 MUST让 BTSMTL 节点和 Timeline 相机轨道只声明强类型序列请求、效果触发、响应策略、目标请求或读取 CameraBasisSnapshot。每个公开能力 MUST经唯一 Compiler 降低为版本化 Program operation，并保留 Graph/Node/Timeline/Track/Clip identity、端口和 Source Map；Float32 与 Fixed MUST按同一语义提交现有 PresentationCommand。序列、Override、Zoom、Stretch、Shake、Shot 和曲线的实际表现参数 MUST从同一 Projection 的正式相机资源绑定取得。节点、Clip、编译 operation 与 Action MUST不直接控制 Cinemachine、Unity Camera、相机 Transform 或优先级，不将本地相机状态写入 Character/World state。未知类型、缺失字段、无效资源引用和 Target 未实现 MUST在 build/composition 明确失败。

#### Scenario: BTSMTL 请求瞄准相机

- **WHEN** Aim 状态中的相机节点通过正式 Compiler 编译
- **THEN** emitter MUST生成带稳定 Source Map、请求身份和正式 Aim 序列绑定的 operation
- **AND** Target MUST通过 PresentationCommand 提交请求，节点 MUST不调用相机组件

#### Scenario: Timeline 触发技能特写

- **WHEN** Timeline 相机 Clip 到达 SkillCloseup 窗口
- **THEN** Clip MUST提交对应 Shot/Sequence 的生命周期请求
- **AND** Timeline MUST不直接修改虚拟相机优先级或创建镜头播放器

#### Scenario: Camera node 缺少目标配置

- **WHEN** 目标节点缺少正式目标身份或包含未知目标类型
- **THEN** Compiler MUST报告节点 source identity 并拒绝生成 Program
- **AND** runtime MUST不把该节点当作成功或选择默认目标

#### Scenario: Fixed Target 编译 Camera operation

- **WHEN** Fixed Program 包含当前版本的 Camera operation
- **THEN** Fixed Target MUST提交与 Float32 同语义的相机命令并消费同一 Projection
- **AND** 本地镜头序列和效果状态 MUST不进入 deterministic state 或 Snapshot

### Requirement: CharacterSimulationPresentationRuntime 必须是相机 runtime 唯一边界

系统 MUST使用 CharacterSimulationPresentationRuntime 作为角色相机唯一公开编排边界。内部相机能力 MUST唯一拥有请求、序列、目标、响应、效果生命周期、时间上下文和 Rig Adapter 调用；相机构图计算、效果求值和平台输出 MUST具有分离的输入输出与所有权。协调器 MUST在 Body 和最终动画姿态发布后使用同一 CharacterBodyPresentationFrame 与显式目标采样推进相机。相机能力 MUST由 Factory 完整显式装配，不决定 Body 时钟策略，不自行注册 LateUpdate、另一个 Presentation target 或外部 resolver 驱动。

#### Scenario: Local Owner推进相机

- **WHEN** 唯一表现目标推进一帧
- **THEN** 协调器 MUST先完成本帧 Body 与最终动画，再让内部相机生成并应用唯一输出计划
- **AND** 默认跟随 MUST继续来自同一 visible Body，骨骼目标 MUST来自本帧最终姿态

#### Scenario: 无相机 Simulated Actor

- **WHEN** Factory 创建不拥有本地相机的完整模拟 Actor
- **THEN** MUST不创建 Camera Runtime、请求容器或相机求值状态
- **AND** Actor MUST继续使用其显式 Body 时钟策略

#### Scenario: 无Camera组合收到Camera命令

- **WHEN** 无相机 Actor 收到需要本地消费的相机命令
- **THEN** 唯一协调器 MUST报告明确配置错误
- **AND** MUST不搜索场景相机或创建默认相机能力

#### Scenario: 禁止双驱动

- **WHEN** 相机输出已由内部 Camera Runtime 驱动
- **THEN** Host、Network adapter、Timeline、Preview 和旧相机脚本 MUST不能再次推进或写入同一物理输出
- **AND** 一帧内 MUST只有一次正式相机输出更新

### Requirement: CameraBasisSnapshot 必须作为可采样事实暴露

系统 MUST暴露稳定 CameraBasisSnapshot，至少表达 planar forward、planar right、look direction、aim point、yaw 和 pitch。Snapshot MUST在本帧唯一相机输出完成后从实际活动输出发布，所有方向和角度 MUST属于同一帧与同一镜头；Shot 切换、blend 和 collision MUST不让 basis 继续引用未活动的 FreeLook。若 basis 被用于技能瞄准、突进、目标选择或角色 yaw，采样值 MUST固化为相应 Action、Motion 或 Gameplay fact，后续镜头变化 MUST不反写该事实。

#### Scenario: 射击动作采样相机方向

- **WHEN** 玩家启动射击动作
- **THEN** 逻辑 MUST采样最近一次有效的 CameraBasisSnapshot
- **AND** 射击方向 MUST保存为动作激活或等价 Gameplay fact

#### Scenario: 相机之后继续转动

- **WHEN** 技能已经固化 aim direction 后玩家继续转动相机
- **THEN** 已提交技能事实 MUST保持原采样方向
- **AND** 新镜头 basis MUST只供后续明确采样使用

#### Scenario: Shot 成为活动输出

- **WHEN** 镜头进入 Shot 或正在与 Shot 混合
- **THEN** basis MUST来自最终活动输出的同帧结果
- **AND** MUST不混合旧 FreeLook yaw 和新 Shot look direction

### Requirement: Cinemachine 必须是 CameraRigAdapter 实现细节

系统 MUST通过 `ICameraRigAdapter` 的正式实现 `CinemachineCameraRigAdapter` 将 `CameraFramePlan` 应用到 Unity 相机系统。ZZZ 原调用链中属于相机核心的构图、球面/轨道、阻尼、转场和效果计算 MUST允许由核心执行；原本属于 Cinemachine 的承载、组件计算、碰撞或镜头混合 MUST通过 Adapter 的明确平台能力执行。每项计算 MUST只有一个确定 owner，同一种阻尼、效果、碰撞或过渡不得在核心与 Cinemachine 重复执行。Adapter MUST不另行裁决业务请求、寻找目标或维护技能生命周期；Graph、Timeline、Program 与相机核心 MUST不依赖具体 Cinemachine 组件作为业务状态机。旧具体 Controller 的 FreeLook 专用公开合同 MUST迁入新的正式输出合同后删除。

#### Scenario: 默认序列输出到 Cinemachine

- **WHEN** `CameraFramePlan` 表达 follow point、aim point、FOV 和裁决后的 look delta
- **THEN** Adapter MUST按已确认的原职责分工应用计划并更新唯一输出
- **AND** 默认镜头是否活动 MUST来自相机 Runtime，Cinemachine MUST不再次作业务选择

#### Scenario: Shot 使用专用 virtual camera

- **WHEN** `CameraFramePlan` 表达需要专用 Shot 承载的镜头
- **THEN** Adapter MUST使用正式绑定建立或复用该承载，激活、退出和混合请求 MUST来自相机 Runtime
- **AND** 专用承载 MUST不注册独立更新或成为另一个 influence stack

#### Scenario: 核心已计算位置阻尼

- **WHEN** 原算法对应的核心阶段已完成本帧位置阻尼
- **THEN** 输出合同 MUST明确该阶段已完成，Cinemachine 承载 MUST不再执行等价阻尼
- **AND** 同一约束 MUST适用于旋转、过渡、碰撞与震动

### Requirement: Camera debug 必须解释状态和输出

系统 MUST发布只读相机诊断，说明活动序列、请求和效果、来源身份、producer/generation、动作关联、优先级、胜出/压制原因、真实混合进度与起终点、响应策略、目标来源、时间域、效果阶段、碰撞、basis 和最终输出。诊断 MUST来自同一正式运行结果，不在 UI 中再次求值或影响相机决策；源资源或 Projection 不匹配时 MUST明确报告过期。

#### Scenario: 排查技能后镜头残留

- **WHEN** 技能请求已经退休
- **THEN** debug MUST显示其已清理，或仍由哪项已配置退出规则持有短期退出状态
- **AND** 当前序列和每项效果 MUST能追踪到真实来源与 generation

#### Scenario: 排查 look 不响应

- **WHEN** 输入被采集但没有产生手动相机旋转
- **THEN** debug MUST显示输入值、实际响应权重和抑制来源
- **AND** MUST不把无输入、被抑制和缺少目标混为一个状态

### Requirement: Camera Timeline控制曲线必须作为typed Curve Channel编辑

相机 Sequence/Response 等 Clip 的 Timeline-local Weight、Ease In 与 Ease Out MUST继续通过显式 registered ChannelId 进入 Timeline Curve Editor，使用 ClipNormalized 时间域和 `[0,1]` 值域，并由该 Clip 的正式 Mutation 原子替换。共享 Camera Curve 与效果资源的曲线 MUST由各自真实 owner 保存，声明自身单位、时间和值域，不得强制套用 Clip 的归一化合同。Curve Editor、Catalog 与 Agent MUST只写作者数据；Runtime MUST只消费编译后的相机计划，不直接读取 Editor 曲线或形成第二套效果求值。

#### Scenario: 编辑Camera Ease In

- **WHEN** 作者在相机 Track 展开某个 Clip 自己的 Ease In channel 并移动 key
- **THEN** Editor MUST在 Clip 时间范围显示并提交完整曲线
- **AND** 后续相机编译与表现 MUST沿唯一链路消费修改

#### Scenario: Camera与Animation曲线同时展开

- **WHEN** Timeline 同时显示相机与动画 Track 的曲线分组
- **THEN** 两者 MUST复用同一 Curve Lane 交互与 frame geometry
- **AND** 每条曲线 MUST保持自己的 owner、ChannelId 和运行消费者

#### Scenario: Curve Editor尝试直接控制Cinemachine

- **WHEN** 作者修改相机 Weight、Ease 或共享效果曲线
- **THEN** Curve Editor MUST只修改对应作者 owner
- **AND** MUST不访问活动 Cinemachine 或写相机 Transform

## REMOVED Requirements

### Requirement: CameraStateResolver 必须使用有限状态仲裁

**Reason**：旧 requirement 把基础镜头固定为代码内的有限模式，不能完整表达原角色 Profile、状态序列与真实进入/退出行为。

**Migration**：将原 FreeLook、Aim、LockOn、ActionFocus 和 SkillCloseup 的正式引用迁入 Profile 序列与 Shot 资源；由“相机状态必须按正式序列裁决并真实混合”保留确定性仲裁、来源追踪和默认镜头要求，删除旧模式到硬编码 FOV 的执行映射。

### Requirement: Camera modifier 必须按有限顺序裁决相机表现意图

**Reason**：旧 requirement 禁止核心计算构图/轨道并将效果限制为意图，无法保留 ZZZ 的实际效果算法和组合阶段。

**Migration**：由“相机效果必须保留独立语义与原组合顺序”定义完整效果求值，由正式 Rig Adapter requirement 明确平台分工；删除旧无消费者 Cue 分支和通用 Custom 配置。

## ADDED Requirements

### Requirement: 相机状态必须按正式序列裁决并真实混合

相机 MUST从明确 Profile 取得默认序列及有限的已注册算法组合；没有活动覆盖请求时 MUST使用该正式默认配置，不要求 Graph 每帧重发，也不使用硬编码参数补齐缺失 Profile。请求 MUST根据已确认的优先级、权重、稳定顺序和生命周期规则确定活动序列。进入、退出、抢占、恢复和 Cut MUST实际作用于构图、位置、旋转、轨道与镜头参数，混合进度不得只作为诊断数字。需要从当前镜头进入时 MUST使用对应原规则指定的当前输出阶段，不能把临时震动误当成持久轨道状态。

#### Scenario: 默认 FreeLook

- **WHEN** 本帧没有活动序列覆盖请求
- **THEN** 相机 MUST执行 Profile 的正式默认跟随序列
- **AND** 默认序列 MUST不依赖额外 Gameplay producer 或旧 FreeLook 参数补齐

#### Scenario: 技能特写覆盖瞄准

- **WHEN** Aim 请求与具有更高有效优先级的 SkillCloseup 请求同时活动
- **THEN** 相机 MUST按正式规则选择 SkillCloseup 并实际混合到对应 Shot/Sequence
- **AND** debug MUST能追踪赢家及被压制请求的来源

#### Scenario: 混合过程中再次切换

- **WHEN** 一个镜头尚未进入完成便被新镜头接管
- **THEN** 相机 MUST按原中断规则选择 Cut、承接当前结果或等待原来源结束
- **AND** 位置、旋转、构图和 FOV MUST使用同一过渡规则，不跳回旧起点

### Requirement: 相机效果必须保留独立语义与原组合顺序

Override MUST替换其声明的轨道和构图字段；Zoom MUST按其规则控制 FOV；Stretch MUST按原语义控制半径、位置、俯仰/倾斜及回弹；Shake MUST保留方向、频率、幅度、随机/噪声和衰减；Shot MUST保留目标绑定、镜头资源、裁剪面与进出场。每项效果 MUST保留原时间、优先级、标签/覆盖、静音、叠加与退出语义，并在已确认的有限阶段顺序中求值。碰撞 MUST具有唯一明确阶段与物理上下文，震动 MUST不通过扰动角色或 Follow/LookAt 目标伪造。

#### Scenario: 命中帧震屏

- **WHEN** 已提交 Shake 事件到达表现层
- **THEN** 相机 MUST按对应资源在正式效果阶段计算震动，并保留事件身份与来源
- **AND** MUST不移动角色、骨骼或跟随目标来模拟震屏

#### Scenario: Zoom 与 SkillCloseup 同帧存在

- **WHEN** 当前 Shot 和 Zoom 都参与本帧相机
- **THEN** 两者 MUST按原覆盖/叠加规则得到唯一 FOV
- **AND** debug MUST显示每项贡献及最终结果

#### Scenario: Stretch 结束

- **WHEN** Stretch 进入回弹或退出阶段
- **THEN** 半径、偏移、角度及其关联镜头参数 MUST按原退出曲线恢复到当前有效构图
- **AND** MUST不直接恢复为启动时已失效的角色默认值

### Requirement: 相机生命周期必须区分请求实例与效果退出

相机 MUST使用正式 producer identity、generation、EventId 和明确动作关联追踪请求与效果。Publish、Replace、Retire、重复事件和循环 MUST服从已提交命令的 disposition；同一事件不得重复触发，不同 generation 不得互相清理。请求退休后是否立即切断、执行退出或保留效果尾段 MUST来自对应资源规则。默认配置、Runtime reset、Body reset、目标切换和销毁 MUST具有明确且统一的状态重置语义。

#### Scenario: 同一技能快速再次触发

- **WHEN** 旧 generation 的退出尚未结束，新 generation 已提交相机请求
- **THEN** 两者 MUST按原覆盖规则处理，旧 Retire MUST不删除新实例
- **AND** 当前来源与退出尾段 MUST分别可追踪

#### Scenario: 重复提交同一命中事件

- **WHEN** 同一正式 EventId 被再次路由到相机
- **THEN** 相机 MUST遵循已提交事件去重语义，不再新增一次震动
- **AND** 不同 EventId 的连续命中 MUST仍可按原叠加规则同时存在

### Requirement: 相机必须显式使用原时间语义

相机 MUST从调用方接收明确的表现帧时间、受缩放时间、未缩放时间和必要的动作采样锚点；每项原配置 MUST映射到已确认的时间域。相机不得隐式读取另一套全局时间、再次乘时间缩放，或把资源帧号当成当前 Simulation tick。延迟、进入、保持、退出、暂停、慢放、循环和第一帧触发 MUST使用一致的跨越与采样语义。

#### Scenario: 技能慢放与实时震动

- **WHEN** 动作时间被缩放而某项已确认配置要求不受该缩放影响
- **THEN** 两项行为 MUST分别消费自己明确的时间域
- **AND** MUST不因共享一个 delta 而改变原持续时间关系

#### Scenario: 短事件跨过一个表现帧

- **WHEN** 新提交的效果窗口短于本帧跨度
- **THEN** 相机 MUST按原事件采样规则处理首次作用与完成状态
- **AND** MUST不因先统一扣减寿命而无条件丢失首次作用

### Requirement: 构图与碰撞只能消费明确的目标和世界输入

相机 MUST通过正式绑定或已提交上下文取得角色、普通目标、Boss、多点构图、骨骼和世界点，以及本地物理场景与碰撞规则。原算法需要候选目标时 MUST从明确候选输入取得，仅决定镜头取景，不反向生成 Gameplay 锁定事实。角色默认跟随 MUST继续消费最终 visible Body；原相机阻尼 MUST作用于镜头结果，不重建 Body 插值、台阶修正或移动计划。缺少必要目标/世界能力 MUST按正式配置报告或执行已声明的请求失效规则，不搜索场景补齐。

#### Scenario: 角色和 Boss 同时取景

- **WHEN** 序列需要角色与 Boss 的正式构图目标
- **THEN** 相机 MUST使用同帧明确提供的目标与已确认取景算法
- **AND** MUST不移动角色或修改 Gameplay 目标来满足构图

#### Scenario: Preview 缺少物理场景

- **WHEN** 当前相机计划需要碰撞而 Preview 没有明确物理场景
- **THEN** 相机 MUST报告该能力不可用并阻止宣称完整预览
- **AND** MUST不创建假地面或关闭碰撞以掩盖缺失输入
