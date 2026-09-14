## Why

当前角色必须先生成整包 Character Program，再与 Pose ProgramImage、Projection 和 Session 身份对齐；修改技能、角色控制或表现资源会牵动不属于该领域的编译、状态与发布合同。用户已明确选择让 C# 拥有角色控制、技能只处理自己的执行数据、网络保留 Pipeline／Pass，并让 Pose 直接使用 FlowCanvas Runtime；Timeline 直接调度正式内容数据，不再生成时间轴 IR／operation，以减少重复表示和整体构建依赖。

## What Changes

本change保留整体目标与公共合同，实施清单按2026-09-14协调审阅拆回已有任务：Timeline Runtime归Timeline配对，Pose原生Runtime归Pose配对；本任务只保留核心运行、状态／网络与公共集成。当前仅更新规划归属，不启动实现。

- **BREAKING**：取消 `CharacterSimulationProgram` 作为整个角色的执行、配置、资源和状态装配根。角色由 C# ControlModule、AbilityRuntime、Motion、Effect、Equipment 等正式模块装配；不以另一个总包或万能运行上下文改名保留旧职责。
- **BREAKING**：Semantic 处理收窄为 Ability 的私有 Graph／FSM／条件／子图引用，以及 TreeClip 引用的技能图。保留 Float32／Fixed 数值目标、portable 技能数据、必要技能局部状态和来源映射；不把角色控制、BodyMotion、装备总目录或 Pose 资源继续编码进技能产物。
- **BREAKING**：Timeline 轨道、Clip、区间、播放参数和 MotionWarp 配置作为正式时间轴数据直接调度，不编成 Semantic IR／Program operation。技能只保留调用 Timeline 的节点和精确内容引用；TreeClip 图独立编译。保留 portable 内容导出、资源绑定和目标数值准备，复用现有时间、窗口、取消、回绕和恢复语义，不接入 Slate Runtime 或第二播放器。
- 各领域分别提供技能、独立Timeline、Pose、Camera、Motion的准备／实际采用结果。Timeline独立Prepare／CreatePlayback不要求Ability外壳；Advance只生成Pending，调用方Step决定提交／丢弃，Timeline提供分型私有状态，核心只聚合。预览只消费真实领域操作与结果，不恢复 Character Build／ProgramEpoch 或创造假全局版本。
- Camera 领域准备 Profile／资源到只读运行绑定，本任务只负责装配调用与旧 Projection 挂接迁出；运动曲线统一引用 RootMotionCurveAsset 和 Timeline 唯一时间映射，portable 数值运行不读取 Unity 资产。
- 保留全部现有网络 Pipeline、四阶段 Pass、Backend、Source、WorldSolver、预测纠正、回滚、History、EventId disposition 与普通 .NET Authority 产品边界。仅迁移角色执行、玩法内容身份和完整状态快照接口，不撤销网络模型或改用固定 C# 网络流水线。
- **BREAKING**：Pose Graph、节点及连接使用 FlowCanvas 原生 Runtime，按角色实例化、由现有表现宿主手动驱动；删除 Pose IR、ProgramImage、全图操作调度及其专属编译和发布链，不把编译转移到加载时。
- 保留 PoseState、Player、Slot、混合、惯性化、Source、Foot／Goal／FBBIK、Animancer Barrier 和唯一 Final Publication 的业务与所有权。真实节点端口、单次求值缓存、分型姿态缓冲、资源生命周期及节点内部数值算法接入原生图；不把算法改成默认播放、图外 IK 或第二写骨骼路径。
- EventGraph 继续原生运行并唯一写入动画实例变量，Pose 只读其成功发布的 typed Frame。有限 Action／Timeline 直接提交现有播放生命周期请求，不经 EventGraph 转发。
- Projection 中的 Pose ProgramImage 与全 Character Program 身份依赖退役；仍被消费的 Rig、ACL、动画资源、有限动作与 Camera 绑定迁回各自正式资源／实例绑定，资源烘焙不随技能构建触发。
- 正式运行、Pose／Timeline 预览、Live Debug、C# authoring 和 Build／Run 产品入口共同迁移；旧产物、旧字段、旧 reader 与废弃 UI 同步退出，不保留双运行路径或运行时自动构建。
- 按用户2026-09-14明确选择先大删除再接线：先删除整角色Program、Timeline操作码编译／ProgramPlan、Pose IR／Image及专属执行、转换、缓存和产物链，再把保留业务接入正式接口，不等旧消费者全部迁完才删除。允许中间提交编译失败或功能明确不可用，以剩余引用定位接线；不为维持旧体系编译增加桥接、占位类型或假成功。共享文件中的技能／动画算法保留并做必要提取；技能编译、Pipeline／Pass计划校验及ACL／Motion Matching／Foot资源处理继续保留。
- 本次只规划；保留当前正确算法、作者 identity、已发布 ACL 和其它窗口修改。Corin 是本次资产迁移对象，TrainingEnemy 的不稳定作者数据和未完成行为任务不纳入。

