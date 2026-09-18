# character-camera-authoring Specification

## Purpose
定义 Profile、Sequence、Effect 和 Curve 的唯一作者 owner，以及 TreeClip Node 与唯一 Timeline 效果轨道向 Camera Runtime 提交请求的正式边界。

## Requirements

### Requirement: 相机配置必须由唯一正式 owner 装配

Character Definition MUST 装配正式 Camera Profile；Profile MUST 注册 Sequence、效果、曲线和全局输入/平滑/环境配置；各参数只能由一个正式 owner 定义。所有有效字段 MUST 有明确单位、数值域和运行消费者。重复轨道、确认无消费者字段和旧控制器配置 MUST 在迁移后完整删除，不保留兼容读法。

#### Scenario: 作者调整默认轨道

- **WHEN** 作者修改默认 Sequence 的轨道
- **THEN** 正式相机资源转换与运行绑定 MUST 消费该唯一配置
- **AND** MUST 不存在另一份可编辑轨道覆盖其结果

### Requirement: 相机作者编辑必须复用现有领域 API

Graph、Timeline、资源 Inspector 与 C# 作者入口 MUST 调用相同正式业务 API，保持稳定身份、引用、顺序、Undo/Redo 和保存语义。相机作者能力 MUST 不恢复旧 Agent Document、目录包、同步器或第二份字段/校验模型。OnInspectorGUI MUST 不执行编译、批量导入或运行求值等重操作。

#### Scenario: 编辑相机资源

- **WHEN** 作者改变正式 Profile 或 Effect 参数
- **THEN** 编辑 MUST 只修改真实 owner 并产生正确 dirty/Undo 状态
- **AND** MUST 不自动导出 C# 或直接修改正在运行的相机

### Requirement: Timeline 相机表达必须按触发与排布划界

Timeline 中的相机表达 MUST 按工作性质划分：绑定动作实例的一次性触发 MUST 由 TreeClip 特殊 Node 表达；效果资产的持续窗口排布 MUST 由 Timeline 效果轨道表达。同一请求 MUST 不在 Node 与轨道间保留两份并行表达；确认无消费者的轨道型表达 MUST 在迁移后删除。轨道型表达的存在意义是同一时间坐标系下的窗口对齐与资源排布，MUST NOT 将持续窗口内嵌进 Node 使作者失去时间轴可视化。

#### Scenario: 动作命中帧触发震屏

- **WHEN** 动作在指定帧需要一次震屏
- **THEN** 作者 MUST 使用 TreeClip 相机 Node 表达该触发
- **AND** MUST 不在 Timeline 上新建一条触发型相机轨道

#### Scenario: 排布持续推镜窗口

- **WHEN** 作者需要让推镜效果持续一个窗口并与动画节奏对齐
- **THEN** 作者 MUST 在 Timeline 效果轨道上排布资源窗口与曲线
- **AND** 窗口生命周期 MUST 跟随时间轴时间而不是动作实例，动作取消后窗口 MUST 继续按正式合同自然退出

### Requirement: 动作相机请求必须统一由 TreeClip 特殊 Node 表达

技能动作中的 Camera State、Effect、Response、Target 请求 MUST 通过正式 TreeClip 内的相机特殊 Node 表达。Node MUST 只提交带稳定 ActionContext、来源身份、ResourceId 和生命周期的 typed Camera request，不得直接写 Camera、Cinemachine 或虚拟相机。动作链 MUST 不再为同一请求维护 CameraStateTrack、CameraResponseTrack、CameraCueTrack、CameraCueClip 或 `ActionCueClip(CueType: Camera)` 的并行表达。瞬态请求统一由 Node 表达是通用合同：音效、特效等其它域的帧触发表达 MUST 按同一边界迁往 Node，其宿主轨道在迁移完成后整体删除，MUST NOT 新增触发型轨道用法。

#### Scenario: 动作在指定时点触发镜头效果

