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

角色状态与事务 MUST由角色绑定和完整领域状态定义，不得依附一个技能安装对象。多个技能的局部值／调用帧 MUST按技能与调用实例隔离；控制、请求、效果、装备及角色序号 MUST只保存一份。角色内容身份 MUST覆盖实际模块、配置及授予集合，不得直接把任一Ability ContentHash转换成角色GameplayContentHash。

#### Scenario: 在技能中段恢复并重放
- **WHEN** 网络要求恢复到具有活动技能和控制状态的历史 Tick
- **THEN** 系统 MUST同时恢复控制机器、技能调用帧和 Timeline 时间、请求、目标、效果、装备及世界状态
- **AND** 后续 MUST按原 Pipeline 重放，不得仅恢复位置或活动技能名

#### Scenario: 同一角色恢复两个技能与共享效果状态
- **WHEN** 一个角色安装两个不同技能，快照包含两者执行状态和角色效果／装备状态
- **THEN** 恢复 MUST按各技能身份解析局部状态，并只恢复一份角色共享领域状态
- **AND** MUST校验完整角色内容与分区schema，不得挑选某一个技能安装或Hash代表整个角色

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

技能、独立Timeline、Pose、Camera、Motion MUST各自返回请求身份、请求来源与版本、Ready／Pending／Missing／Invalid／Failed 状态和 typed 原因；只有 Ready 才能提供合法准备结果。采用 MUST由实际 owner 单独完成并发布其确认的 actor／调用实例和实际采用版本；核心只装配并汇集，不能将Ready或请求版本重写为已采用。明确不需要某领域的角色只能依据正式角色职责返回 NotRequired，不能把缺失配置当成不需要。

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

### Requirement: 领域工厂必须实际脱离整角色Program装配

角色领域工厂 MUST从明确角色配置、已绑定独立技能集合、数值服务及领域状态创建实例，并由正式Host和原Pipeline Pass实际使用。只把旧Program、ExecutionLayout、Workspace和Evaluator包进新的Factory MUST不被视为完成领域装配；最终公开输入输出不得要求整角色可执行容器。

#### Scenario: 角色没有旧Program但领域配置完整
- **WHEN** 角色已具备合法控制、技能、运动及其它必需领域绑定
- **THEN** 正式工厂 MUST可以创建可由原Pass推进的角色实例
- **AND** MUST不为了创建实例而生成或加载旧Character Program

### Requirement: 独立技能不得复制角色级领域状态

独立技能数据 MUST只声明其局部图执行、调用和实例状态，不得为复用旧执行器无条件复制角色GameplayEffect aggregate、全局随机状态、HandleAllocator或FactSequence。角色级服务与状态 MUST由原正式领域owner唯一提供，多个技能通过typed服务使用同一事实。明确的技能私有状态 MUST具有技能／实例作用域和恢复规则，不能与角色级字段混用。

#### Scenario: 两个技能同时消费效果与实例分配服务
- **WHEN** 同一角色安装并运行两个独立技能
- **THEN** 两者 MUST使用角色正式效果与分配服务，不能各自创建一份角色aggregate或全局序号
- **AND** 两个技能的私有计时与局部变量 MUST仍独立

### Requirement: Provider绑定必须检查真实依赖合同

Provider绑定 MUST从当前实际模块或正式配置取得被引用成员的identity、值类型、合同版本和typed运行句柄，并按技能要求解析。只匹配Provider种类或资产GUID MUST不足以判定Ready。缺失成员、类型不符或版本不兼容 MUST在准备／绑定阶段返回精确依赖原因；不得从旧技能产物反推提供者当前合同。

角色 MAY具有某个技能未使用的领域服务，技能安装 MUST只解析自己声明的需求；服务可用不能强迫全部技能声明该能力，也不得成为拒绝未使用该能力技能的理由。共享服务和业务状态 MUST仍归角色领域owner，不按技能复制。

#### Scenario: 同一资产删除被引用输入
- **WHEN** InputProfile的GUID未变但实际合同已删除技能使用的MoveAxis
- **THEN** 绑定 MUST拒绝该技能依赖并定位输入，不得因owner identity相同而接受

#### Scenario: 提供者改变字段类型
- **WHEN** 技能要求的属性类型与实际提供者声明不同
- **THEN** 绑定 MUST在运行前报告类型不匹配，不能延迟为Tick中的默认值或错误读取

#### Scenario: 角色有效果服务但一个技能不使用效果
- **WHEN** 同一角色安装使用GameplayEffect的技能A和不声明该能力的技能B，并提供合法角色效果服务
- **THEN** 两个技能 MUST都能按自己的声明完成安装，B不得因角色具有该服务被拒绝
- **AND** A所需效果成员不存在或版本不匹配时 MUST明确失败，不能给B强开能力或提供空服务绕过

### Requirement: 独立Timeline必须拥有无需Ability外壳的准备入口

Timeline MUST接收明确内容identity／revision、数值目标、调用身份、资源与实际需要的TreeClip服务，独立返回Ready／Pending／Missing／Invalid／Failed及精确原因。Ready MUST提供只读内容绑定，CreatePlayback MUST建立独立调用实例并由Timeline确认实际版本。无技能的合法内容 MUST不被要求伪造Ability；缺失必要技能服务的内容 MUST明确失败。

