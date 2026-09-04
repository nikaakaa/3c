## Purpose

定义 ZZZ 角色相机完整移植时必须保留的配置、算法、时间、组合与输出行为，以及每项行为到 3C 正式资源、运行消费者和编辑器入口的对应关系，防止以字段存在或局部效果替代完整可运行能力。

## ADDED Requirements

### Requirement: 完整移植必须建立同版本的原行为对应表

系统 MUST为纳入移植的角色相机保存原游戏版本、资源身份与内容 hash、类型/函数证据、输入输出、计算顺序、数值单位、坐标空间、时间来源和正式消费者。对应表 MUST区分已确认配置、已确认执行行为和未解析依赖；不同版本的配置与函数不得无标记混用。每个已纳入字段和行为 MUST对应正式实现及作者入口，只有具有原消费者证据的无效或纯编辑字段才能记录为不参与运行。

#### Scenario: 字段存在但消费者未知

- **WHEN** 已解码某个 Stretch 参数但尚未确认其计算方式
- **THEN** 对应表 MUST把该项保持为未解析，并定位缺少的消费者证据
- **AND** 移植 MUST不以忽略字段、默认值或近似曲线将该项标记完成

#### Scenario: 同名资源来自不同版本

- **WHEN** 两份同名相机资源的版本或内容 hash 不同
- **THEN** 导入 MUST要求明确的同版本来源闭包
- **AND** MUST不按文件顺序选择一份或拼接两版字段

### Requirement: 移植范围必须覆盖完整角色相机职责与依赖

移植 MUST覆盖角色默认 Profile、球面和轨道构图、单点/双点/多点与实体取景、手动旋转与响应、普通目标/Boss 锁定、位置/旋转阻尼、从当前镜头进入与混合移动、状态覆盖、OverrideTrack、Zoom、Stretch及回弹、Shake、Shot、相机碰撞、输入接管、打断、退场和目标切换。上述入口实际可达的配置、曲线、镜头资源、开关、优先级与组合分支 MUST进入同一依赖闭包；不得以当前 Corin 演示未触发为由删除已纳入的核心行为。

#### Scenario: Shot 引用公共曲线或镜头资源

- **WHEN** 已纳入 Shot 引用共享曲线、镜头资源或绑定规则
- **THEN** 移植 MUST继续解析该依赖并提供正式资源与消费者
- **AND** 只有 Shot 名称而没有资源或运行绑定 MUST不算完成

#### Scenario: 同一帧触发多个效果

- **WHEN** 技能同一时点触发 Override、Zoom、Stretch 和 Shake
- **THEN** 移植 MUST保留原行为的组合顺序、覆盖关系、各自时间与退出规则
- **AND** MUST不把这些效果统一折算为一个 FOV 强度

### Requirement: 正式导入必须保留数值语义并发布完整作者资源

导入 MUST显式接收精确来源、目标角色配置和依赖闭包，保留原事件帧率、时间尺度、曲线关键点与切线/插值语义、角度/距离单位、坐标系、枚举和特殊持续时间语义。来源名称 MUST只用于可追溯身份和显示，运行资源绑定 MUST使用正式强类型引用。导入 MUST经唯一作者事务发布资源与引用；未知枚举、缺失曲线、身份冲突或未解释特殊值 MUST在发布前报告，失败 MUST不留下半套可运行配置。

#### Scenario: 持续时间包含特殊值

- **WHEN** 原配置包含 `HoldTime = -1` 或等价哨兵值
- **THEN** 导入 MUST依据对应原消费者的已确认语义生成明确的持续/释放规则
- **AND** MUST不直接钳制为零、擅自理解为永久或以无限长浮点时间替代

#### Scenario: 来源帧率不同于 Simulation tick rate

- **WHEN** 原事件帧率与目标项目 tick rate 不同
- **THEN** 导入 MUST按来源帧率保留事件时间，并由正式时间跨越规则处理目标 tick
- **AND** 同一时点事件顺序、循环与重复触发身份 MUST保持明确

### Requirement: Corin 相机资源与事件必须完整接入同一角色配置

Corin 已解码的 81 项 Shake、18 项 Zoom、18 项 Stretch、4 项 Override，以及其余可达 Profile、Curve、Sequence、Shot 与事件 MUST逐项对账并进入正式角色配置。事件 MUST迁入现有角色 Graph/Action Timeline 的已提交请求链，原 Animator 事件表 MUST不成为第二个运行调度器。完整性 MUST同时覆盖资源、时间、绑定、作者编辑、运行消费和诊断来源。

#### Scenario: AssaultAid 相机时点

- **WHEN** 对照 `Corin_Attack_AssaultAid` 的已确认事件表
- **THEN** 第 0 帧 Override/Zoom/Stretch 和第 8、19、32、52、55 帧 Shake MUST分别有正式请求与对应资源
- **AND** 帧号 MUST按原事件帧率解释，不按目标 tick rate 直接代入

#### Scenario: ParryAid 与未决来源事件

- **WHEN** 对照 ParryAid H/L 与 SwitchInAttack 的已确认事件表
- **THEN** ParryAid 第 124 帧共享 Shake MUST进入单角色相机对账，SwitchInAttack 的身份和时点 MUST只保持为来源证据
- **AND** SwitchInAttack 在换人/跨角色归属未完成设计前 MUST不生成当前框架的切人或第二角色相机路径

### Requirement: 完成判定必须覆盖每个原行为的正式去向

系统 MUST提供可定位的完整性报告，逐项关联来源身份、行为证据、正式资源、编译结果、运行消费、作者入口和现有诊断数据。任何已纳入项未解析、无消费者、无法绑定、无法编辑或仍依赖旧执行链时，完整移植状态 MUST保持未完成；编译成功或少数演示镜头可播放不能替代该判定。报告 MUST是同一正式导入/编译/运行信息的只读结果，不拥有替代相机求值器。

#### Scenario: 资源已导入但没有实际运行效果

- **WHEN** Shake 资源已存在而运行消费仍为空分支
- **THEN** 完整性报告 MUST定位该资源和缺少的消费者并保持未完成
- **AND** MUST不因资源数量匹配而通过完整性判定

#### Scenario: 原输出承载链尚未闭合

- **WHEN** 尚未确认基础相机数据与实际 Cinemachine 输出实例之间的关系
- **THEN** 报告 MUST明确缺少的写入点、更新顺序或实例证据
- **AND** MUST不声明已经完成逐行为移植
