## Purpose

定义角色控制、技能、运动、效果、装备与网络之间的独立运行和状态边界，使角色不再依赖整体编译产物，并保证各领域内容能独立更新且保持同一模拟时钟、完整恢复与既有业务结果。

## ADDED Requirements

### Requirement: 角色必须按正式领域配置创建唯一运行实例

系统 MUST从明确的角色配置和能力授予创建每 actor 的控制、技能、输入、运动、效果与装备模块。角色 MUST不要求整体可执行 Program、全角色操作表或把全部配置复制成另一种总包。模块 MUST只通过各自正式输入输出连接，且所有 actor MUST继续由同一 Session 拥有的模拟时钟和世界推进。

#### Scenario: 创建两个使用同一角色配置的角色
- **WHEN** 两个 actor 引用同一合法控制配置和技能
- **THEN** 系统 MUST复用只读技能与资源数据，并创建独立的控制、技能实例和业务状态
- **AND** 两者 MUST不共享可变状态机、技能计时或世界 body

### Requirement: 技能构建必须只处理自身可达玩法内容

技能构建 MUST以明确的 Ability 为根，只编译其私有图、FSM、条件、子图引用及 TreeClip 引用的技能图、局部状态和数值；Timeline／Motion 只作为真实内容和资源依赖，轨道／Clip 不编成技能操作。控制状态机、BodyMotion、全角色装备目录、Pose 图、相机和动画资源内容 MUST不进入技能执行数据。角色与装备授予 MUST引用同一技能数据，不按角色复制。缺失实际技能依赖 MUST明确报错。

#### Scenario: 只修改攻击窗口
- **WHEN** 作者修改一个 Ability 的攻击窗口且其公共接口未改变
- **THEN** 系统 MUST更新该 Timeline 内容和引用它的实际技能依赖版本，不重新编译未改变的技能图操作
- **AND** MUST不重建 Locomotion、Pose、ACL 或网络 Pass 计划

#### Scenario: 同一技能授予两个角色
- **WHEN** 两个角色授予同一内容和数值目标的 Ability
- **THEN** 系统 MUST消费同一不可变技能数据，并在运行时各自绑定目标、输入和实例状态

### Requirement: CSharp控制与运动资源必须独立拥有实际行为

角色走跑转身、输入准入与技能请求 MUST只由 C# 控制模块执行。静态运动描述 MUST只从其正式合同取得，可配置参数 MUST由明确控制配置提供，不得编码进技能后再重建。SourceCurve MUST继续消费精确的运动资源，保持时间、空间、数值与运动仲裁语义。

#### Scenario: MovingTurn使用源位移曲线
- **WHEN** 控制模块提交带源曲线和连续 Tick 的转身请求
- **THEN** 运动模块 MUST从精确绑定资源计算该 Tick 位移和旋转，并进入唯一世界求解链
- **AND** 资源缺失 MUST失败，不得以恒速、默认曲线或技能 Timeline 猜测替代

### Requirement: 领域状态必须在统一Tick边界完整提交和恢复

控制、输入请求、技能执行、效果、装备以及会影响下一 Tick 的运动状态 MUST分别拥有正式状态格式。角色快照 MUST组合同次成功提交的完整分区；世界和 Pipeline 状态 MUST继续按原 owner 保存。恢复 MUST先完整解码、检查身份并建立候选，再原子提交。Pending、临时运动贡献、图对象、Pose 内存和资源句柄 MUST不进入玩法快照。

#### Scenario: 在技能中段恢复并重放
- **WHEN** 网络要求恢复到具有活动技能和控制状态的历史 Tick
- **THEN** 系统 MUST同时恢复控制机器、技能调用帧和 Timeline 时间、请求、目标、效果、装备及世界状态
- **AND** 后续 MUST按原 Pipeline 重放，不得仅恢复位置或活动技能名

### Requirement: 玩法一致性身份必须覆盖代码合同与实际数据

Session MUST锁定控制模块语义版本、必要玩法配置、技能／授予内容、领域状态格式、数值目标及世界要求。网络 MUST拒绝不匹配身份。该身份 MUST只是兼容信息，不承载角色操作表或重复配置；纯表现资源变化 MUST不改变玩法身份，实际共同接口变化 MUST重新检查绑定。

#### Scenario: 控制逻辑版本不同
- **WHEN** 两端技能数据相同但控制模块的语义版本不同
- **THEN** 系统 MUST拒绝建立兼容玩法会话，不得因技能 Hash 相同而接受

### Requirement: 原生表现不得影响玩法恢复与权威判定

Pose、节点实例和播放表现内存 MUST只属于本地表现。技能命中、移动、目标和权威判定 MUST不从实际骨骼、原生图或动画权重反推。网络重放 MUST不重复推进 Pose 图，提交后的表现更新 MUST遵守原模型的事件确认和去重规则。

#### Scenario: 一次网络纠正重放多个Tick
- **WHEN** 同一外层更新恢复并重放多个玩法 Tick
- **THEN** 系统 MUST按模型规则提交最终角色事实与表现事件
- **AND** MUST不让本地 Pose 播放时间跟随重放次数重复增长

### Requirement: 普通DotNet服务端必须保留可消费的数据和模块

保留的普通 .NET Authority MUST只依赖 portable 控制和玩法模块、技能执行数据及必要配置，不得依赖 Unity、FlowCanvas 或 Pose。Float32／Fixed 的数值规则和各 Solver 能力要求 MUST保留，不能通过简化配置放宽原不支持的组合。

