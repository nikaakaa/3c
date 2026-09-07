# deterministic-rollback-two-client-demo Specification

## Purpose

定义两个 Unity Client、纯 .NET Relay 与独立只读 GM 组成的 Rollback Demo，保持精确 Candidate／Run 身份、选择性输入时序和现有 Gameplay 边界。

## Requirements
### Requirement: Demo 必须使用两个Unity Client与一个纯.NET Dedicated Relay Server

Player MUST使用唯一共享 GameplayLab 场景和显式 Rollback Variant，不恢复旧 Bootstrap／Peer Scene 分裂入口。

每个Demo Run MUST从一个精确DeterministicRollback Candidate启动两个独立Unity Client、一个纯.NET Dedicated Relay和一个独立开发GM。两端 MUST加载相同CandidateId、Model、SemanticHash、Fixed ProgramHash、TickRate、CollisionWorldHash、KCC identity和stable actor roster。Relay MUST只拥有网络职责和GM窄只读查询桥，不执行Gameplay Program、KCC、Presentation或Unity Scene；GM MUST不参与gameplay handshake、canonical input、rollback history或hash。不同Slot中的Demo Run MUST拥有独立RunId、SessionId、endpoint、token、进程与日志。

#### Scenario: 双端开始模拟

- **WHEN** 一个Run的Client A/B完成精确Candidate与Session handshake
- **THEN** Relay MUST校验全部deterministic和Run identities后才允许SimulationTick推进
- **AND** GM MUST只查询该Run的Relay快照

#### Scenario: Demo 使用选择性输入时序

- **WHEN** 一个Run的双Client开始推进Rollback Session
- **THEN** 连续移动与Immediate request MUST使用0 Tick模型延迟，Corin Offensive request MUST使用2 Tick延迟
- **AND** confirmed frontier MUST继续使用独立confirmation delay

#### Scenario: 启动两个开发Session

- **WHEN** 作者用两个不同Slot启动两份Candidate
- **THEN** 每个Run MUST各自启动Relay、GM、Client A、Client B四个进程
- **AND** 两个Run MUST不共享endpoint、token、mutable runtime或日志目录

#### Scenario: 旧Unity Host或固定Product入口进入候选

- **WHEN** Candidate Session Plan、Scene closure或启动参数包含Canonical Host、Host Player、固定ProductRoot或StopExisting
- **THEN** Build或Run MUST失败
- **AND** MUST不保留旧入口作为fallback

#### Scenario: 启动开发产品

- **WHEN** 作者从正式 Candidate 入口启动本次开发 Run
- **THEN** MUST由该 Candidate 的工具与 Session Plan 启动 GM、Relay、Client A、Client B
- **AND** 四个角色 MUST绑定同一 Candidate／Run／Session，只有两个进程是 Unity Player

#### Scenario: 工具访问四个只读命令

- **WHEN** 作者在独立 GM 进程的文本控制台提交正式查询
- **THEN** 独立 GM MUST通过 Relay 查询桥获得该会话事实
- **AND** MUST不修改移动、Offensive 延迟、最大预测领先量或表现链路

#### Scenario: 旧 Unity Host 进入产品

- **WHEN** Scene closure、manifest 或参数包含旧 Canonical Host 或 Host Player role
- **THEN** Build MUST失败，不保留 fallback

#### Scenario: 两端 Handshake

- **WHEN** Client A 与 Client B 加入 Demo
- **THEN** Relay Server MUST校验全部deterministic identities后才允许SimulationTick推进
- **AND** Server MUST不加载Fixed Program或Collision World内容

#### Scenario: Demo 启动产品

- **WHEN** 作者选择已发布的精确 Rollback Candidate 与合法 Slot
- **THEN** Run MUST启动该候选的 Dedicated Relay、独立 GM、Client A 与 Client B
- **AND** 只有两个进程是 Unity Player，Run MUST不重新 Build

#### Scenario: 旧 Unity Host 资产进入产品

- **WHEN** Build scene closure、manifest或启动参数包含Canonical Host Scene或Host Player role
- **THEN** Build MUST失败
- **AND** MUST不把旧Host保留为fallback

### Requirement: Demo 必须复用 Corin 同一 Gameplay Semantic Artifact

