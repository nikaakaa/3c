## MODIFIED Requirements

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标

ScenePlay MUST 作为 Timeline/Ability 预览目标的唯一正式入口，并由场景中声明的 Character/Presentation owner 提供实际 Actor binding。正式 Actor MUST 沿 CharacterPipelineDefinition 的正式 Profile、原生 Pose 图实例、Rig 与有限 Action producer source binding 取得 Action Playback Input 和 AnimationSlot，并使用正式 Animancer/Presentation binding。Timeline UI MUST 不直接创建持续 Locomotion、Pose Fact 或 Query fixture；系统 MUST 不使用 TimelinePlayer、场景搜索、fallback target、Definition 内联 Presentation 或第二份动画拓扑配置作为预览目标。

#### Scenario: 选择预览目标

- **WHEN** 用户在 Timeline 编辑器 target field 选择场景对象
- **THEN** 可接受对象 MUST是 `TimelinePreviewTarget`
- **AND** 当前角色管线目标 MUST由 `CharacterPipelineHost` 实现
- **AND** `CharacterPipelineHost` MUST使用 Definition 引用的正式 CharacterAnimationPresentationProfile 与匹配 动画绑定
- **AND** `CharacterPipelineHost` MUST使用正式 `AnimancerComponent` 应用动画预览

#### Scenario: 未选择预览目标

- **WHEN** Timeline 编辑器没有有效 `TimelinePreviewTarget`
- **THEN** 用户 MAY继续编辑 Timeline 数据
- **AND** 播放、暂停、速度和可应用预览 MUST处于禁用状态
- **AND** 系统 MUST不自动查找场景中的 Host 或 TimelinePlayer


### Requirement: Timeline UI 必须不拥有动画生命周期状态

Timeline 页面 MUST 只拥有作者选择、ScenePlay 观察 binding、显示过滤和正式 Mutation/Undo。ActionInstance、playback generation、Action command、AnimationSlot、原生 Pose 图实例、Animancer source backend 与 snapshot MUST 由正式 ScenePlay/Session/Actor owner 拥有。Timeline UI MUST 不读取并复制一份可写 Action lifecycle，不与其它窗口建立第二 command batch/state，也 MUST 不把 lifecycle、Slot、Player 或 Pose Graph 状态写入 Timeline asset。

#### Scenario: 两个 Preview 窗口

- **WHEN** 两个窗口预览同一 Timeline
- **THEN** 两个session MUST拥有独立playback generation、queue、channel lifecycle、Player、source与原生Pose图实例 workspace

#### Scenario: 两个 Preview session 绑定同一物理目标

- **WHEN** 两个 Preview session 尝试同时绑定同一个 CharacterPipelineHost 与 AnimancerComponent
- **THEN** 目标 MUST明确拒绝第二个 session
- **AND** 系统 MUST不让两个 session 共享、重复推进或竞争同一 Animancer Graph 输出
- **AND** 两个页面 MAY通过不同 Preview target 分别建立完整动画预览

#### Scenario: 切换 target

- **WHEN** session 切换 Preview target
- **THEN** 旧target queue、channel lifecycle、Player、source与原生Pose图实例 workspace MUST清理
- **AND** 新 target MUST使用新 session identity

#### Scenario: Dispose

- **WHEN** Preview stop 或 dispose
- **THEN** pending commands、每channel Lifecycle、Player、source、原生Pose图实例 workspace与native state MUST释放
- **AND** Timeline asset MUST不保存 runtime state


### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

Timeline Editor MUST显示 TreeClip的 Decision/Commit阶段、inline/shared ownership和 Blackboard输出摘要。Authoring Preview MUST NOT执行 TreeClip、技能执行操作、SimulationKernel、Action、Blackboard、GameplayEffect、Motion 或 WorldSolver。TreeClip 的真实 Decision/Commit、输出与终止事实 MUST只由正式运行 Session产生，并通过 Live Debug显示。系统 MUST NOT创建 Preview Simulation Session、临时 `CharacterGraphContext`、`TimelineRunningTree` clone、写入 authoring默认值或形成第二套 TreeClip执行语义。

#### Scenario: Authoring Preview 打开含 TreeClip 的 Timeline

- **WHEN** 用户选择 Authoring Preview
- **THEN** TimelineEditor MUST使用显式 preview target、preview time 与独立 animation lifecycle
- **AND** MUST不创建 Simulation Session、输入、logic Tick、Action target或WorldSolver
- **AND** UI MUST不把结果标记为真实 gameplay runtime

#### Scenario: Preview target 缺少正式上下文

