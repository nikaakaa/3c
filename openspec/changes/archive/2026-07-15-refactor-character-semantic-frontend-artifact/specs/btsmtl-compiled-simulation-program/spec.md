## MODIFIED Requirements

### Requirement: Character authoring 必须按显式 Numeric Target 生成 Simulation Program

系统 MUST先以 CharacterPipelineDefinition 为唯一编译根运行 Character Semantic Frontend，生成经过 canonical 校验的 Gameplay Semantic IR artifact，再由显式 Numeric Target 生成 CharacterSimulationProgram。Target Compiler 的正式输入 MUST是 validated artifact，MUST不接收 CharacterPipelineDefinition、Graph、Node、Timeline、Unity object 或 Frontend 私有 discovered model。每个 target artifact MUST只包含一个 NumericProfile；同一 source MAY为不同 target 生成不同 Program，但 MUST不重新实现或改变 Semantic IR operation。Runtime MUST不直接从 authoring object 或 Semantic IR 创建 gameplay runtime clone。

#### Scenario: 编译 Corin Float32 Program

- **WHEN** 作者编译 Corin CharacterPipelineDefinition
- **THEN** Frontend MUST先发布一份 validated Semantic IR artifact，Float32 Target MUST只从该 artifact 生成 Program
- **AND** Runtime MUST不递归 clone RootTree、StateMachine 或 Timeline graph

#### Scenario: Target 收到未校验的内存 IR

- **WHEN** 调用方尝试绕过 artifact codec，把任意 `CharacterGameplaySemanticIr` 对象直接交给正式 Target 入口
- **THEN** 编译 API MUST不提供该公共路径
- **AND** MUST不因对象来自当前 Editor 进程就视为合法 build input

## ADDED Requirements

### Requirement: Program 与 Projection 必须在同一 Build Transaction 中发布

Character Simulation build MUST按 `Frontend artifact -> Numeric Target Program -> Presentation Projection -> identity validation -> asset publish` 顺序执行。Semantic IR artifact、Program 与 Projection MUST共享精确 ProgramId、SourceRevision、SemanticHash 与 operation/producer source identity；Projection MUST再匹配目标 ProgramHash。任一阶段失败时 MUST不把本次 Program 或 Projection 作为成功产物发布，也 MUST不更新一半 generated reference。

#### Scenario: Projection Producer 缺失

- **WHEN** Float32 Program 已成功生成但 Projection 缺少 Semantic IR 中声明的 animation producer
- **THEN** 整个 build transaction MUST失败
- **AND** Definition MUST不绑定新 Program 与旧 Projection 的混合组合

#### Scenario: 自动 stale build

- **WHEN** Asset 变更使 Definition source revision 过期并触发自动 rebuild
- **THEN** 自动 rebuild MUST调用与显式 Compile 命令相同的 Frontend、artifact、Target 和 Projection transaction
- **AND** MUST不使用只在自动路径存在的内存 lowering 或旧 Program fallback

### Requirement: Compiler Diagnostics 与 Agent 必须复用正式 Frontend 和 Target 阶段

Definition diagnostics、Agent validator 和其它 Editor caller MAY执行不发布 Program/Projection 的 dry-run，但 MUST复用正式 Authoring Discovery、Semantic Emission、artifact codec 和 Target Compiler。Dry-run result MUST以 artifact descriptor/identity 和分阶段 report 表达 Semantic 成功，不得依赖旧 `CharacterSimulationCompileResult.SemanticIr` 直通对象，也不得维护第二个 validator operation table。

#### Scenario: Agent 校验 Corin Patch

- **WHEN** Agent validator 对修改后的 Corin authoring 执行正式编译校验
- **THEN** MUST通过同一 Frontend 生成并校验 Semantic artifact payload
- **AND** MUST不自行发射 Semantic operations 或直接调用 raw Float32 lowerer
