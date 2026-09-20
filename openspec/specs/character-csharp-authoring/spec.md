# character-csharp-authoring Specification

## Purpose

定义通过两个显式作者入口把正式Graph、Timeline、Pose和EventGraph资产导出为可审查C#创建代码，并从已编译代码重建明确范围资产的合同。人工资产与代码输出不自动同步，业务规则继续由各领域API拥有。

## Requirements

### Requirement: 作者工具必须只有导出代码和创建资产两个入口

系统 MUST只提供导出当前正式资产为C#代码、执行明确已编译入口创建资产两个公共操作。调用方必须显式给出领域、根资产或生成入口和输出范围；工具不得根据Selection、场景对象、目录扫描、显示名或旧配置猜测目标，也不得提供节点级任意写入、YAML修改、Document同步或兼容协议。

#### Scenario: 导出明确资产

- **WHEN** 调用方提交合法领域和根资产路径
- **THEN** 导出 MUST只读取该根及其正式可达依赖
- **AND** MUST返回精确输出文件、内容身份和结构化失败

#### Scenario: 创建入口不明确

- **WHEN** 调用方没有提供可解析的已编译创建入口
- **THEN** 创建 MUST拒绝执行
- **AND** MUST不扫描程序集、项目目录或上次输出猜测入口

### Requirement: 人工编辑与代码操作必须保持非同步

人工修改、Undo、保存和窗口编辑 MUST只作用于正式资产；源码编辑或编译 MUST只作用于代码。系统 MUST不因资产变化自动导出、不因编译自动创建资产，也不得自动合并人工修改与旧导出。导出和创建均不得自动Build运行产物、启动ScenePlay或改变Runtime状态。

#### Scenario: 人工修改资产

- **WHEN** 作者在正式窗口修改并保存Graph或Timeline
- **THEN** 资产 MUST按原Mutation与Undo合同更新
- **AND** 任何C#输出 MUST保持不变直到显式导出

#### Scenario: 导出代码完成

- **WHEN** 导出成功写入声明范围
- **THEN** 系统 MUST报告写入文件和identity
- **AND** MUST不执行资产创建、运行准备或播放

### Requirement: 导出必须形成最小完整重建闭包

导出 MUST从正式资产读取稳定identity、节点、端口、连接、typed字段、默认值覆盖、资源引用、子图、曲线、业务顺序和根挂接，并输出能够从空范围重建目标的确定性C#代码。范围外共享资产 MUST作为显式输入引用；范围内私有对象、环和共享引用 MUST使用本次创建的局部变量连接。导出 MUST不复制运行缓存、编译offset、窗口状态、SourceMap索引或调度元数据。

#### Scenario: 图包含环和共享子图

- **WHEN** 范围内多个节点引用同一对象且连接形成合法环
- **THEN** 代码 MUST先创建对象再连接引用
- **AND** 重建后 MUST保持共享关系和稳定逻辑identity

#### Scenario: 引用范围外资源

- **WHEN** Timeline或Pose引用范围外AnimationClip、Graph或Profile
- **THEN** 输出 MUST把它声明为精确外部输入
- **AND** MUST不复制资源内容或按GUID反查上一次生成对象

### Requirement: 公共机制必须通过领域API消费正式内容

公共导出器只负责确定性遍历、引用图、代码组织、输出差异和文件归属；Graph、Timeline、Pose、EventGraph及后续登记领域 MUST分别提供薄适配读取正式字段并调用正式创建API。字段、默认值、端口、校验和业务顺序发生常规变化时，应由领域定义和通用字段访问自动进入导出，不得要求同步维护一套镜像业务DTO或中央switch。

#### Scenario: 领域新增普通typed字段

- **WHEN** 正式节点定义新增可作者编辑字段
- **THEN** 领域适配 MUST通过同一字段合同导出和创建
- **AND** 公共导出器 MUST不增加该业务类型的专用分支

#### Scenario: 领域不支持某字段

- **WHEN** 当前正式API不能无损表达资产字段
- **THEN** 导出或创建 MUST定位字段并失败
- **AND** MUST不跳过、补默认值或直接写SerializedProperty绕过领域规则

