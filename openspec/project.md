# Project Context

## Purpose
本项目是在 Unity 2022.3 LTS 客户端中构建复杂动画 3C demo，并逐步接入 Fantasy 网络同步、客户端预测和回滚。目标不是另起一套角色控制器，而是把当前角色 `CharacterFramePipeline`、Locomotion 领域模块、Action 领域模块、运动驱动和 Animancer 表现层扩展成可展示、可测试、可同步的动画技术样板。

## Tech Stack
- Unity 2022.3.62f2c1 项目：`3cDemo/Client/3C_Client`
- C# / Unity Test Framework / EditMode tests
- Animancer 作为动画播放外观层
- ScriptableObject 配置驱动角色能力
- Fantasy.Net 服务端与 Fantasy protocol export tool
- OpenSpec 用于能力设计、审批和归档

## Project Conventions

### Code Style
- 生成代码尽量少写注释；关键复杂逻辑可用少量中文注释解释。
- 保持抽象和实现分离，避免把业务规则写死在 MonoBehaviour 细节里。
- 配置驱动优先，动画资源和动作链路从 SO 模块进入运行时。
- 不删除现有 log，除非用户明确要求。

### Architecture Patterns
- 角色帧最高调度入口是 `CharacterFramePipeline`；`CharacterBehaviorSubmissionRunner` 或等价组合模块汇集 Locomotion、CommittedAction 和后续行为域的 sibling submissions。
- 角色级帧管线代码必须位于 `Assets/Scripts/Character/Pipeline/Model|Runtime|Contracts/...`；Action 领域代码位于 `Assets/Scripts/Character/Action/Model|Runtime|Solver|Config|Diagnostics/...`，不得恢复 `Action/FullBody` 作为正式目录或主线入口。
- Locomotion 是 Movement module，负责移动状态演进、移动事实和移动候选输出；它可以内部使用状态图，但不属于 `FullBody` 子树。
- Action 是 Action domain，负责请求解析、打断、lifecycle、body/channel claim、动作运动和动作动画候选；Action 可使用已审批的领域局部 graph、timeline 或策略对象，但不要求成为角色级统一大状态树叶子。
- `FullBody` 只表达 body/channel claim、动作输出占用或动画层语义，不是 Locomotion owner、状态树根、runtime source、rollback adapter 名或第二个角色帧权威。
- 正式状态 ID 使用 `Locomotion.Idle`、`Locomotion.MoveLoop`、`Action.Dodge` 这类领域 ID；`FullBody/Locomotion/...` 与 `FullBody/Action/...` 只允许作为遗留迁移输入或归档历史出现，active specs 和新提案不得把它写成目标架构。
- 角色身体数据模型使用六层术语：Source、Action、Claim、Slot、Channel、Presentation Layer。新 proposal、spec 和 design 必须说明自己修改的是哪一层。
- Source 是提交候选的领域来源，例如 LocomotionSource、CommittedActionSource 和未来经批准的 UpperBodyActionSource；Action 是动作语义，例如 `Action.Dodge`、Attack、Shoot。
- Claim 是身体占用声明，例如 FullBody claim 或 UpperBody claim；Slot 是角色级仲裁后的资源位置。当前正式讨论使用 `BaseSlot` 和 `UpperBodySlot`，`CharacterFramePlan` / `BodyOccupancyDecision` 的正式读取面是 `BaseSlotOwner`、`UpperBodySlotOwner` 和 `UpperBodySlotSuppressed`，不得把 `FullBody` 当成 slot、source、graph node 或 gameplay owner。
- `BaseLayerOwner` / `UpperBodyOwner` 属于旧 layer 口径，不是正式 gameplay contract。涉及身体数据模型的更新必须把 runtime、compiler、editor adapter 和测试迁到 slot contract；只有表现层文档可以使用 animation layer 语义。
- Channel 是输出类型，例如 Motion、Animation、Window、Cue、facts；Presentation Layer 是表现执行，例如 motion executor、Animancer layer、AvatarMask、Timeline view、VFX/SFX/Camera presenter。
- Dodge 是 `Action.Dodge`，由 CommittedAction source 提交 FullBody claim；它不需要也不得重新引入 `FullBody` gameplay node、FullBody 主状态树或第二角色帧入口。
- UpperBody 当前只作为 claim/slot 扩展位和未来 source 的设计边界存在；Facial、FaceBody、FacialOwner、FacialCandidate 或 facial slot 未经新的 OpenSpec 批准不得进入正式 BodyArbiter 或 frame plan。
- `com.inspiaaa.unityhfsm` 可以保留为第三方库参考，但未经新的 OpenSpec 审批不得接入为正式角色状态机 engine。
- `CharacterStateMachineRunner` 只解释状态图、选择 transition、维护 active state / state time / variant / pending transition 和纯数据 snapshot/restore。
- 状态机运行时代码目录为 `Assets/Scripts/Character/StateMachine/Model|Config|Solver/...`，中心配置资产目录为 `Assets/Configs/3C/StateMachine/`；旧 `Statemachine` 拼法不得作为并行入口保留。
- timeline facts、state output、motion command、animation request、input consume、run latch 和 diagnostics 必须位于明确外围模块或 runner 内明确子职责，不得回到混合式大 runner。
- 物理位移权威通过当前运动 executor / `CharacterMotionDriver` 主线收敛，状态机和动画 Presenter 不直接调用 `CharacterController.Move`。
- 动画播放外观是 Animancer Presenter；状态机输出动画语义 key / timeline binding key，具体 clip、transition、fade、speed、start time 归动画配置和 Animancer TransitionLibrary。
- 输入、运动、动画、相机和诊断都是状态机外围 adapter，只提供纯数据 facts 或消费状态机 frame 输出。
- 网络同步不得直接同步 Unity 对象、Animancer 内部对象或场景实例引用，必须先映射为稳定 ID 和纯数据快照。