#### Scenario: 非Skill调用准备纯相机Timeline
- **WHEN** 调用方提供合法Timeline内容、数值目标、相机绑定和调用身份，内容不包含TreeClip或其它技能需求
- **THEN** Timeline准备 MUST独立完成，不要求技能Program或角色总包
- **AND** Camera采用事实 MUST仍由相机领域确认

### Requirement: Timeline推进必须参加调用方的提交和丢弃边界

Advance MUST只产生本次Step的Pending播放状态与待提交结果，不提前更新committed cursor、发布窗口／运动／效果等Gameplay副作用或内部完成不可撤销提交。调用方 MUST在必要领域与World结果全部通过后决定Commit，否则Discard。Timeline MUST只安装已经验证的自身状态，不重复执行Clip；外部输出只在整个正式Step成功后发布。Stop／取消 MUST遵守同一边界，不能提前关闭其它实例状态。

#### Scenario: Timeline推进后世界求解失败
- **WHEN** Timeline已产生本次Pending窗口与cursor，但同Step的World求解失败
- **THEN** 调用方 MUST丢弃该Pending，Timeline此前committed状态保持不变
- **AND** 本次窗口、运动、效果与外部表现输出 MUST不得提前发布

### Requirement: Timeline私有状态必须分型交接且只有一个owner

Timeline MUST唯一拥有cursor、区间／loop／section、活动Clip与播放私有状态，提供typed Capture／PrepareRestore／ApplyRestore。角色核心 MUST只组合这些同次提交结果与其它领域／世界／Pipeline快照，不复制私有字段语义或直接修改它们。TreeClip的图执行帧归核心技能服务，Timeline只保存其调用关联与调度状态，不能复制第二份技能执行状态。

#### Scenario: 恢复一个含TreeClip的播放实例
- **WHEN** 核心恢复合法的完整角色快照
- **THEN** Timeline owner MUST恢复本实例私有状态，核心技能服务恢复对应图执行帧，并精确重连调用关联
- **AND** 任一内容／schema／调用身份不匹配 MUST在整体状态安装前失败

### Requirement: 公共装配与领域执行必须遵守唯一实现归属

核心 MUST唯一维护角色Host／Factory、角色Step／状态codec、网络checkpoint／manifest及共享技能编译／调用接口。Timeline和Pose MUST各自提供内部运行和分型状态／结果；共享技能Timeline调用入口不能由Timeline另实现TreeClip编译。公共装配只调用领域接口并汇集事实，不解释其私有轨道／图操作或重新生成执行计划。

#### Scenario: 技能调用含TreeClip的Timeline
- **WHEN** 核心技能执行器发起Timeline调用
- **THEN** Timeline MUST使用核心提供的已编译TreeClip执行服务，并由自身Runtime负责播放调度
- **AND** MUST不出现两套TreeClip编译／执行入口或重复操作发射

### Requirement: 领域迁移必须同时退出对应旧编译和运行链

领域迁移 MUST先按owner删除已取消的整角色Program、Timeline操作码／ProgramPlan、Pose IR／Image及专属编译器、执行器、转换层、字段、codec、产物、缓存和构建入口，再接通保留业务消费者；不得以旧消费者尚有引用或新Runtime未完成为理由维持已取消职责。混合文件 MUST保留实际技能／动画／网络算法与资源数据，可做必要提取，不得按文件命名整删业务。中间提交 MAY无法编译或运行，但 MUST记录缺失接线且不得以桥接、占位类型或假成功掩盖。最终完成 MUST同时包含删除与正式Host／工具／产品接通，单独删除或新增外壳均不构成整体完成。技能编译、Pipeline的Pass计划与能力校验及实际资源处理 MUST保留。

#### Scenario: 角色装配两个独立技能
- **WHEN** 正式角色工厂装配两个已准备的独立技能
- **THEN** MUST直接消费各自只读技能数据，不从整角色Program复制操作和布局或伪造Ability根
- **AND** 技能Loader MUST不通过旧角色Program codec转换，退出的转换Factory与旧入口 MUST删除

#### Scenario: Pose与Timeline正式消费者完成切换
- **WHEN** 某领域的正式运行、预览及产品消费者全部切换到该领域原生图或直接内容运行
- **THEN** 该领域先前删除的IR／Image／ProgramPlan及专属编译运行链 MUST保持退出，不能重新进入正式依赖
- **AND** MUST保留仍有效的动画／播放算法、资源准备、网络Pass校验和技能TreeClip服务，不提供新旧运行开关

#### Scenario: 删除批次暴露旧消费者引用
- **WHEN** owner删除已取消的编译和执行类型后，角色或工具仍引用这些类型而无法编译
- **THEN** 实施 MUST区分废弃消费者与保留业务，分别删除或接到正式接口，不恢复旧类型维持编译
- **AND** 缺失接线 MUST记录到对应owner的未完成事项，中间不可用不能被报告为整体交付完成