### Requirement: Timeline导出必须保留正式内容和独立曲线

Timeline输出 MUST完整表达Track、Clip、Section、TreeClip、执行域、时间区间、资源binding、Clip数据段和所有正式曲线。曲线 MUST保留key time、value、in／out tangent、weight、WeightedMode、pre／post wrap及其owner；外部源曲线只输出精确引用与使用映射，不复制为Timeline局部曲线。创建必须沿Timeline正式AddTrack／AddClip／AddSection和typed配置入口恢复内容。

#### Scenario: 重建含曲线与TreeClip的Timeline

- **WHEN** 导出范围包含本地曲线、TreeClip和共享资源
- **THEN** 重建 MUST保持内容顺序、identity、全部曲线语义和根挂接
- **AND** MUST不读取Slate草稿、显示降采样数据或旧生成子资产

### Requirement: 生成必须执行明确入口并原子保存声明范围

创建资产 MUST执行调用方指定的已编译入口，通过正式领域API创建、校验、挂接并保存声明范围。成功前所有变化必须属于同一资产事务；失败时不得留下半成品、部分根挂接或混用旧输出。工具只可替换本次明确拥有的生成范围，不得清理范围外文件或猜测消费者。

#### Scenario: 从空范围重建

- **WHEN** 生成范围内旧输出不存在且创建入口合法
- **THEN** 系统 MUST创建完整资产并恢复根引用
- **AND** 逻辑identity MUST由代码声明决定而不是复用旧GUID

#### Scenario: 创建中途失败

- **WHEN** 任一字段、引用或保存步骤失败
- **THEN** 本次范围 MUST保持失败前正式状态或完全不存在
- **AND** MUST返回失败阶段、目标和原因

### Requirement: 外部输入与生成输出必须严格分离

生成入口 MUST显式声明只读外部资产与本次拥有的输出对象。共享资源、范围外Graph和原生素材不得被修改、移动或删除；输出代码和生成资产必须位于调用方声明路径。工具调度参数、源文件路径和执行元数据不得写入作者资产或生成类业务字段。

#### Scenario: 两个根复用同一外部资源

- **WHEN** 两次生成引用同一范围外AnimationClip或Graph
- **THEN** 两个结果 MUST保留同一外部引用
- **AND** 任一生成 MUST不复制或修改该资源

### Requirement: 业务规则必须继续由正式模块承担

C#代码只表达作者对象创建、字段配置、连接和根挂接。Action准入、Timeline运行、Pose求值、Camera请求、Gameplay Effect、Motion、网络、诊断和资源准备 MUST继续由各自正式模块执行；生成类不得携带运行调度器、兼容reader、第二Validator或业务执行旁路。

#### Scenario: 创建Gameplay内容

- **WHEN** 生成代码配置Ability、Timeline和Pose引用
- **THEN** 代码 MUST只调用各领域authoring API
- **AND** MUST不直接执行Action、播放动画、求值Pose或启动Session

### Requirement: GameplayAbilityDefinition必须是完整技能生成根

技能生成范围 MUST以GameplayAbilityDefinition为明确根，包含其正式Ability外壳、Gameplay Graph、状态局部行为、ActionProfile、Timeline和必要binding；共享资源继续作为外部输入。删除并重建该范围时 MUST恢复Definition到内部对象的正式引用，不得只生成孤立FSM或Timeline冒充完整技能。

#### Scenario: 重建完整技能

- **WHEN** 调用方选择一个GameplayAbilityDefinition生成范围
- **THEN** 创建入口 MUST重建该范围内全部私有内容并恢复根挂接
- **AND** 范围外共享Graph、Clip和Profile MUST保持原引用

### Requirement: 操作结果必须如实报告

导出和创建 MUST返回操作类型、目标、实际读取或写入文件、资产、identity、未执行项和结构化错误。部分能力未实现时必须失败或明确报告未完成，不得以代码生成、序列化成功、文件存在或旧产物可读宣称完整重建。

#### Scenario: 只完成代码输出

- **WHEN** 导出成功但尚未执行创建资产
- **THEN** 结果 MUST只报告代码已生成
- **AND** MUST不声称资产、运行binding或产品已经更新
