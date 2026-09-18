# PoseGraph只读Blackboard任务

第1、2组保留已完成的只读输入与r2作者接入事实，不重新打开。第3组是2026-09-14接收的原生Pose Runtime唯一实施清单，复用实现窗口01a081f3-46f4-7c91-8930-73923ff7950b；用户已明确授权由该实现窗口开始执行，具体授权见design.md。主方案D13的原编号仅作职责映射，不在两处重复执行。

## 1. 只读输入与作用范围

- [x] 1.1 定义Graph输入的正式分类、声明owner、消费者和可访问范围，分清动画实例变量、只读表现事实、Pose曲线和节点/资源配置。
- [x] 1.2 引用事件图唯一变量Contract/Layout/Frame，按图/变量稳定身份及Float、Int32、Bool精确类型建立Pose消费接口，不复制声明、布局或更新器。
- [x] 1.3 定义Root、StatePose、普通Subgraph与Linked Pose入口的可访问输入和公开参数规则，区分外部变量读取与子图调用参数，不把根图声明复制到全部子图。
- [x] 1.4 让FlowCanvas原生Blackboard投影当前图可访问的正式输入，显示名称、类型、来源、作用范围和使用情况；合法未使用声明保持可见或可筛选，不因暂未连线而禁止使用。
- [x] 1.5 让拖拽只创建绑定正确声明的Get，禁止PoseGraph主图创建共享动画变量Set；编辑绑定和引用继续使用唯一typed Mutation。
- [x] 1.6 移除动画属性导入器向每张PoseGraph复制BlendShape等声明的路径，改由正式曲线/资源合同提供编译所需完整曲线清单，保持最终属性写入消费者。
- [x] 1.7 区分动画变量帧、正式Fact、子图公开参数和指定输入Pose曲线的读取，补齐类型、来源、作用范围、Stage依赖和缺失数据诊断，保留完整曲线依赖。
- [x] 1.8 为Body内部FootPlacement权重建立明确曲线绑定或公开输入合同；保留已有曲线混合、惯性响应与Foot权重作用，迁移完成后删除不再需要的根图Get和透传端口。
- [x] 1.9 在Blackboard、Get、子图入口和Slot主要显示区域使用作者名称，内部稳定ID只用于引用与详情诊断，重命名不破坏连接。
- [x] 1.10 通过正式Pose类型化API及既有资产服务删除指定生成范围内确认无引用的重复子图与废弃声明，保留范围外资源；不依赖Document反向导出或自动合并未导出修改。
- [x] 1.11 补齐共享Capability、正式Pose字段读取/配置、动态端口、资源引用、领域校验、Compiler source map与只读观察，供人工编辑和C#输出薄适配使用同一语义。
- [x] 1.12 更新对应spec与项目当前状态，明确EventGraph接口、曲线传播与只读消费边界的实际交付范围，移除迁移后的旧入口和过期说明，不把未交付能力写成current truth。

## 2. r2公共输入与原生C#作者协议接入

- [x] 2.1 将CharacterPoseGraphAuthoringAdapter.ApplyEventGraphMutation改为调用事件图正式原生操作API，移除经AgentAuthoringEventGraphDocumentMapper.Map调用ApplyAuthoringDocument的依赖；Pose Mutation载荷不保留Agent DTO或同义Document模型。
- [x] 2.2 修改CharacterAnimationInputContract，共享实例变量部分引用事件图唯一合同与布局，保留原Fact、Slot、World、子图入参和source-local曲线部分。
- [x] 2.3 将Get、Transition条件和BlendSpace接到同次成功发布的typed变量帧，保留条件短路、priority、stable order及状态时间语义，拒绝Gameplay mutable address。
- [x] 2.4 让运行和完整Pose/角色Preview沿同一动画宿主消费变量帧；单资源查看继续使用原正式资源调参合同，移除其对角色固定motor桥的依赖。
- [x] 2.5 迁移全部CharacterPresentationProgramParameterFrame消费签名及Preview调用，使用同一实例/采样/tick/Reset/版本身份和输出租约；事件图任务在消费者迁移完成后删除旧类型与生产方法。
- [x] 2.6 提供可完整重建Pose图、节点、变量引用、曲线、布局、动态端口和资源引用的正式读取/配置能力；业务ID重建一致，生成范围内引用使用本次新对象。
- [x] 2.7 在显式生成范围中恢复Profile/Definition的明确根挂接，按已有依赖失效与独立显式Build处理新对象；不依赖旧生成子资产GUID，不自动Build或新增源码同步。
- [x] 2.8 清理正式Pose层的Agent命名空间、Mapper/DTO和协议校验调用，把仅存在旧协议中的必要Pose规则归回原领域修改或编译入口，不新增中央Validator或整包同步事务。

