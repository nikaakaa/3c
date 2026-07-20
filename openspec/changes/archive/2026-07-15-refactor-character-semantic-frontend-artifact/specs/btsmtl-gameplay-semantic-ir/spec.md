## MODIFIED Requirements

### Requirement: Character Authoring 必须先编译为 Numeric-Neutral Semantic IR

Compiler Frontend MUST以 CharacterPipelineDefinition 为唯一根，将全部可达 Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 MotionCurve 编译为不可变 Gameplay Semantic IR。Frontend MUST先完成稳定 Authoring Discovery，再从唯一 discovered model 执行 Semantic Emission，并产出经过 canonical encode、decode、header 与 SemanticHash 校验的正式 artifact。IR MUST表达稳定 source identity、operation 语义、控制流、状态声明、数值字面量、producer identity 和能力要求；IR MUST不保存 Unity object、Float32 runtime value、Fixed runtime value、Network Model 或 mutable runtime state。Numeric Target MUST消费该 validated artifact，不得重新遍历 authoring 或直接接收 CharacterPipelineDefinition。

#### Scenario: 编译 Corin Semantic IR Artifact

- **WHEN** Compiler Frontend 读取 Corin CharacterPipelineDefinition
- **THEN** MUST只生成一份与 Numeric Target 无关且可 canonical 读取的 Semantic IR artifact
- **AND** Float32 与未来 Fixed Target MUST消费该 artifact，不得重新遍历节点生成另一套业务规则

#### Scenario: Frontend Discovery 失败

- **WHEN** 可达 authoring 存在重复 identity、循环引用、缺失 owner 或缺失 Emitter
- **THEN** Frontend MUST在 Semantic Emission 或 artifact publish 前失败并报告精确 source identity
- **AND** MUST不跳过无效元素、读取旧 cache 或调用 Target Compiler

### Requirement: Semantic IR 不得成为第二个 Runtime Interpreter

Gameplay Semantic IR MUST只存在于编译和诊断边界。Unity Editor MAY将当前 canonical artifact 保存为 `Library` generated cache，但 MUST不把它创建为 ScriptableObject、Definition 配置字段、source-controlled authoring asset 或 Player 运行依赖。Runtime Host MUST加载已完成 target lowering 的 CharacterSimulationProgram，MUST不在运行时解释 IR、从 IR 临时生成 operation 或在 stale Program 时回退 IR 执行。

#### Scenario: Program Artifact 过期

- **WHEN** Host 发现 Program source revision 或 target manifest 与当前 Definition 不一致
- **THEN** Host MUST拒绝创建 Session
- **AND** MUST不直接解释 Semantic IR、读取 Library cache 或执行 authoring object

#### Scenario: Library Cache 被清理

- **WHEN** 当前 Program/Projection 与 authoring source revision 匹配但 `.csir` cache 不存在
- **THEN** Runtime MUST继续只按 Program/Projection 合同启动
- **AND** Editor 在需要 Target build 或 IR inspection 时 MUST通过正式 Frontend 重建 artifact，不把 cache 缺失解释为 Runtime fallback

### Requirement: Semantic Identity 与 Target Artifact 必须可追溯

Semantic IR artifact MUST记录 ProgramId、CompilerVersion、OperationSetVersion、TickRate、SourceRevision、SemanticHash、capability manifest 与 canonical payload identity。每个 target Program manifest MUST记录同一 SemanticHash、source revision、compiler version、operation-set version、NumericProfile 和 required world capabilities；Program source map MUST能从 target operation、constant、state slot 和 producer 追溯回同一 Semantic IR/source identity。Artifact 路径、显示名或缓存时间 MUST不参与业务 identity。

#### Scenario: 比较 Float 与 Fixed Artifact

- **WHEN** 两个 Program 来自相同 source revision 与 Semantic IR artifact 但使用不同 NumericProfile
- **THEN** 两者 MUST具有相同 SemanticHash
- **AND** MUST具有不同 ProgramHash 与可能不同的 LayoutHash

#### Scenario: Semantic IR Cache 身份不匹配

- **WHEN** cache 中的 ProgramId、CompilerVersion、OperationSetVersion、SourceRevision 或 SemanticHash 与当前 build expectation 不一致
- **THEN** artifact loader MUST拒绝该 cache
- **AND** MUST不按 Definition 名称、文件时间或旧 ProgramHash 近似接受

## ADDED Requirements

### Requirement: Semantic IR Artifact 必须原子生成并可由普通 DotNet 读取

Frontend MUST使用 Core 中唯一 canonical codec 生成 Semantic IR artifact，并在发布前完成两次未修改 authoring 编译的 canonical bytes 比较、encode/decode round-trip 与 SemanticHash 校验。Unity artifact store MUST使用临时文件与原子替换发布当前 Definition cache；普通 .NET 项目 MUST从同一 portable Core source set读取相同 bytes，不复制 Unity serializer、DTO 或 schema。

#### Scenario: Frontend 重复编译未修改 Definition

- **WHEN** 同一 Definition、CompilerVersion、OperationSetVersion 与 source dependencies 未变化
- **THEN** 两次 Frontend build MUST生成相同 canonical IR bytes 与 SemanticHash
- **AND** artifact store MUST只发布完整通过校验的 bytes

#### Scenario: Artifact 写入中断

- **WHEN** 新 artifact 在 encode、磁盘写入或重新读取校验阶段失败
- **THEN** 当前 cache MUST不被部分文件替换
- **AND** Target build MUST失败而不是读取临时文件或旧版本兼容格式
