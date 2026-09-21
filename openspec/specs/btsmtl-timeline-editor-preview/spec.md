# btsmtl-timeline-editor-preview Specification

## Purpose

定义 ScenePlay 中 Timeline UI 的作者、选择和观察边界。Timeline UI 是正式运行链的窗口入口，不是第二个 Runtime、播放器、求值器或独立时钟。正式 Timeline Runtime、Ability、Character、Pose、Camera 和 World 继续由各自领域 owner 负责。

## Requirements

### Requirement: ScenePlay 必须拥有预览生命周期

ScenePlay 的正式 Session、Actor 和 RuntimeDebug owner MUST 统一拥有 Start、Pause、Resume、Stop、Ability 输入、Live Debug、Capture、History、Restore 和 Replay。TimelineEditorWindow 的 Session 菜单 MUST 先用 Profile 的 ContextId 精确定位 Composition SessionId，再在该 Session 内定位 Actor；窗口只提交请求，并消费正式 binding、状态和只读事实。

#### Scenario: 从编辑器开始一次 Ability 预览

- **WHEN** 作者在 TimelineEditorWindow 选择 Profile 并点击 Start Preview
- **THEN** ScenePlay MUST 加载声明的正式场景并启动唯一 Session/Actor
- **AND** Ability MUST 通过正式输入和请求入口启动
- **AND** Timeline UI MUST 绑定实际 playback identity 后开始观察

#### Scenario: 暂停与停止

- **WHEN** 作者点击 Pause、Resume 或 Stop
- **THEN** ScenePlay MUST 调用正式 Session/Presentation/Timeline 生命周期
- **AND** Timeline UI MUST 不自行推进时间、不清理角色状态、不创建新播放实例；Capture、History 和 Resume Live 只调用 RuntimeDebugSession

### Requirement: Timeline UI 不得拥有运行时执行状态

TimelineEditorWindow MUST 不拥有 evaluator、独立时钟、playback command source、TimelinePreviewSession、AnimationPreviewRuntime、Preview Player、隐藏 Action runtime 或独立 PlayableGraph。窗口本地只保存 authoring selection、view state、观察绑定和显示过滤。

#### Scenario: 打开同一 Timeline

- **WHEN** 作者在不同页面打开同一 TimelineData
- **THEN** 每个页面 MAY 保存自己的 selection 和观察绑定
- **AND** TimelineData MUST 不保存窗口时间、目标、generation、播放状态或 GUI 游标
- **AND** 页面关闭只撤销 interest，不得结束正式 Session，除非作者明确调用 ScenePlay Stop

### Requirement: Timeline 预览必须消费正式 Runtime 事实

Timeline UI MUST 只读取正式 Timeline Runtime、Ability lifecycle、Action playback、Pose 结果和 ScenePlay diagnostics 发布的 binding、playback identity、generation、content revision、active Clip、窗口、TreeClip 阶段、Motion/Cue 结果和 completion trace。UI MUST 不从 Animancer weight、当前 authoring 游标或场景对象推断运行事实。

#### Scenario: 当前 Ability 没有执行该 Timeline

- **WHEN** 正式 Session 的 playback summary 不包含当前 Timeline identity
- **THEN** UI MUST 显示未执行或未绑定
- **AND** MUST 不调用预览求值器、不重采样 TimelineData、不猜测其它 Actor

#### Scenario: 同一 Timeline 有多个播放实例

- **WHEN** 正式 Runtime 同时存在多个 playback identity
- **THEN** UI MUST 要求作者显式 Pin 或 Follow 一个实例
- **AND** 不得自动选择第一个实例或按名称匹配

### Requirement: Timeline 内容直接由正式 TimelineData 驱动

正式 Timeline Runtime MUST 直接准备和调度 TimelineData、Track、Clip、Section、Window、Motion、MotionWarp、Decision、Cue 和内容资源引用。轨道和 Clip 不得编译为 Character Program operation、Timeline IR 或窗口专用执行语言。Skill/Ability 调用只提交内容 identity、调用 identity、参数和生命周期请求。

#### Scenario: 跨 Clip 和循环边界

- **WHEN** 一次正式 Step 跨越多个 Clip 或循环边界
- **THEN** Timeline Runtime MUST 按稳定顺序处理尾段、整循环、头段、Enter、采样和 Exit
- **AND** MUST 将候选结果交给同一调用方的 Commit/Discard 边界

