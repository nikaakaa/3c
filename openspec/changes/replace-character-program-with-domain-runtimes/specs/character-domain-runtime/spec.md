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

技能构建 MUST以明确的 Ability 为根，只处理其私有图、FSM、条件、子图引用、Timeline、局部状态、数值和实际使用的领域引用。控制状态机、BodyMotion、全角色装备目录、Pose 图、相机和动画资源内容 MUST不进入技能执行数据。角色与装备授予 MUST引用同一技能数据，不按角色复制。缺失实际技能依赖 MUST明确报错。

#### Scenario: 只修改攻击窗口
- **WHEN** 作者修改一个 Ability 的攻击窗口且其公共接口未改变
- **THEN** 系统 MUST只使该技能及真实依赖它的技能内容失效
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
