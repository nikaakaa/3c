## ADDED Requirements

### Requirement: Camera 作者目标必须进入现有 Character Document v4

Character Document v4 MUST通过正式注册的 `editable/presentation/camera/` 分片表达 Camera Profile、Sequence、Override、Zoom、Stretch、Shake、Shot、Curve 及其引用，并把 Definition 装配和 Graph/Timeline 资源引用纳入同一目标状态。每个资源 MUST使用包含 GUID、非零有符号 local file id 与一致 asset path 的结构化对象引用；新建正式资源或子资产 MUST使用已声明的 local identity 规则。Camera 分片 MUST只出现在有相应 Character authoring 能力的文档闭包内，AI domain MUST拒绝这些可编辑分片。场景 Transform、物理世界、原 dump、生成相机计划和 Runtime 状态 MUST不作为可写作者正文。

#### Scenario: checkout 角色相机配置

- **WHEN** 显式 Character Definition 装配正式 Camera Profile
- **THEN** checkout MUST导出完整可达 Camera 作者分片、资源 Catalog 与依赖身份
- **AND** generated Projection 与场景绑定 MUST只作为只读上下文提供

#### Scenario: Agent 修改某个技能 Zoom

- **WHEN** Agent 修改正式 Zoom 资源参数并保持 Timeline 引用
- **THEN** dry-run MUST定位真实资源 owner，列出同包受影响引用与依赖变化
- **AND** MUST不把参数写成 Timeline 私有副本或运行时配置

### Requirement: Camera 能力必须由同一 Catalog 与严格 Codec 闭合

相机资源、字段、单位、枚举、时间/坐标域、曲线 Channel、目标槽位和 Graph/Timeline 引用 MUST由人工编辑、Compiler 与 Document 共享的正式能力定义提供。Exporter、Codec、Reconciler、Validator 和反向导出 MUST同时支持全部已公开 Camera authoring 语义。v4 Camera 扩展 MUST进入正式 capability/context revision；不含当前必需闭包或上下文过期的旧工作包 MUST明确要求重新 checkout，不提供旧 Camera schema reader、字段兼容或未知字典保留。

#### Scenario: Document 使用未知效果字段

- **WHEN** Camera 分片包含未注册字段、类型不匹配引用或未知枚举
- **THEN** strict parser/Validator MUST在任何资产 Mutation 前报告精确 path、code、message 和 suggestion
- **AND** MUST不忽略字段或把它转成通用 Custom 效果

#### Scenario: Agent 新建 Camera 资源

- **WHEN** Agent 按已注册的文件闭包和 local identity 规则声明新资源并配置正式引用
- **THEN** service MUST校验真实 owner、完整分片和引用关系后纳入有效整包 hash
- **AND** Agent MUST不直接编辑 manifest，任意额外文件 MUST继续被拒绝

### Requirement: Camera 修改必须复用唯一整包资产事务

Camera 创建、修改、删除、引用替换、曲线编辑和正式导入 MUST通过与人工作者相同的 typed Mutation 及资产事务；Document Reconciler 只生成完整计划，不直接写 Unity YAML、SerializedProperty 路径或生成产物。dry-run/apply MUST锁定整个包与全部实际资源 owner，共享资源冲突 MUST明确报告，不自动覆盖外部修改。apply 成功 MUST反向导出正式对象身份并返回 Clean；任何 Mutation、校验、保存或 package 发布失败 MUST同时恢复 Camera、Gameplay、Timeline、Animation 和其它同事务 owner。

#### Scenario: 新建效果并引用时发生失败

- **WHEN** 新 Camera 效果已创建但后续 Timeline 引用校验失败
- **THEN** 事务 MUST恢复全部新增资源、Profile 目录、Timeline 及同包变更
- **AND** 正式 Document MUST保持 apply 前内容，响应 MUST不报告 Clean

#### Scenario: 共享曲线被另一个作者修改

- **WHEN** apply 前某个 Camera Curve 的 live revision 已偏离整包基线
- **THEN** service MUST返回冲突并定位该共享 owner
- **AND** MUST不按资源名合并或自动覆盖

### Requirement: 相机接入必须保持现有生命周期工具与明确 Build 边界

Camera Document MUST继续只通过 checkout、rebase、dry-run、apply 和 validate 五个正式生命周期工具操作。Camera authoring apply MUST不构建 Program/Projection、不改变活动镜头、不启动 Preview；生成产物 MUST通过精确 Definition 的既有 Character Build 生命周期明确发布。MCP bridge、Window、Skill 和诊断文档 MUST同步呈现同一能力，禁止相机专用局部编辑 MCP、watcher 或第二套导入/Mutation 服务。

#### Scenario: Camera Document apply 成功

- **WHEN** Agent 成功修改 Profile 和 Camera Timeline
- **THEN** authoring MUST保存且整包 MUST回到 Clean，相关 generated products MUST明确显示需要重建
- **AND** 相机运行输出 MUST不被直接修改，Build MUST等待明确命令

#### Scenario: Bridge 报告相机资源错误

- **WHEN** Camera Validator 发现缺失 Shot 依赖
- **THEN** MCP 与人工窗口 MUST展示同一机器诊断和精确作者路径
- **AND** MUST不通过另一编辑工具补建默认 Shot