#### Scenario: Timeline 依赖缺失

- **WHEN** 内容、资源、TreeClip 图或外部领域服务缺失或 revision 不匹配
- **THEN** Prepare 或 playback 创建 MUST 精确失败
- **AND** MUST 不创建空 Ability、假 Actor、默认资源或 fallback 播放器

### Requirement: 动态 TreeClip 长度必须来自对应执行域的实例事实

Logic 与 Presentation 的 TreeClip MUST 支持显式 `TreeDecision` 结束来源，图内“结束片段”只结束当前 playback、generation 与 cycle 的调用实例。Presentation 的 `FrameBoundary` MUST 保留为显式固定区间模式。动态长度 MUST NOT 取作者 End，也 MUST NOT 反写作者资产；表现域退出 MUST NOT 修改 Gameplay 时钟或逻辑生命周期。

#### Scenario: 动态片段仍在执行

- **WHEN** 所观察实例尚无已接受的退出或销毁事实
- **THEN** 可视 End MUST 使用对应执行域的已提交游标；Presentation 使用表现时间，Logic 使用逻辑时间
- **AND** 不同 playback、generation 或 cycle MUST 独立保存实际长度

#### Scenario: 表现图主动结束片段

- **WHEN** 当前 Presentation TreeClip 的 OnEnable 或 Root 请求结束当前片段
- **THEN** 原表现驱动 MUST 在当前候选帧执行 OnDisable、移除活跃片段并回收持续输出
- **AND** 只有 Commit 后 MUST 保存退出；Discard MUST 保留此前已接受状态

#### Scenario: 回看退出之前的历史

- **WHEN** 历史位置尚未包含本次实例的退出事实
- **THEN** Clip MUST 显示 open 并截取到该历史位置的对应域游标
- **AND** MUST NOT 使用未来退出事件或当前作者结束时间

### Requirement: Timeline 私有状态必须由 Timeline Runtime 拥有

Timeline Runtime MUST 保存自己的 committed cursor、循环/Section 位置、活动 Clip、窗口阶段、TreeClip 调用关联、停止原因、generation、内容 revision 和恢复所需的正式私有状态。Character 核心负责整体快照校验、Step 接受/丢弃和最终安装；Timeline UI、Ability 和 Pose 不得复制可写 Timeline 状态。

#### Scenario: 候选被丢弃

- **WHEN** 当前 Step 或正式事务失败、取消或停止
- **THEN** Timeline MUST 丢弃本次 Pending 候选并保留上一份 committed 状态
- **AND** 不得先清空已提交游标、窗口或调用关联

#### Scenario: 恢复播放

- **WHEN** 正式 Session 恢复 Timeline 状态
- **THEN** 恢复 MUST 校验内容 revision、schema、generation、NumericTarget 和服务关联
- **AND** 不兼容时明确失败，不读取当前作者资产猜测旧状态

### Requirement: Timeline UI 只编辑正式作者数据

Timeline Editor MUST 通过 Timeline owner 的正式 Mutation、Validator 和 Undo 入口编辑 Action Track、AnimationClip Segment、Slot、Section、Window、Motion、MotionWarp、Decision、Cue 与 Timeline-local Curve。素材骨骼和注册表现曲线继续由 Unity Animation Window 及其正式 owner 编辑。

#### Scenario: 修改 Timeline 曲线

- **WHEN** 作者拖动 key 或修改曲线 Inspector 值
- **THEN** UI MUST 只生成一次正式 Mutation/Undo
- **AND** 修改后的内容 MUST 通过正式 Export/Prepare/Publish/Adopt 进入后续 ScenePlay
- **AND** 当前活动播放不得被窗口静默替换

### Requirement: 旧窗口预览路径必须删除

当前主线 MUST 不恢复 `TimelinePreviewSession`、`AnimationPreviewRuntime`、`TimelinePlayer`、Fact/Action/Query Fixture、独立 Motion evaluator、窗口时钟、独立 Scene Preview runtime 或独立 PlayableGraph。历史文档 MAY 记录这些路径作为迁移证据，但新代码和新规范不得引用它们作为入口。

#### Scenario: 搜索旧预览入口

- **WHEN** 检查 Timeline Editor、Pose Editor 和 ScenePlay 代码
- **THEN** 播放、暂停、重置和停止入口 MUST 指向 ScenePlay/正式 Runtime 合同
- **AND** MUST 不存在窗口级第二套执行路径或兼容别名