### Testing Strategy
- 实现代码必须配套 Unity EditMode 测试。
- 每个 OpenSpec proposal 的任务清单必须包含自动测试和工具验证；手动验证方式可以写在 proposal/design 说明中，不写入 `tasks.md`。
- 优先使用 Unity MCP 运行定向 EditMode 测试；全量测试若初始化超时，不视为替代定向测试失败。
- OpenSpec 变更必须通过 `openspec validate <change-id> --strict --no-interactive`。

### OpenSpec Cleanup
- `openspec/specs/` 和本文件是当前架构真相；`openspec/changes/archive/` 只保留仍有追溯价值的历史记录，不作为实现或规划依据。
- 已被当前 specs 完整覆盖、已经明确被后续架构废弃、或会误导后续实现的 archive 可以删除，不需要在 archive 内维护兼容叙述。
- 删除当前 spec 前必须确认该能力已经被其它当前 spec 完整吸收，或确认对应运行时目标已经退役；不得保留废弃占位 spec、旧命名镜像 spec 或 fallback spec。
- 清理 archive 或 current specs 后必须运行 `openspec validate --specs --strict --no-interactive`；若同时有 active changes，再运行 `openspec validate --all --strict --no-interactive`，并说明任何失败是否来自既有未完成迁移。

### Git Workflow
- 工作树可能包含用户或其他 agent 的未提交变更，不能回退未确认的改动。
- 不使用破坏性 git 命令。

## Domain Context
- 复杂动画 demo 需要展示移动、起步、循环、急停、跳跃、落地、闪避、翻滚、翻越、装备、瞄准、近战连招、远程射击、受击、死亡、表情、音效、IK、RootQ 姿态和镜头事件。
- 后续网络能力基于 `AnimationStateSnapshot`、Fantasy 协议 DTO、输入历史、快照历史、预测、回滚和事件去重。
- `Ref` 目录可作为参考，不直接复制实现。可参考 UE/Animancer/ZZZ 类项目的动作节点、预输入、取消窗口、notify state、root motion 采样和服务端同步思路。

## Important Constraints
- 新能力和架构变化必须先走 OpenSpec proposal，未经审批不得直接实现大功能。
- 任务颗粒度要细。
- 所有需要绕过当前系统额外做的路径必须停止，等待审批。
- Demo 不新增未审批的独立角色控制器路径。
- OpenSpec 内容使用中文书写。

## External Dependencies
- Fantasy.Net 网络框架。
- `3cDemo/Tools/NetworkProtocol` 下的 proto 协议。
- `3cDemo/Tools/ProtocolExportTool` 用于生成客户端和服务端协议代码。