- **WHEN** 作者打开含 TreeClip 的 Timeline 但没有绑定完整 preview target
- **THEN** Timeline Editor MUST 继续显示 Clip、阶段、Graph 和声明摘要
- **AND** Preview MUST 不执行 TreeClip
- **AND** 系统 MUST NOT 创建 fallback context 或解释器路径

#### Scenario: 只预览动画资源

- **WHEN** 作者只请求纯表现动画采样且不执行 TreeClip Gameplay
- **THEN** Timeline Editor MAY 使用 动画实例绑定 采样表现资源
- **AND** MUST 不产生 Motion、Window、Blackboard、Action 或 GameplayEffect 事实


### Requirement: Timeline、Track 和 Clip 必须拥有稳定 authoring identity

`TimelineData`、每个 Track 和每个 Clip MUST 持有稳定 authoring identity。authoring 重排 MUST 保持 identity，复制 Track/Clip MUST 生成新 identity，Timeline内容记录、技能调用、动画播放源与运行观察 MUST保留对应 source identity。TrackIndex 和 ClipIndex MUST NOT 作为 Debug Source Map 的 source identity。

#### Scenario: 重排 Track

- **WHEN** 作者调整 Timeline Track 顺序
- **THEN** Track 和其 Clip authoring identity MUST 保持
- **AND** runtime debug source mapping MUST 不因 index 变化指向其它 Track

#### Scenario: 复制 Clip

- **WHEN** 作者复制一个 Clip
- **THEN** 新 Clip MUST 获得新 authoring identity
- **AND** 原 Clip identity MUST 保持

#### Scenario: 编译 Timeline

- **WHEN** 正式Timeline内容导出与运行绑定读取 TimelineData 与 动画绑定 producer
- **THEN** Timeline、Track 和 Clip authoring identity MUST进入正式 Source Map
- **AND** runtime activation、EventId、cycle 和 playback generation MUST独立生成


### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

有限Action Timeline Authoring Preview MUST把当前Track/Clip时间降低为正式Action Selection与Parameter page，并通过session-local Action command inbox执行匹配动画绑定的Action Playback Input、AnimationSlot、Transition Routing、Player和`CharacterPresentationPosePlan`。持续Locomotion Pose source、PoseStateMachine与Transition Rule预览 MUST属于Pose Graph Workspace，Timeline Preview不得伪造BaseLocomotion Timeline或Presentation Fact。具备正式Body与PhysicsScene上下文时 MAY执行FootPlacement，否则 MUST标记world-aware阶段Unavailable。Preview MUST不创建隐藏素材同步节点、固定per-slot Stack、隐藏Inertialization、简化PoseGraph、Animancer direct Play、假Foot Physics或自动全局平滑。

#### Scenario: 当前时间采样

- **WHEN** 作者把Preview游标移动到Attack clip中间
- **THEN** Preview MUST生成对应Attack Selection并送入FullBodyAction Action Playback Input与AnimationSlot
- **AND** 最终路径 MUST与正式原生Pose图实例一致

#### Scenario: 尝试预览Walk到Run

- **WHEN** 作者需要预览Locomotion PoseState从Walk到Run
- **THEN** Timeline Editor MUST导航到Pose Graph Workspace
- **AND** MUST不创建临时Walk/Run Timeline preview command

#### Scenario: 非连续seek

- **WHEN** 作者从一个producer非连续seek到另一个producer
- **THEN** AnimationSlot MUST按正式Action seek/reset policy重建source usage
- **AND** 连接Inertialization时 MUST按正式seek/reset policy处理history与residual
- **AND** 连接BlendStack时 MUST按正式node reset/seek policy处理而不创建额外fade

#### Scenario: Preview非连续拖动时间

- **WHEN** 作者把预览时间从一个不连续位置跳到另一个位置
- **THEN** Preview MUST重置Inertialization history
- **AND** MUST不把seek解释为可惯性化的连续切换


### Requirement: Curve Key编辑必须无损且原子

Curve Lane MUST支持单选、Shift追加、框选、双击或右键新增、一个或多个key拖动、Delete或右键删除、复制粘贴、数值Inspector以及Auto、Clamped Auto、Linear、Constant、Free和Weighted tangent编辑。横轴 MUST通过descriptor在Timeline frame与curve local time之间映射，并按整数Timeline frame吸附；纵轴 MUST按typed value domain处理。一次手势或Inspector提交 MUST只修改本地完整curve草稿并通过descriptor MutationAdapter生成一个Undo事务。Pointer Cancel MUST丢弃草稿；Pointer Up或意外Capture Out MUST提交最后草稿。提交后 MUST重新读取owner并刷新Timeline、Inspector、领域validation、动画绑定 stale状态和可用Authoring Preview。