### Requirement: Timeline Preview 必须观察唯一正式动作与 Pose 运行

有限动作 Timeline Preview MUST只观察正式 Session 中由 Ability、RootTree、Timeline Runtime、原生 Pose Factory、资源、Slot、Source 和最终输出规则共同产生的结果。窗口 MUST不保存 session-local 动作状态、不创建 Action adapter、不执行 Gameplay 或树逻辑，也不读取活动角色私有状态；持续 Locomotion 仍由正式 Pose 运行链拥有。

#### Scenario: 在同一动画配置上预览有限动作
- **WHEN** 正式 Session 执行一个合法有限动作 Timeline
- **THEN** Preview MUST观察同一原生 Pose 实例产生的动作时间与 Slot 混合事实
- **AND** MUST不创建另一套动画执行器、临时角色 Program 或 Preview 专用动作状态

### Requirement: 预览接入必须以领域实际准备和采用事实为准

预览 MUST消费角色领域工厂提供的技能、Pose、Camera、Motion准备与采用事实，分别显示请求来源／版本、Pending／Ready／Missing／Invalid／Failed及精确原因，并显示当前actor真正采用的版本和实例。预览 MUST不重建Character Build／ProgramEpoch，不计算假全局版本，不实现Camera或Motion准备，不因作者保存或准备Ready就显示已采用。原独立作者预览的会话实现迁移由预览任务唯一负责；本任务只提供正式实例／输入／结果合同，不扩建窗口私有执行路径。

#### Scenario: Camera尚未采用新绑定
- **WHEN** Camera资源准备已Ready但当前actor仍使用旧BindingId
- **THEN** 预览 MUST明确显示当前实际BindingId和待采用状态，不显示整个角色已更新

### Requirement: 相机预览必须消费现有预览 owner 的正式输入

相机 MUST 向现有 Timeline Preview/ScenePlay owner 提供正式 Camera 资源、只读运行绑定、显式 Rig、目标/物理输入、Reset/替换和实际采用身份。预览消费的 Timeline 相机表达只有 TreeClip 相机 Node 与唯一效果轨道两类；MUST NOT 期待或兼容 CameraState/Response/Cue 触发型轨道及按效果类型拆分的四条效果轨道。会话、输入来源和历史重建继续由对应 owner 管理；相机 MUST 不创建独立 Preview 会话、第二角色运行链或自己的 seek 执行器。预览 MUST 不实现相机求值、不要求恢复角色全量 Build 或整包 Projection，也不得隐式搜索补齐输入。

#### Scenario: 预览需要碰撞环境

- **WHEN** 已启用碰撞的相机计划进入预览
- **THEN** owner MUST 提供明确物理输入和输出绑定
- **AND** 缺失时 MUST 报告不可用，不得静默按无碰撞运行

### Requirement: 相机历史重建必须走统一 owner

有状态镜头需要重新定位时，系统 MUST 通过现有 owner 的正式 Reset/历史重建合同重新生成状态；Timeline 游标移动不能直接改 Simulation 或插入第二更新循环。缺少重建能力时必须明确不可用，不得将相机预览临时接到旧 Controller。

#### Scenario: 重定位到有持续效果的时刻

- **WHEN** 预览 owner 支持该次正式重建
- **THEN** 相机 MUST 使用同一资源、请求和求值模块恢复对应状态
- **AND** MUST 不残留上一次时间位置的平滑或效果历史

### Requirement: Timeline 相机诊断必须读取实际运行快照

Timeline Live Debug MUST 读取正式相机快照中的来源身份、请求状态、响应、混合、效果、目标与输出，并导航到真实 TreeClip Node、唯一效果轨道 Clip 或资源。它 MUST 不根据游标位置伪造活动请求或重算另一份镜头结果。

实际采用的相机资源/内容版本、绑定实例/代际及失败原因 MUST 由 Camera 领域返回。预览 MUST 不把资源处理完成、技能编译完成、旧 Projection 或旧 Runtime DLL 的调用结果冒充新相机绑定已采用。

#### Scenario: 动作镜头被取消

- **WHEN** 相应请求已经进入正式退出
- **THEN** Live Debug MUST 显示真实退出状态与原因
- **AND** MUST 不因游标仍位于 Clip 范围而显示镜头继续生效

### Requirement: Timeline Editor必须直接使用Slate CutsceneEditor作为实际编辑表面