## 3. 原生FlowCanvas Pose Runtime接收范围（实施中）

DOMAIN-BOUNDARIES-20260914-03执行补充见design第12节：3.8—3.14沿真实Source/Barrier/求值/ValidatePending/Final完成服务、注入需求与结果消费，保留已有原生节点及算法；共享Host由核心4.7/6.4唯一接线。3.15/3.17覆盖实际generation/释放/采用结果；表现Discard不回滚已接受玩法Step，Pose历史不进入玩法快照。仅续接当前未完成部分，不复制checkbox，不把接口存在当完整交付；只通知本实现，不要求回执。

2026-09-14当前接线口径（主方案D19）：除Clip／BlendSpace／Selected Player外，Blend／Layered／Additive等值节点已有算法，Constraint适配和CharacterFinalPoseNativePublication已有真实入口；Source准备也有新增，不能继续把这些全部列为“尚无代码”。但角色Host未接入；ICharacterPoseNativeStateMachineSource已有CharacterPoseNativeStateMachineSource具体实现并接入唯一handler工厂，共享Host仍需提供真实装配与最终消费，StateMachine服务存在不能替代Host接线。接续按3.8—3.14核对每类节点的真实服务、构造注入和下游消费者，完整事项仍未勾选；不增加空服务或通用外壳冒充业务完成。

主方案D17已明确删除优先；a31c27b79删除了Pose编译Pass和Timeline发射主链。3.18不再等待共享Host或全部消费者切换后才删旧Image专属代码，混合文件保留有效算法和作者定义。共享Host／Barrier外壳由核心tasks 4.7接入，本任务提供真实服务和结果，不复制公共装配。

