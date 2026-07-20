# btsmtl-gameplay-semantic-ir Specification

## Purpose
定义 Character authoring 到 numeric-neutral Gameplay Semantic IR 的唯一 Frontend、canonical artifact、稳定身份和非运行时边界，使不同 Numeric Target 共享业务语义而不共享目标 ABI。
## Requirements
### Requirement: Character Authoring 必须先编译为 Numeric-Neutral Semantic IR

Compiler Frontend MUST以 CharacterPipelineDefinition 为唯一根，将全部可达 Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 MotionCurve 编译为不可变 Gameplay Semantic IR。Frontend MUST先完成稳定 Authoring Discovery，再从唯一 discovered model 执行 Semantic Emission，并产出经过 canonical encode、decode、header 与 SemanticHash 校验的正式 artifact。IR MUST表达稳定 source identity、operation 语义、控制流、状态声明、数值字面量、producer identity 和能力要求；IR MUST不保存 Unity object、Float32 runtime value、Fixed runtime value、Network Model 或 mutable runtime state。Numeric Target MUST消费该 validated artifact，不得重新遍历 authoring 或直接接收 CharacterPipelineDefinition。

#### Scenario: 编译 Corin Semantic IR Artifact

- **WHEN** Compiler Frontend 读取 Corin CharacterPipelineDefinition
- **THEN** MUST只生成一份与 Numeric Target 无关且可 canonical 读取的 Semantic IR artifact
- **AND** Float32 与 FixedQ32.32 Target MUST消费该 artifact，不得重新遍历节点生成另一套业务规则

#### Scenario: Frontend Discovery 失败

- **WHEN** 可达 authoring 存在重复 identity、循环引用、缺失 owner 或缺失 Emitter
- **THEN** Frontend MUST在 Semantic Emission 或 artifact publish 前失败并报告精确 source identity
- **AND** MUST不跳过无效元素、读取旧 cache 或调用 Target Compiler

### Requirement: Semantic IR Operation 必须由唯一 Emitter 定义

每个可执行 authoring type MUST由唯一 Frontend Emitter 生成 Semantic IR operation。Emitter MUST不读取 Local、ServerAuthoritative、Rollback、WorldSolver concrete type 或 Numeric Target 来改变业务控制流。Target 不支持某个 operation 时 MUST在 lowering 阶段明确失败，MUST不跳过 operation、改写规则或调用旧 runtime。

#### Scenario: Fixed Target 不支持某个 Operation

- **WHEN** FixedQ32.32 Target 无法降低 Semantic IR 中的某个 operation
- **THEN** target build MUST报告 operation source identity 与缺失 capability
- **AND** MUST不生成 Rollback 专用节点或使用 Float32 evaluator fallback

### Requirement: Semantic 数值字面量必须保持来源与精确降低边界

Frontend MUST以 canonical source literal 保存 authoring 数值及其 source identity，不得在 IR 阶段提前量化为公共定点格式。Numeric Target MUST负责将 literal 转为自己的 scalar/vector 表示，并报告非法值、超范围、舍入或不支持的精度要求。

#### Scenario: 同一 MotionCurve 降低到不同 Target

- **WHEN** 同一 Semantic IR 分别交给 Float32 Target 与 FixedQ32.32 Target
- **THEN** 两个 Target MUST从同一 source literal 独立生成各自 constant
- **AND** Fixed lowering 的量化误差 MUST不改变 Float32 Program constant

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

### Requirement: Semantic IR Value输入必须遵守版本化Port Contract

Character Gameplay Operation Set MUST为每个operation code声明numeric-neutral且版本化的Value input/output port contract。Contract MUST描述稳定port identity、canonical order、固定value kind或受约束kind group及允许转换。Semantic Frontend MUST使用该contract解析linked Value edge和未连接input constant，并 MUST在validated Semantic IR中保存每个constant input的target operation、target port、constant index与resolved value kind。`ProgramControlFlowEdge(kind=Value)` MUST继续是linked input唯一真值；系统 MUST不保存第二份linked binding或依赖constant identity字符串推导端口。

#### Scenario: Linked Value输入通过合同解析

- **WHEN** 一个InputScalar operation连接到Compare的Left端口
- **THEN** Frontend MUST通过Operation Set合同解析source output和target input
- **AND** Semantic IR MUST保留该Value edge且确认其resolved kind满足Compare约束

#### Scenario: 未连接输入使用constant

- **WHEN** Compare的Right端口没有Value edge并在authoring中保存数值常量
- **THEN** Semantic IR MUST生成该literal及指向Compare/Right的结构化constant input binding
- **AND** constant identity MUST不承担端口寻址语义

#### Scenario: 受约束多态端口完成解析

- **WHEN** Compare、And、Or、Not、BlackboardGet或BlackboardSet使用由上下文决定的Value kind
- **THEN** Frontend MUST按operation contract、declaration reference和literal kind解析出确定的Semantic value kind
- **AND** MUST不使用Unknown、Object、运行时反射或Target专用类型作为成功结果

#### Scenario: Value来源或类型冲突

- **WHEN** 同一target port存在两个Value edge、同时存在Value edge与constant binding、source output不存在或value kind不兼容
- **THEN** Semantic artifact build MUST失败并报告source operation、target operation与port identity
- **AND** MUST不发布近似IR、跳过binding或延迟到Runtime猜测

### Requirement: MotionWarp authoring 必须编译为唯一 numeric-neutral operation

Frontend MUST为每个合法MotionWarpClip生成唯一`TimelineMotionWarp` Semantic operation，并保存position/rotation mode、target offset、weight、clamp、两条canonical progress curve、Timeline/Action Context provenance及到源MotionCurve operation的typed reference。IR MUST不保存Unity Transform、GameObject、AnimationCurve对象或Solver类型。

#### Scenario: 编译带 MotionWarp 的动作 Timeline

- **WHEN** Timeline包含合法MotionCurveClip和引用它的MotionWarpClip
- **THEN** Semantic IR MUST包含两个独立operation
- **AND** MotionWarp operation MUST通过typed reference唯一指向MotionCurve operation
- **AND** SourceMap MUST能返回两个authoring clip

### Requirement: MotionWarp 必须成为两个 Numeric Target 的显式 capability

Operation Set MUST声明MotionWarp operation schema、reference、state requirement与canonical modifier顺序。Float32和Fixed Target MUST显式声明支持或在Target编译时拒绝整个Program；系统 MUST不允许某个Network Model在runtime忽略未知Warp operation。

#### Scenario: Target backend 缺少 MotionWarp

- **WHEN** validated Semantic IR包含TimelineMotionWarp
- **AND** 某Numeric Target没有完整实现该operation和state schema
- **THEN** Target编译 MUST失败
- **AND** MUST不生成会在运行时跳过Warp的Program

### Requirement: MotionWarp source 与 Action Context 必须在 Semantic 阶段闭合

Frontend MUST验证MotionWarp source、Timeline owner、窗口、Action channel、Override语义、Action Context call site与ActionProfile target requirement。shared Timeline被多个TimelineNode引用时，每个可执行call site MUST满足同一要求；任一call site缺少Action Context MUST使编译失败。

#### Scenario: Shared Timeline 被普通状态复用

- **WHEN** 一个包含MotionWarp的shared Timeline同时被动作状态和无Action Context状态引用
- **THEN** Frontend MUST拒绝该Program
- **AND** MUST不假定运行时只会走合法call site
