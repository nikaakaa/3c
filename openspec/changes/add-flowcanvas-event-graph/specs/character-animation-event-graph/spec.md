## Purpose

定义通用事件图与现有动画输入的职责边界：正式Fact、作者实例变量、子图输入、姿势曲线和节点配置各守来源，只有实际需要时才装配动画事件宿主。迁移保持现有表现，保留原生运行与同次只读变量交接，不为使用事件图新增动画效果。

## ADDED Requirements

### Requirement: 动画输入整理必须保持现有业务

整理 MUST先列出现有数据或计算的生产者、真实消费者、类型、时序和所属职责。只有当前已存在且明确归为作者逻辑的计算才进入事件图迁移；迁移 MUST保持原公式、阈值、时钟、初值、Reset和消费者。系统 MUST不为使图非空而新增步频、播放倍率、平滑或改变状态判断。

#### Scenario: 当前没有应迁移的作者计算

- **WHEN** 当前职责清单没有需要放进事件图的既有业务逻辑
- **THEN** 该角色 MUST可以没有业务事件图
- **AND** MUST不通过复制事实、常量Set或新动画效果制造内容接入

#### Scenario: 某既有计算被确定为作者逻辑

- **WHEN** 已明确该计算的原实现和实际Pose消费者
- **THEN** 迁移 MUST保留既有行为并删除同义重复生产
- **AND** MUST不额外改变运动或动画策略

### Requirement: 正式事实与其它输入必须分别拥有来源

Body/Intent对齐、正式运动事实 MUST继续由其公共事实合同提供，只读Fact输入 MUST不因可视化而复制为动画实例变量。实例变量 MUST引用同一原生声明和唯一布局；子图入参 MUST属于调用接口；曲线 MUST从指定输入Pose读取并保持原混合/惯性语义；节点配置 MUST留在对应正式字段或资源owner。

作者输入来源 MUST以正式类型和绑定表达，不通过Action/Foot固定ID或名称前缀猜测。编译器可以派生只读执行索引，但 MUST不以此保留第二份可写声明或每张图的同义变量定义。

#### Scenario: Pose直接读取正式MovementMode事实

- **WHEN** 既有状态规则使用正式MovementMode Fact
- **THEN** 规则 MUST保持原事实身份和语义
- **AND** MUST不被自动改写为EventGraph中的速度判断或同名变量转抄

#### Scenario: 整理Foot曲线来源

- **WHEN** 作者输入表区分Foot曲线与实例变量
- **THEN** 曲线 MUST继续来自对应姿势的原采样和混合链
- **AND** MUST不恢复EventGraph对该曲线的共享Set

### Requirement: 事件图必须按实际需求装配

未绑定事件图且正式输入合同不要求动画变量或事件执行时，动画 MUST沿同一正式Fact/Pose链工作，不创建占位宿主、占位图或虚构变量发布帧。存在必需变量但图、声明或绑定缺失时 MUST明确失败，不补旧motor值或默认变量。显式绑定图 MUST执行其合法原生逻辑，不因为输出变量为空而被隐式跳过。

#### Scenario: 无事件图需求的角色

- **WHEN** 角色未配置事件图，Pose只消费Fact、曲线和已有配置
- **THEN** 同一动画装配 MUST接受该正式输入合同
- **AND** MUST不要求创建仅有Start/Update的图来通过装配

#### Scenario: 必需变量缺少生产者

- **WHEN** Pose声明读取动画变量但没有合法图或变量绑定
- **THEN** 接入 MUST失败并定位缺失输入，不能转到无变量模式继续

#### Scenario: 已绑定图没有输出变量

- **WHEN** 作者显式绑定合法事件逻辑且未发布变量
- **THEN** 图 MUST仍按原生事件语义执行
- **AND** 系统 MUST不根据变量数量自动删除或跳过配置

### Requirement: Fact输入节点必须引用正式事实合同

事件图Fact输入 MUST通过正式节点和直接API绑定已有Fact身份，在本次图逻辑读取时取当前宿主只读值。受支持输入目录 MUST来自正式事实声明及明确类型适配，不独立维护另一份字段规则。需要但不支持的类型 MUST明确报告，不猜测转换或补值。

#### Scenario: 创建并消费水平速度输入节点

- **WHEN** 作者把正式水平速度Fact输入节点连到已确定的图内计算
- **THEN** 节点 MUST读取当前同次Fact值
- **AND** 仅将FactFrame存入宿主上下文、没有节点消费时 MUST不被描述为已完成业务内容接入

### Requirement: 已装配宿主必须保持正式更新生命周期

已装配的动画事件宿主 MUST在首次有效输入上初始化并执行本次更新，后续只随正式表现采样及其delta推进。暂停或没有正delta时 MUST不推进；图窗口与额外组件 MUST不提供另一时钟。

#### Scenario: 首次执行已有事件逻辑

- **WHEN** 新实例收到首次有效表现输入
- **THEN** 初始化写入 MUST对随后同次更新可见，完整成功后才发布变量

### Requirement: 动画变量必须精确且只发布完整更新

需要变量的实例 MUST按稳定图/Variable.ID、Float/Int32/Bool精确类型和唯一布局交接同次完整输出。帧 MUST携带实例、表现采样、Simulation tick、Reset代际及合同版本，所有消费者完成前 MUST不能覆盖。缺变量、类型或身份不匹配 MUST失败。值读取需求与索引 MUST由正式编译或实例绑定确定，不在每帧重新发现全部消费者。

#### Scenario: 已有变量在本次Set后被Pose读取

- **WHEN** 原生事件完成对已确定业务变量的写入
- **THEN** 随后的同次Pose消费 MUST读取该次值，不持有可变Blackboard对象

#### Scenario: 生成后恢复变量引用

- **WHEN** 代码重建已配置事件图并恢复明确根绑定
- **THEN** 图ID和Variable.ID MUST可重建，Get MUST解析同一声明与布局
- **AND** MUST不依赖旧生成资产GUID或第二变量表

### Requirement: 更新状态与Pose提交必须保持各自语义

原生事件成功后的变量和节点状态 MUST按表现时间保留，即使随后Pose Source未就绪。原生更新失败 MUST不发布部分结果；Pose Actor Faulted时 MUST停止其后续事件更新。Reset/Body discontinuity/Replacement MUST按同一实例完整清理变量和原生历史；普通PoseState切换 MUST不重建整个事件图。

#### Scenario: 已配置事件图更新后Pose未就绪

- **WHEN** 事件更新成功而Pose source返回Pending
- **THEN** 原生更新状态 MUST保留，Pose沿原规则处理Pending
- **AND** MUST不重放事件或退回旧输入伪装共同提交

#### Scenario: 写入中途发生错误

- **WHEN** 一次事件已部分Set后失败
- **THEN** 本次变量 MUST不发布，故障实例停止被消费
- **AND** MUST不把仅恢复Blackboard当成完整原生状态回滚

### Requirement: 动画事件必须保持一次同步调用

已装配动画宿主中的事件逻辑 MUST在本次调用内完整结束，时间计算 MUST使用正式宿主delta。未准入的跨帧等待、全局时间模式或悬挂断点 MUST明确拒绝；这一动画宿主限制 MUST不删除通用FlowCanvas其它合法宿主能力。

#### Scenario: 动画图使用未准入的全局时间模式

- **WHEN** 作者配置绕过宿主delta的每秒赋值
- **THEN** 局部规则 MUST报告不支持的模式，不悄悄替换原生语义