- [x] 3.1 提供Pose领域分型Prepare、Create/Replace、PrepareFrame、Evaluate、Commit、Discard、Stop/Dispose接口及真实准备/采用结果，明确请求、图/资源版本、实例、Reset代际和帧身份。
- [x] 3.2 将CharacterPoseCanvasGraph接入FlowCanvas原生初始化与Manual运行，移除Pose作者图的原生执行禁令，沿现有表现时钟驱动而不安装第二自动Update。
- [x] 3.3 将CharacterPoseCanvasNode、Connection和NativePorts改为真实原生typed绑定，提供Local/Component Pose、精确变量/Fact及目标结果端口，删除Editor占位输出。
- [x] 3.4 建立每Actor原生图及StatePose/Subgraph/Linked Pose调用实例生命周期，共享作者资产和资源只读，可变节点历史按调用实例隔离。
- [x] 3.5 在原生节点中按Actor、图调用实例、求值身份和阶段缓存结果，防止共享分支重复推进Player、状态转换、源采样、Foot/FBBIK或最终输出。
- [x] 3.6 将空间、必要输入、递归子图、悬空引用、唯一Output/Goal Set/FBBIK、目标槽和写冲突规则接到正式原生图校验/绑定入口，保留准确节点和引用链诊断，不生成IR。
- [x] 3.7 让原生运行、UI、Clipboard、Mutation和C# authoring共享同一Pose节点字段/端口/资源定义，移出仍有业务意义的规则后删除Image专属第二映射。
- [x] 3.8 实现同一原生图的源需求准备和姿态求值阶段，准备结果只表达活跃分支与source demand，求值仅在唯一Animancer Barrier完成后读取同次源结果。
- [x] 3.9 将Player、PoseState、Slot、BlendStack和Inertialization的既有逻辑接入原生节点，保留准入、时间、权重、relevance、capture/release及Pending/Committed历史。保留已有Player和混合算法；补齐ICharacterPoseNativeStateMachineSource等具体服务实现、构造注入和实际输出消费者，不能以handler或接口声明数量判定完成。
- [ ] 3.10 接入Phase同步、state-local source与Linked Pose正式绑定，保留实际资源身份、continuation、readiness和调用共享规则，不用插件通用FSM替换动画状态语义。
- [ ] 3.11 将原生节点采样需求与结果交接接回唯一Source模块，保留ACL/Playable和现有资源准备/释放能力，不新增direct Play、资源副本或旧Image回退。已有Player Source binding必须由正式实例装配注入，并让同次Barrier后的采样进入实际姿态链，不能只完成binding类声明。当前状态：模块帧生命周期已由RoleRuntime单点接回（开帧/收帧/丢弃与根图帧对称，子图共享租约），绑定Prepare已具备前提；剩余收口为评估阶段native Job完成时机（同步评估屏障在新管线无调用方，CompletedAt检查疑似逐帧错位），待运行日志指认后修复。
- [x] 3.12 让Foot、Goal聚合和FBBIK原生节点调用IK任务当前正式Constraint接口，保持输入与求解次序、单数Goal/输出，不复制Foot/IK状态或修改算法。
- [x] 3.13 实现按实例复用的节点输出缓冲、只读分支输入和明确租约释放，移除全图Value Lifetime/Workspace计划依赖；节点内部已有Native/Job算法由原owner管理。
- [x] 3.14 将原生Output接入唯一Final Publication，复用已新增CharacterFinalPoseNativePublication及物理／属性Writer入口，完成正式实例注入和最终消费者切换；保留整Rig预检查、完整骨骼写入、提交／丢弃及Barrier前后故障边界，不从节点另写Transform。共享Host由核心4.7接线，Final服务存在不等同角色已调用。
- [x] 3.15 完成原生实例Reset、Replacement与Stop/Dispose，先阻止旧调用、完成在途工作、失效旧generation并释放资源，实际安装成功后由Pose发布采用版本与重置结果。
- [x] 3.16 将已完成Get/条件/BlendSpace的唯一EventGraph变量帧消费适配到原生端口与调用实例，保留只读范围、精确类型、曲线来源和Source Pending不回退事件状态的行为。
- [x] 3.17 从原生图、Node、Port、调用实例和已完成阶段结果发布节点观察与Pose Watch，保持订阅/租约释放，不为旧SourceMap生成隐藏Image或重新求值。
- [x] 3.18 按主方案D17先删除Pose IR、ProgramImage、Execution View、全图操作表/Worker编排和专属Compiler/产物入口，再处理保留消费者接线；混合文件只提取有效算法／作者规则，不等待Host全链完成才删。a31c27b79已完成编译Pass主链删除，剩余类型与消费者仍需收口；共享壳及总Projection引用交核心，不恢复加载期编译、兼容reader或双运行模式。
- [ ] 3.19 清理Source Module执行壳残留死面：BindDemand、RequireDemand、PrepareFrameResult、RequirePendingReady、SealReadiness、HasPreparedSource、RequirePreparedResources、EnterEvaluateBarrier、usage与release诊断包装及FramePage的demand/result半边在新管线零调用方，统一删除；同pass按运行日志收口评估阶段Job完成时机的底层失败。
