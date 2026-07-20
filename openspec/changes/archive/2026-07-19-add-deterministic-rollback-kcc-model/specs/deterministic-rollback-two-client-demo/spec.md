# deterministic-rollback-two-client-demo Specification

## ADDED Requirements

### Requirement: Demo 必须使用两个客户端与一个 Canonical Input Host

系统 MUST提供一个本地 Demo 组合，启动两个独立客户端与一个 canonical input host/relay。两端 MUST加载相同 ModelId、SemanticHash、Fixed ProgramHash、TickRate、CollisionWorldHash、KccId 和 stable actor roster。

#### Scenario: 两端 Handshake

- **WHEN** Client A 与 Client B 加入 Demo
- **THEN** Host MUST校验全部 deterministic identities 后才允许 SimulationTick 推进

#### Scenario: Demo 使用正式 Canonical Input 延迟

- **WHEN** Host开始推进双Actor canonical clock
- **THEN** MUST使用显式4 Tick input delay与独立Host confirmation frontier
- **AND** Peer本地预测 MUST不等待该delay才响应输入

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

Demo MUST只使用已编译的静态 DeterministicCollisionWorldArtifact、fixed capsule Actor contact profile和已声明 KCC capabilities。Rollback Composition MUST显式要求`WorldFeature.ActorCollision`。UI/文档 MUST明确支持静态世界与双Actor `SolidBodyBlock`，但未支持 Unity Physics、Rigidbody、moving platform、动态破坏、质量/冲量物理和完整竞技网络产品。

#### Scenario: 查看 Demo 能力

- **WHEN** 作者查看 Demo Host/Diagnostics
- **THEN** MUST显示静态几何、fixed capsule、ActorCollision、SolidBodyBlock与双 Actor 限制

#### Scenario: 一个 Peer 冲刺撞向另一个 Actor

- **WHEN** Peer A的闪避或Timeline motion在一个Tick内会穿过Peer B
- **THEN** 两端 MUST由同一Fixed KCC batch阻挡或沿接触切向滑动
- **AND** rollback/replay后 MUST保持相同Actor Body、WorldHash与KCC hash

### Requirement: Demo 必须暴露 Rollback 与 Desync 诊断

Demo MUST只读显示 predicted/confirmed tick、input delay、late input、rollback count/depth、replayed ticks、world/actor/KCC hash、desync scope、snapshot recovery 和 presentation keep/replace/cancel。Diagnostics MUST不修改 simulation result。

#### Scenario: 发生一次 Rollback

- **WHEN** canonical bundle 替换了旧 predicted input
- **THEN** diagnostics MUST记录起始 Tick、depth、replayed ticks 和 replay 后 hash