Timeline作者入口 MUST复用Slate CutsceneEditor已有时间尺、Track／Clip列表、选择、拖动、缩放、Curve和DopeSheet交互，但正式数据、identity、Mutation、Undo与资源引用始终由TimelineData和TimelineEditorSessionContext拥有。Slate运行播放器、Actor／Director组件树和Preview不得成为作者依赖；不得用另一套UI Toolkit或IMGUI时间轴复制相同编辑器。

#### Scenario: 从正式Skill Graph打开Skill Timeline

- **WHEN** 作者从Skill Graph调用点打开Timeline
- **THEN** 唯一TimelineEditorWindow MUST在嵌入Surface中显示正式Track、Clip、Section和identity
- **AND** MUST不创建独立Slate窗口、运行Actor或第二编辑表面

#### Scenario: Slate UI编辑Clip范围

- **WHEN** 作者移动或裁剪一个Clip
- **THEN** adapter MUST把一次完成手势转换成一次正式Timeline Mutation
- **AND** 提交后 MUST从Timeline owner刷新显示，不把proxy字段保存为业务数据

### Requirement: Slate Projection必须是Editor-only桥接而不是第二个正式数据源

Editor-only projection MUST只保存正式identity到Slate显示对象的临时映射和当前手势草稿。它不得序列化为业务资产、进入C#输出或提供运行数据。关闭、切换owner或revision失效时必须解除回调并释放临时状态，正式TimelineData保持不变。

#### Scenario: 关闭Slate窗口

- **WHEN** Surface被关闭或切换到其它Timeline
- **THEN** adapter MUST释放临时映射、选择和回调
- **AND** MUST不生成Cutscene资产、可运行GameObject或回写未提交草稿

#### Scenario: Slate插件不可用

- **WHEN** CutsceneEditor无法加载
- **THEN** Timeline UI MUST显示明确Unavailable原因
- **AND** MUST不切换到仿制界面、默认Timeline或替代数据源

### Requirement: Timeline Editor必须明确Slate临时写入与BTSMTL正式写入边界

Slate字段变化 MUST只可作为单次手势草稿；字段输入、菜单、曲线和多Clip操作必须在提交边界形成一次完整Mutation、业务校验和Undo。选择、滚动、缩放、游标与折叠不建立作者事务。取消、异常或owner revision变化必须丢弃草稿，不得留下半成品TimelineData或第二套Undo。

#### Scenario: 一次Slate拖动提交

- **WHEN** 作者完成一次Clip拖动
- **THEN** adapter MUST只提交一次正式Mutation
- **AND** 拖动中间帧、空手势和派生显示值 MUST不写回正式数据

#### Scenario: Pointer Cancel或窗口关闭

- **WHEN** 手势取消、窗口关闭或projection过期
- **THEN** 未提交草稿 MUST被丢弃并从owner刷新
- **AND** MUST不修改已有Track、Clip或曲线

### Requirement: Timeline新增Track与Clip必须使用正式typed authoring contract

新增菜单 MUST来自当前owner的TimelineContractCatalog和Track允许的Clip kind，并通过正式AddTrack／AddClip／AddSection或等价typed Mutation创建identity、关系、字段和资源引用。显示名、GUI整数ID和Slate对象不得成为authoring identity。取消或校验失败不得留下半成品；合法空Track可以保存。

#### Scenario: 新增合法Track

- **WHEN** 作者选择当前owner支持的Track contract
- **THEN** 系统 MUST创建正式identity并进入owner revision和Undo
- **AND** 提交后 MUST从owner重建显示

#### Scenario: 新增不允许的Clip

- **WHEN** Track不允许所选Clip kind或必需binding缺失
- **THEN** 菜单或Mutation MUST拒绝创建
- **AND** MUST不生成默认资源、无效identity或fallback内容

### Requirement: Timeline预览联动必须消费领域准备和实际采用报告

Timeline UI MUST只消费技能、Pose、Camera、Motion和Session owner发布的请求版本、准备状态、实际采用版本／实例及失败原因。Ready不等于Adopted，UI不得按作者hash、保存成功或旧产物推断运行状态，也不得代替领域owner执行准备或重建。

#### Scenario: 就绪不等于实际采用

- **WHEN** 新内容已Ready而Actor仍使用旧binding
- **THEN** UI MUST同时显示候选与实际采用identity
- **AND** 晚到的旧请求结果 MUST不覆盖当前generation

### Requirement: Timeline运动源与局部作者曲线必须分开