两端 MUST使用与单机/ServerAuthoritative相同SourceRevision/SemanticHash的Corin `.csir`，并由Fixed Target生成相同Fixed Program。Fixed ProgramHash MAY且通常 MUST不同于Float32 ProgramHash。Rollback Presentation MUST通过正式Fixed Adapter生成与Frontend相同的`CharacterPresentationSemanticContract`并复用唯一target-neutral Projection，MUST不生成或加载Float32 Program作为Projection前置依赖。业务覆盖移动、转身、闪避、Run、Attack1/Attack2、连段、打断、Timeline TreeClip Window、motion curve和GameplayEffect。系统 MUST不使用rollback专用节点、业务图、第二semantic evaluator或第二Projection。

#### Scenario: 迟到 Combo Input

- **WHEN** Attack2 request 的 canonical input 迟到
- **THEN** 两端 MUST通过相同 Fixed Program restore/replay 得到相同 Action/Timeline state

#### Scenario: 修改 Corin Authoring 后构建 Rollback Player

- **WHEN** 作者修改 BTSMTL、Timeline 或其它 Corin Character Definition依赖后执行Rollback Build
- **THEN** Build入口 MUST先从当前Definition重新生成validated Semantic IR、Presentation contract与target-neutral Projection
- **AND** MUST由唯一Fixed Target Adapter从同一Semantic IR生成Fixed Program artifact
- **AND** MUST在Player Build前精确校验ProgramId、SourceRevision、SemanticHash、ContractHash与ordered producer contract
- **AND** 任一身份不一致 MUST拒绝构建，MUST不复用旧Fixed Program、旧Projection或Float32 Projection前置产物

#### Scenario: Fixed-only Rollback产品发布

- **WHEN** Deterministic Rollback Product只声明Fixed Numeric Target
- **THEN** 公共Build Orchestrator MUST只发布Fixed Program与同一target-neutral Projection
- **AND** MUST不调用Float32 Target Compiler或写入Float32 Program wrapper

### Requirement: Demo 必须限制并明确世界能力范围

Demo MUST只使用已编译的静态 DeterministicCollisionWorldArtifact、fixed capsule Actor contact profile和已声明 KCC capabilities。Rollback Variant MUST在共享GameplayLab场景中引用与Local Fixed相同的正式可见环境和Collision Artifact；该场景中唯一`DeterministicCollisionWorldAuthoring`及其显式surface marker MUST同时作为可见测试几何和Fixed Collision Artifact的唯一作者来源，Build MUST不创建隐藏临时碰撞世界。Rollback Composition MUST显式要求`WorldFeature.ActorCollision`。UI/文档 MUST明确支持静态世界与双Actor `SolidBodyBlock`，但未支持 Unity Physics、Rigidbody、moving platform、动态破坏、质量/冲量物理和完整竞技网络产品。

#### Scenario: 查看 Demo 能力

- **WHEN** 作者查看Dedicated Relay Server与Client Diagnostics
- **THEN** MUST显示静态几何、fixed capsule、ActorCollision、SolidBodyBlock与双 Actor 限制

#### Scenario: 作者调整通用移动测试环境

- **WHEN** 作者修改共享Prefab中的楼梯、坡面、墙体、门洞、台阶或不平整静态几何并执行Rollback Prepare/Build
- **THEN** Baker MUST从共享GameplayLab场景的同一正式Collider作者层级重新生成CollisionWorldHash
- **AND** MUST不从代码生成第二份隐藏测试地图

#### Scenario: 一个 Peer 冲刺撞向另一个 Actor

- **WHEN** Peer A的闪避或Timeline motion在一个Tick内会穿过Peer B
- **THEN** 两端 MUST由同一Fixed KCC batch阻挡或沿接触切向滑动
- **AND** rollback/replay后 MUST保持相同Actor Body、WorldHash与KCC hash

### Requirement: Demo 必须暴露 Rollback 与 Desync 诊断

Demo MUST只读显示predicted/canonical/confirmed tick、Offensive request delay、confirmation delay、relayed explicit arrival lead/late、exact remote input hit、predicted fallback、rollback count/depth、replayed ticks、world/actor/KCC hash、desync scope、snapshot recovery、Body/动画branch replacement和presentation keep/replace/cancel。Relay Server MUST只读显示forward/dedupe/invalid计数、canonical前沿与confirmed前沿。Diagnostics MUST不修改simulation result。

#### Scenario: 发生一次 Rollback

- **WHEN** late explicit input替换了旧predicted input
- **THEN** diagnostics MUST记录输入到达延迟、起始Tick、depth、replayed ticks和replay后hash

#### Scenario: Canonical 只提升 provenance

- **WHEN** canonical bundle与已应用explicit input的GameplayHash一致
- **THEN** diagnostics MUST记录provenance-only promotion
- **AND** rollback与Body/动画replacement计数 MUST不增加