#### Scenario: DotRecast后端缺少垂直运动能力
- **WHEN** 角色要求的世界能力不被选定 Authority Solver 支持
- **THEN** Session preparation MUST明确失败，不得丢弃垂直分量或改用其它 Solver

### Requirement: 作者操作必须按领域触发并明确版本边界

技能构建、Pose 校验与实例替换、资源构建、Pipeline preparation MUST各自只消费真实依赖，不得在 selection、Inspector 重绘或普通保存时触发整体构建。活动技能实例 MUST绑定开始时的技能版本；网络会话玩法内容改变 MUST重新准备，不得热换身份。

#### Scenario: 运行时安装新版技能数据
- **WHEN** 本地作者显式安装一个接口兼容的技能版本
- **THEN** 已活动技能 MUST继续消费其原版本，后续实例使用新版本
- **AND** 网络锁定会话 MUST不隐式接受改变后的玩法内容

### Requirement: Timeline必须直接调度内容而非生成操作程序

Timeline MUST使用唯一正式运行时直接调度轨道、Clip、区间、播放参数和资源引用，不生成 Timeline Semantic IR、Clip operation、操作控制流或全图状态布局。技能调用节点 MUST只保存调用和精确内容绑定；TreeClip 图 MUST仍走独立技能编译与执行。Unity 与普通 .NET MUST消费同一正式内容合同的只读表示，portable 运行不读 Unity 资产，不接入 Slate Runtime 或第二播放器。

#### Scenario: 修改Timeline片段区间
- **WHEN** 作者修改合法片段的区间但不改变技能图逻辑
- **THEN** 系统 MUST更新正式 Timeline 内容和实际依赖身份
- **AND** MUST不重新把轨道和Clip发射成操作程序

#### Scenario: 一次Tick跨过多个边界
- **WHEN** 正式时钟在一次 Tick 跨过多个 Clip／loop／section 边界
- **THEN** 唯一 Timeline Runtime MUST按既有稳定顺序处理进入、采样和退出，并保持窗口、Decision／Commit和取消语义

### Requirement: Motion源与时间映射必须由唯一正式合同提供

MotionCurve MUST引用 RootMotionCurveAsset 的正式源内容，源区间和播放时间映射 MUST由 Timeline 领域唯一提供。技能侧 MUST消费直接 Timeline 内容的实际运动依赖，C# Control／Motion MUST消费正式资源绑定；两者不得依赖旧 ControlMotion catalog 或整个角色 Program，不得在 portable 数值运行中读取 Unity 资产。源的累计终值和 Clip 自身生命周期 MUST分别保持，Weight／Ease 与 Warp 参数继续归原 owner。

#### Scenario: 同一曲线被技能和控制使用
- **WHEN** 技能 Timeline 与 C# 控制引用同一源的明确区间
- **THEN** 两者 MUST通过同一正式映射规则取得目标数值数据与绑定
- **AND** Clip到达源区间末尾后 MUST保持累计终值、后续运动delta为零，不能提前结束尚有效的Clip生命周期

### Requirement: 各领域必须分别提供准备与实际采用结果

技能、Pose、Camera、Motion MUST各自返回请求身份、请求来源与版本、Ready／Pending／Missing／Invalid／Failed 状态和 typed 原因；只有 Ready 才能提供合法准备结果。采用 MUST由实际 owner 单独完成，返回 actor／实例和实际采用版本。明确不需要某领域的角色只能依据正式角色职责返回 NotRequired，不能把缺失配置当成不需要。

#### Scenario: 资源准备失败但角色仍有旧实例
- **WHEN** 请求版本的资源缺失或无效
- **THEN** owner MUST返回真实失败原因，不能以旧实例版本伪装 Ready 或已采用
- **AND** 当前运行实例身份 MUST保持真实状态

### Requirement: 预览必须只消费领域真实状态与操作

预览 MUST只调用公开领域操作并显示对应准备／采用／运行观察事实；不得维护 Character Build、ProgramEpoch、统一假版本或自建领域工厂。准备就绪不等于实际采用，保存作者数据不等于运行实例已更新。Camera准备由其领域拥有，角色装配只调用和挂接，预览不得重写其求解或资源规则。

#### Scenario: Pose准备完成但尚未替换实例
- **WHEN** 新图准备 Ready 但当前 actor 仍运行旧实例
- **THEN** 预览 MUST分别显示请求图版本和实际运行实例版本
- **AND** MUST只在正式替换成功后显示新的 InstanceId／ResetGeneration

### Requirement: 领域采用必须遵守实例和会话生命周期

活动技能 MUST固定启动时的技能与 Timeline／Motion 内容版本；Pose 显式重建 MUST重置对应历史；玩法内容或状态格式变化 MUST按原 Session 规则重新准备。Camera采用 MUST使用其模块的实际采用边界，不得通过重建整个角色冒充相机更新。不同领域 MUST不合成为另一个可执行包或假全局Epoch。

#### Scenario: 修改活动技能引用的运动源
- **WHEN** 作者更新源数据但当前技能实例仍活动
- **THEN** 当前实例 MUST继续使用启动时绑定的内容，正式重新准备后新实例采用新版本
- **AND** 网络锁定会话不得偷偷更换轨迹或内容身份