MotionCurve源及其源区间和播放映射 MUST由资源owner拥有；Timeline只保存明确源引用、使用配置以及Weight、Ease、Warp progress等真正局部曲线。源XYZ／Yaw可以只读显示并导航到真实owner，但不得复制为Timeline可写channel或在UI重写采样公式。

#### Scenario: 编辑一次运动使用

- **WHEN** 作者修改源引用、使用区间或Timeline局部曲线
- **THEN** Timeline MUST只修改本次使用和局部曲线
- **AND** 共享源关键帧与映射只能由其正式owner修改

### Requirement: Timeline必须直接复用现成Slate编辑功能

正式Timeline MUST在Slate原绘制和事件处理主体中接入TimelineData与typed命令，不得通过临时GameObject／Cutscene／Director／Track／ActionClip组件树或另一套Surface复刻功能。解除Slate运行对象依赖时应修改原函数的数据访问和必要参数，保留选择、手势、Curve与DopeSheet算法。

#### Scenario: 正式合法操作不受Slate对象规则阻止

- **WHEN** Timeline合同允许选择、拖动或新增内容
- **THEN** 原Slate交互 MUST按正式Capability和重叠规则执行
- **AND** MUST不因缺少Actor、Director或可运行Cutscene而拒绝

### Requirement: Timeline作者输入必须遵守两个显式C#操作边界

Timeline MUST只消费公共C# authoring的导出代码与创建资产两个显式操作。人工编辑和Undo只修改TimelineData；导出不创建资产，代码编译不自动创建资产，创建资产不自动Play或准备领域。不得新增Timeline专用MCP、Document同步、源码Undo或兼容JSON路径。

#### Scenario: 人工编辑与源码编译

- **WHEN** 作者修改Timeline或编译导出代码
- **THEN** 两者 MUST保持独立且都不自动触发另一个操作
- **AND** 只有显式公共入口可以写对应代码或资产范围

### Requirement: Timeline UI配置必须直接使用共同强类型入口

Timeline UI与C#创建代码 MUST调用同一Timeline typed配置入口，覆盖Track、Clip、Section、曲线和引用约束。UI不得通过JObject、显示字段镜像、SerializedProperty任意写入或第二DTO转发业务配置；入口尚未支持的字段必须明确未完成。

#### Scenario: 强类型合同接通

- **WHEN** 作者或创建代码提交合法typed配置
- **THEN** 相同Timeline规则 MUST完成校验、Mutation和保存
- **AND** UI与代码路径 MUST不维护不同默认值或字段映射

### Requirement: Timeline完整代码输出必须保留正式内容与生成边界

Timeline导出 MUST读取正式Track、Clip、Section、TreeClip、外部binding、全部typed配置和完整曲线，并使用正式API重建声明范围。曲线必须保留key、tangent、weight、WeightedMode和wrap；范围外资源保持精确外部引用，窗口选择、滚动、运行Session、编辑游标和Slate草稿不得进入输出。

#### Scenario: 完整曲线与引用重建

- **WHEN** 显式操作导出并创建含曲线、TreeClip和共享资源的Timeline
- **THEN** 业务顺序、identity、曲线语义和根挂接 MUST保持
- **AND** MUST不依赖旧生成子资产、显示降采样或全项目扫描

### Requirement: Timeline必须与共享预览区完成跨窗口联动

Timeline和Graph Shell MUST通过正式调用路径、playback identity、generation与内容版本互相导航；Timeline编辑游标不得执行角色。切页、折叠或关闭Timeline不停止Session，运行overlay与编辑视图状态分别保存。

#### Scenario: 技能产生多个Timeline调用

- **WHEN** 当前ActionInstance存在多个合法Timeline调用
- **THEN** UI MUST按完整调用路径和generation要求作者选择或Follow
- **AND** MUST不按名称或列表首项猜测

#### Scenario: 编辑后返回预览

- **WHEN** 作者修改Timeline并返回共享预览区
- **THEN** UI MUST显示待准备、待采用、已采用或失败状态
- **AND** MUST不静默替换正在运行的内容

#### Scenario: 独立内容预览

- **WHEN** shared Timeline没有合法非Skill调用方或明确技能来源
- **THEN** 系统 MUST保留作者编辑并说明缺项
- **AND** MUST不伪造Actor、Skill或Timeline播放器

### Requirement: Timeline布局必须统一计算并适应窗口尺寸