- **WHEN** TreeClip 执行到一个带正式相机特殊 Node 的动作时点
- **THEN** Node MUST 向 Camera Runtime 提交对应的 Camera request
- **AND** Camera Runtime MUST 继续负责效果求值、生命周期、碰撞和最终输出
- **AND** Node MUST NOT 直接修改 Camera 或 Cinemachine 状态

#### Scenario: 动作相机请求被取消或循环

- **WHEN** TreeClip 发生循环、取消、自然结束或 seek/replay
- **THEN** 相机请求 MUST 使用稳定的 action/cycle/event 身份只触发一次并按正式退出合同处理
- **AND** MUST 不因每个逻辑 tick 重复提交同一请求

### Requirement: 相机持续窗口必须收敛为单一效果轨道

Timeline 中相机效果的持续窗口 MUST 由唯一的效果轨道表达；该轨道的 Clip MUST 只拥有窗口、效果资源引用和 Weight/EaseIn/EaseOut 曲线，效果类型 MUST 由引用的资源自描述，MUST NOT 按效果类型拆分多条结构重复的轨道。Shot 的真实 prefab 要求、裁剪面接管合同 MUST 在合并后继续生效。迁移完成后 CameraOverrideTrack、CameraZoomTrack、CameraStretchTrack、CameraShotTrack 及其按类型拆分的曲线 channel MUST 删除，MUST NOT 保留兼容读法。

#### Scenario: 作者排布不同类型效果窗口

- **WHEN** 作者在同一时间轴排布推镜、切机位等不同效果
- **THEN** 作者 MUST 在同一条效果轨道上通过引用不同效果资源表达类型差异
- **AND** 效果类型与持续窗口 MUST 仍可在时间轴上直接观察与拖拽对齐

### Requirement: 相机曲线与依赖必须具有真实导航

动作相机请求的本地权重及 Ease 曲线 MUST 由对应 TreeClip 特殊 Node 或正式共享资源中的唯一 owner 拥有，并进入现有 typed Curve Channel；共享 Effect 曲线 MUST 保留资源 owner，引用处只能导航，不复制为另一可编辑来源。资源选择 MUST 使用正式引用与支持范围，缺失或不支持能力必须显示原因。

#### Scenario: TreeClip Node 引用共享效果曲线

- **WHEN** 作者从 TreeClip Node 查看共享曲线
- **THEN** 编辑器 MUST 导航到实际资源 owner
- **AND** MUST 不在 Clip 中保存第二份曲线

### Requirement: 作者必须能区分配置保存和实际绑定采用

作者入口 MUST 分别显示必要资源处理、技能编译和相机实际绑定采用状态，身份由 Camera 领域返回，不再依赖角色总 Build/整包 Projection。Runtime/Preview MUST 消费正式相机资源和只读运行绑定，不逐字段热读作者对象形成混合版本，也不能以配置保存或旧 DLL 调用成功证明新绑定已经采用。

#### Scenario: 保存 Profile 后尚未采用新绑定

- **WHEN** 配置已保存而 Camera 返回的实际绑定仍是原版本
- **THEN** 作者界面 MUST 明确显示配置与运行版本的关系
- **AND** MUST 不宣称新参数已经作用于当前画面

### Requirement: 同批 Corin TreeClip 与生成源码必须由单一任务写入

Camera 任务 MUST 拥有相机资源、Builder/payload、运行绑定和 TreeClip 相机请求合同，并提供精确 Node 映射。本批 Corin TreeClip 资产及生成 C# MUST 由曲线迁移任务统一消费该映射后写入，不允许双方独立生成或重建同一资产。角色装配由领域运行时迁移任务调用 Camera 提供的绑定，不复制相机资源定义。

#### Scenario: 相机资源已有正式 ResourceId

- **WHEN** Camera 提供已确认的 TreeClip/Node、ResourceId 和生命周期映射
- **THEN** 曲线迁移任务 MUST 通过正式作者 API 写入本批 TreeClip 与生成源码
- **AND** Camera MUST 不并行重建相同输出范围
