## ADDED Requirements

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

### Requirement: 动作相机请求必须统一由 TreeClip 特殊 Node 表达

技能动作中的 Camera State、Effect、Response、Target 请求 MUST 通过正式 TreeClip 内的相机特殊 Node 表达。Node MUST 只提交带稳定 ActionContext、来源身份、ResourceId 和生命周期的 typed Camera request，不得直接写 Camera、Cinemachine 或虚拟相机。动作链 MUST 不再为同一请求维护 CameraCueTrack、CameraCueClip 或 `ActionCueClip(CueType: Camera)` 的并行表达。

#### Scenario: 动作在指定时点触发镜头效果

- **WHEN** TreeClip 执行到一个带正式相机特殊 Node 的动作时点
- **THEN** Node MUST 向 Camera Runtime 提交对应的 Camera request
- **AND** Camera Runtime MUST 继续负责效果求值、生命周期、碰撞和最终输出
- **AND** Node MUST NOT 直接修改 Camera 或 Cinemachine 状态

#### Scenario: 动作相机请求被取消或循环

- **WHEN** TreeClip 发生循环、取消、自然结束或 seek/replay
- **THEN** 相机请求 MUST 使用稳定的 action/cycle/event 身份只触发一次并按正式退出合同处理
- **AND** MUST 不因每个逻辑 tick 重复提交同一请求

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