嵌入Surface MUST用一份布局结果驱动背景、裁剪、绘制和命中。左侧Track与时间尺、Clip、Curve共用行高和垂直滚动；选中属性复用Unity Inspector，不新增Timeline内部右栏。窄窗口可折叠次要文字，但控件、标尺和命中不得重叠。

#### Scenario: 窄窗口与曲线展开

- **WHEN** 窗口缩小或作者展开曲线
- **THEN** Track、Clip、Curve和命中区域 MUST保持对齐
- **AND** 隐藏控件 MUST不保留空白或重复坐标转换

### Requirement: Timeline必须使用正式帧率统一编辑时间

Surface MUST消费Timeline Session的正式FrameRate，统一像素、整数作者帧和Slate秒之间的转换；标尺、边界和关键帧编辑以作者帧为主，但作者帧不自动等于Logic Tick。未编辑数据不得因打开窗口被整体量化。

#### Scenario: 编辑一帧

- **WHEN** 作者把Clip起点从第12帧移到第13帧
- **THEN** 草稿、属性和正式StartFrame MUST一致为13
- **AND** 一次Undo MUST恢复为12

#### Scenario: Curve时间换算

- **WHEN** 作者编辑Timeline-local曲线key
- **THEN** adapter MUST按正式descriptor转换时间并保留完整切线语义
- **AND** MUST不在UI重造外部源曲线采样公式

#### Scenario: 显示全部内容

- **WHEN** 作者选择显示全部
- **THEN** 视窗 MUST来自真实内容范围
- **AND** 空文档显示不得创建可保存的虚假长度

### Requirement: Timeline刷新必须保留有效作者状态

选择、展开、通道、编辑帧、缩放、滚动和搜索 MUST按正式identity保存。提交、Undo／Redo与外部更新只清理失效状态，不自动选择首个Clip；普通重绘不得创建Undo、重建全部projection或清空合法文本。窗口关闭只释放所属缓存和订阅。

#### Scenario: 修改后继续编辑

- **WHEN** 作者在缩放和曲线展开状态下提交修改
- **THEN** 仍有效的选择、展开和视野 MUST保持
- **AND** 已删除对象的状态 MUST精确清理而不改选其它内容

### Requirement: 编辑控件不能冒充真实角色预览

编辑游标、正式运行标记和Capture历史位置 MUST分开。Timeline按钮、快捷键、EditorUpdate和delayCall不得调用Slate Play／Sample／Stop或写Runtime状态；导航可以定位Graph Shell，但启动、暂停和停止只能走ScenePlay。

#### Scenario: 没有运行绑定

- **WHEN** 作者独立打开shared Timeline
- **THEN** 正式内容 MUST完整可编辑
- **AND** MUST不创建本地播放器、猜测Actor或把游标显示成运行结果

#### Scenario: 运行时继续编辑

- **WHEN** 作者在ScenePlay中修改Timeline
- **THEN** 修改 MUST走同一Mutation／Undo，运行overlay只读正式事实
- **AND** 是否采用新版本 MUST由领域owner报告

### Requirement: 临时投影必须正确释放且没有无关编辑入口

Surface MUST释放选择、回调、GUI capture和临时资源，不删除正式资产。嵌入路径必须移除Actor／Director／Render等无业务入口和未映射命令，保留正式Camera Track。显示优化不得改写key，无曲线内容不得创建空参数面板。

#### Scenario: 关闭与重新打开

- **WHEN** 作者保存后关闭并重新打开Timeline
- **THEN** 正式identity、资源和时间范围 MUST保持且旧临时状态已释放
- **AND** MUST无新增序列化、GUI或生命周期错误

### Requirement: Corin AttackProperty必须经TreeClip正式节点消费

外部AttackProperty MUST不得成为Timeline Runtime直接输入。主控必须把已确认事件时间转换为TreeClip内的正式Gameplay节点，由节点所属的GameplayEffect／Ability owner提交碰撞与属性payload。Timeline只观察正式TreeClip／节点输出及playback／trace，不定义ActionCue轨道、不包装第二套事件，也不解析Dump或建立第二时钟。

#### Scenario: 使用攻击属性配置打击帧

- **WHEN** 作者把AttackProperty时间点转换为TreeClip内的Gameplay节点或正式Ability打击帧
- **THEN** Timeline MUST按声明的Logic／Presentation执行域消费正式内容
- **AND** UI MUST不从原始Dump重算运行结果