Curve mutation MUST原子保存pre/post wrap mode及每个key的time、value、in/out tangent、in/out weight和WeightedMode。Curve key不获得持久AuthoringId；Editor MAY在当前owner revision内使用临时key index选择，C#作者API与持久作者代码 MUST以`OwnerAuthoringId + ChannelId + Full Curve`替换完整channel，不得按key index跨revision修改。

#### Scenario: 拖动多个curve key

- **WHEN** 作者框选多个key并拖动
- **THEN** pointer capture期间 MUST只更新本地curve草稿
- **AND** 所有key MUST按相同Timeline frame delta和值delta移动并保持合法顺序
- **AND** 释放时 MUST只产生一个Undo事务

#### Scenario: 精确编辑weighted tangent

- **WHEN** 作者在Inspector修改一个key的in/out tangent、weight与WeightedMode
- **THEN** MutationAdapter MUST原子保存完整Keyframe字段
- **AND** 未修改的key与wrap mode MUST无损保留

#### Scenario: 源运动曲线不进入Timeline lane

- **WHEN** 作者试图把 RootMotionCurveAsset 源曲线粘贴到 Timeline Weight channel
- **THEN** Editor MUST拒绝该操作并说明源 owner 不属于 Timeline-local channel
- **AND** MUST不创建曲线副本或部分写入

#### Scenario: 外部修改使key选择过期

- **WHEN** owner curve revision在编辑手势外被C#作者API或其它正式入口替换
- **THEN** Editor MUST使临时key选择失效并重新读取完整curve
- **AND** MUST不按旧key index写入新revision

### Requirement: Curve Editor必须保持领域运行链唯一

Timeline Curve Editor MUST只提供Timeline-local作者投影与正式mutation，不得创建`GenericTimelineCurveRuntime`。Animation Segment Weight/Ease MUST继续进入Action Presentation计划；MotionCurve和MotionWarp曲线 MUST作为直接Timeline内容及正式运动源绑定交给唯一Timeline／Motion Runtime，不生成IR或Clip operation；Camera曲线 MUST由Camera领域准备为只读运行绑定并由原求解链消费。AnimationClip注册表现Curve MUST由Clip Curve catalog、Animation Window入口与Character 动画实例绑定链拥有，MUST不经过Timeline Curve MutationAdapter。RootMotionCurveAsset、导入AnimationClip骨骼/BlendShape/属性曲线和没有正式consumer的任意Float Curve MUST不进入Timeline Curve Channel Catalog。

#### Scenario: 编辑MotionWarp progress

- **WHEN** 作者修改MotionWarp Position Progress channel
- **THEN** Timeline只通过MotionWarpClip正式mutation保存curve
- **AND** 后续Compiler MUST沿既有MotionWarp semantic operation编译

#### Scenario: 请求Clip注册Curve

- **WHEN** Timeline Curve Editor收到`presentation.locomotion-phase`或`presentation.foot-placement-weight`
- **THEN** Catalog MUST拒绝该channel并提供Open Animation Clip导航
- **AND** MUST不在Segment或Timeline创建Curve副本


## ADDED Requirements

### Requirement: Timeline预览必须复用原生Pose实现

有限动作 Timeline Preview MUST继续使用自己的 session-local 动作状态与正式 Action adapter，绑定同一原生 Pose Factory、资源、Slot、Source 和最终输出规则。它 MUST不要求旧角色 Program／Pose Image，不执行 Gameplay 或树逻辑，也不读取活动角色私有状态；持续 Locomotion 预览继续归 Pose 入口。

#### Scenario: 在同一动画配置上预览有限动作
- **WHEN** 作者预览一个合法有限动作 Timeline
- **THEN** 预览 MUST通过动作请求进入原生 Pose 实例，保留动作时间与 Slot 混合语义
- **AND** MUST不创建另一套动画执行器或生成临时角色 Program

### Requirement: 预览接入必须以领域实际准备和采用事实为准

预览 MUST消费角色领域工厂提供的技能、Pose、Camera、Motion准备与采用事实，分别显示请求来源／版本、Pending／Ready／Missing／Invalid／Failed及精确原因，并显示当前actor真正采用的版本和实例。预览 MUST不重建Character Build／ProgramEpoch，不计算假全局版本，不实现Camera或Motion准备，不因作者保存或准备Ready就显示已采用。原独立作者预览的会话实现迁移由预览任务唯一负责；本任务只提供正式实例／输入／结果合同，不扩建窗口私有执行路径。

#### Scenario: Camera尚未采用新绑定
- **WHEN** Camera资源准备已Ready但当前actor仍使用旧BindingId
- **THEN** 预览 MUST明确显示当前实际BindingId和待采用状态，不显示整个角色已更新
