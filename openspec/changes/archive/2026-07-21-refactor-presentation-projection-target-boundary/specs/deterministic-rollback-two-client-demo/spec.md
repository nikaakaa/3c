## MODIFIED Requirements

### Requirement: Demo 必须复用 Corin 同一 Gameplay Semantic Artifact

两端 MUST使用与单机/ServerAuthoritative相同SourceRevision/SemanticHash的Corin `.csir`，并由Fixed Target生成相同Fixed Program。Fixed ProgramHash MAY且通常 MUST不同于Float32 ProgramHash。Rollback Presentation MUST通过正式Fixed Adapter生成与Frontend相同的`CharacterPresentationSemanticContract`并复用唯一target-neutral Projection，MUST不生成或加载Float32 Program作为Projection前置依赖。业务覆盖移动、转身、闪避、Run、Attack1/Attack2、连段、打断、Timeline TreeClip Window、motion curve和GameplayEffect。系统 MUST不使用rollback专用节点、业务图、第二semantic evaluator或第二Projection。

#### Scenario: 迟到 Combo Input

- **WHEN** Attack2 request的canonical input迟到
- **THEN** 两端 MUST通过相同Fixed Program restore/replay得到相同Action/Timeline state

#### Scenario: 修改 Corin Authoring 后构建 Rollback Player

- **WHEN** 作者修改BTSMTL、Timeline或其它Corin Character Definition依赖后执行Rollback Build
- **THEN** Build入口 MUST先从当前Definition重新生成validated Semantic IR、Presentation contract与target-neutral Projection
- **AND** MUST由唯一Fixed Target Adapter从同一Semantic IR生成Fixed Program artifact
- **AND** MUST在Player Build前精确校验ProgramId、SourceRevision、SemanticHash、ContractHash与ordered producer contract
- **AND** 任一身份不一致 MUST拒绝构建，MUST不复用旧Fixed Program、旧Projection或Float32 Projection前置产物

#### Scenario: Fixed-only Rollback产品发布

- **WHEN** Deterministic Rollback Product只声明Fixed Numeric Target
- **THEN** 公共Build Orchestrator MUST只发布Fixed Program与同一target-neutral Projection
- **AND** MUST不调用Float32 Target Compiler或写入Float32 Program wrapper