## Capabilities

### New Capabilities

- `character-domain-runtime`：角色模块装配、完整玩法状态、资源引用和分领域构建身份。
- `native-flowcanvas-pose-runtime`：原生 Pose 图执行、节点求值、资源与缓冲生命周期以及独立预览。

### Modified Capabilities

- `btsmtl-gameplay-semantic-ir`：IR 只处理技能图逻辑与调用引用；Timeline 内容直接调度，不再以 Character 为必要根。
- `btsmtl-compiled-simulation-program`：技能执行数据替代角色总 Program，角色模块状态和资源不再编入技能容器。
- `character-pipeline-definition-authoring`：角色配置与生成技能数据、表现资源的独立作者入口。
- `character-state-timeline-authoring-loop`：角色 Locomotion 唯一归 C#，Ability／Timeline 继续拥有有限玩法生命周期。
- `graph-authoring-domain-framework`：Pose 原生节点执行与技能编译的领域差异，保留共享字段、端口和作者 API。
- `character-pose-plan-compilation`：撤销全图 IR／ProgramImage 编译要求，接管必要的原生图校验。
- `character-presentation-pose-graph`：原生 Pose 求值、实例绑定、状态子图与运行观察。
- `character-pose-graph-runtime-architecture`：移除编译操作 owner，保留状态、Source、Constraint、Final Publication 和帧提交边界。
- `character-animation-pipeline`：原生 Pose 执行接入唯一表现帧和动画 Barrier。
- `character-animation-layer-runtime`：移除 Program producer 总身份依赖，保留有限播放、Slot 和持续 Pose 的职责。
- `gameplay-simulation-session-composition`：从角色 Program 装配迁到领域运行模块，五项显式组合和唯一 Session owner 保留。
- `gameplay-simulation-pipeline`：Pass 调用角色执行与快照接口，Pipeline 计划、能力校验及提交边界保留。
- `character-simulation-kernel`：Evaluate／Finalize 和完整角色状态迁为领域模块合同，保留事务、世界与事件边界。
- `btsmtl-semantic-ir-inspection`：检查工具只读取技能／独立内容产物，Pose 观察直接使用原生运行身份。
- `btsmtl-timeline-editor-preview`：有限动作预览消费正式 Timeline 实例与领域准备／采用结果，原生 Pose 仍走同一实现。
- `server-authoritative-hybrid-sync-model`：内容与状态格式身份替代整角色 Program／Layout 身份。
- `server-authoritative-prediction-correction-pipeline`：完整领域状态的 baseline、History 和恢复重放。
- `server-authoritative-host-portability`：普通 .NET Host 消费 portable 技能／模块配置，继续唯一 Composer。
- `deterministic-rollback-network-model`：保留 Fixed、确定性世界、输入／快照／Hash／重放，迁移角色状态身份。

## Impact

- 核心Editor：Character Semantic Frontend／Builder、共享技能编译、资源身份与公共构建／产物；BtsmtlSkillTimelineCompiler由主实现唯一写入。Pose Image专属Compiler退出归Pose，Timeline专属发射退出归Timeline；预览、Camera和C#适配保持原owner。
- 核心Runtime：Character Host／Registration／Factory、Float32／Fixed角色Step和状态codec、Control／Ability最终数据／provider、网络与表现外壳。Timeline和Pose内部Runtime及专属旧链由各自既有任务清单交付，核心不串行代做。
- Network／Server：Session Composer、Pass 产品及快照接口、握手与 Authority manifest；协议身份正式升级，网络行为和部署产品分工不改变。
- Authoring：复用已安装的 FlowCanvas／NodeCanvas、正式 Capability／Mutation 与显式 `export_code`／`generate_assets`，不新增插件或作者同步机制。
- 接口现状（2026-09-14，检查至dd0708d46及工作区）：旧错误目录接线已撤回，28个旧编译源码已删除；独立技能安装／服务、领域状态codec和新CharacterRuntime port已有代码。Timeline已有数值／资源准备及游标Advance候选／Commit／Discard，Pose已有多种值节点和Constraint／Final入口。当前主要纠正角色状态错误依附单个Ability、效果服务安装拒绝不使用该能力的技能、Timeline只按落点识别Clip，以及Pose具体Source实现与Host缺失；完整业务接线仍未完成。旧快照见D16／D18，最新执行依据D19和任务清单，命名本身不构成重做技能数据的理由。
- 分工：唯一领域清单分别是`restyle-timeline-editor-slate-style/tasks.md`与`refine-pose-graph-readonly-blackboard/tasks.md`的Runtime接收章节，本任务只保留指针和公共集成项。领域owner发布实际采用事实，核心装配并汇集，不自行制造版本；详见design D13—D15。
- 文档：本提案替代旧评估中“继续保留角色总 Program”的方向。现行 spec、`openspec/project.md` 和并行 Pose／EventGraph／技能 FSM 提案的矛盾与准确分工见 `design.md`；本轮不改写其它任务，不把旧完成项重新判为未完成。
