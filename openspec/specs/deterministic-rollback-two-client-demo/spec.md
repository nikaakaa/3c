# deterministic-rollback-two-client-demo Specification

## Purpose

定义两个Unity Client与一个纯.NET Dedicated Relay Server组成的隔离Rollback对比Demo，以及构建、启动、握手、选择性输入时序和一致性诊断合同。

## Requirements
### Requirement: Demo 必须使用两个Unity Client与一个纯.NET Dedicated Relay Server

系统 MUST提供一个本地DS Demo组合，启动一个`ThirdPerson.DeterministicRollback.Server`纯.NET Dedicated Relay Server与两个独立Unity Client Player。两端 MUST加载相同ModelId、SemanticHash、Fixed ProgramHash、TickRate、CollisionWorldHash、KccId和stable actor roster。Server MUST只拥有handshake、roster、原始输入立即转发、canonical排序、confirmation、hash与snapshot routing；MUST不执行Gameplay Program、KCC、Presentation或Unity Scene。Unity Player构建 MUST只包含Rollback Bootstrap与Peer Scene。

#### Scenario: 两端 Handshake

- **WHEN** Client A 与 Client B 加入 Demo
- **THEN** Relay Server MUST校验全部deterministic identities后才允许SimulationTick推进
- **AND** Server MUST不加载Fixed Program或Collision World内容

#### Scenario: Demo 使用选择性输入时序

- **WHEN** 双Client开始推进Rollback Session
- **THEN** 连续移动与Immediate request MUST使用0 Tick模型延迟
- **AND** Corin Offensive request MUST使用2 Tick延迟
- **AND** confirmed frontier MUST使用独立confirmation delay

#### Scenario: Demo 启动产品

- **WHEN** 作者运行已经构建的DeterministicRollback network test product
- **THEN** Run MUST启动Dedicated Relay Server、Client A Player与Client B Player
- **AND** 进程列表 MUST只有两个Unity Player

#### Scenario: 旧 Unity Host 资产进入产品

- **WHEN** Build scene closure、manifest或启动参数包含Canonical Host Scene或Host Player role
- **THEN** Build MUST失败
- **AND** MUST不把旧Host保留为fallback

### Requirement: Demo 必须复用 Corin 同一 Gameplay Semantic Artifact

两端 MUST使用与单机/ServerAuthoritative 相同 SourceRevision/SemanticHash 的 Corin `.csir`，并由 Fixed Target 生成相同 Fixed Program。Fixed ProgramHash MAY且通常 MUST不同于 Float32 ProgramHash。业务覆盖移动、转身、闪避、Run、Attack1/Attack2、连段、打断、Timeline TreeClip Window、motion curve 和 GameplayEffect。系统 MUST不使用 rollback 专用节点、业务图或第二 semantic evaluator。

#### Scenario: 迟到 Combo Input

- **WHEN** Attack2 request 的 canonical input 迟到
- **THEN** 两端 MUST通过相同 Fixed Program restore/replay 得到相同 Action/Timeline state

#### Scenario: 修改 Corin Authoring 后构建 Rollback Player

- **WHEN** 作者修改 BTSMTL、Timeline 或其它 Corin Character Definition依赖后执行Rollback Build
- **THEN** Build入口 MUST先从当前Definition重新生成validated Semantic IR与Presentation Projection
- **AND** MUST由唯一Fixed Compiler从同一Semantic IR生成Fixed Program artifact
- **AND** MUST在Player Build前精确校验ProgramId、SourceRevision、SemanticHash与producer identity
- **AND** 任一身份不一致 MUST拒绝构建，MUST不复用旧Fixed Program或旧Projection

### Requirement: Demo 必须限制并明确世界能力范围

Demo MUST只使用已编译的静态 DeterministicCollisionWorldArtifact、fixed capsule Actor contact profile和已声明 KCC capabilities。Rollback Peer Scene MUST复用与本地SandBox相同的通用灰盒移动环境Prefab；该Scene中唯一`DeterministicCollisionWorldAuthoring`及其显式surface marker MUST同时作为可见测试几何和Fixed Collision Artifact的唯一作者来源，Build MUST不创建隐藏临时碰撞世界。Rollback Composition MUST显式要求`WorldFeature.ActorCollision`。UI/文档 MUST明确支持静态世界与双Actor `SolidBodyBlock`，但未支持 Unity Physics、Rigidbody、moving platform、动态破坏、质量/冲量物理和完整竞技网络产品。

#### Scenario: 查看 Demo 能力

- **WHEN** 作者查看Dedicated Relay Server与Client Diagnostics
- **THEN** MUST显示静态几何、fixed capsule、ActorCollision、SolidBodyBlock与双 Actor 限制

#### Scenario: 作者调整通用移动测试环境

- **WHEN** 作者修改共享Prefab中的楼梯、坡面、墙体、门洞、台阶或不平整静态几何并执行Rollback Prepare/Build
- **THEN** Baker MUST从Peer Scene的同一可见Collider层级重新生成CollisionWorldHash
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
