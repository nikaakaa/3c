## MODIFIED Requirements

### Requirement: Character Authoring 必须先编译为 Numeric-Neutral Semantic IR

技能 Frontend MUST以 GameplayAbilityDefinition 为根，只编译其私有 Graph／FSM／Condition／子图引用和 TreeClip 引用的技能图、局部状态与实际领域调用，生成不可变且通过 canonical 编解码和身份校验的 numeric-neutral 技能 IR。IR MUST不包含控制状态机、角色 BodyMotion 或装备总目录，不包含 Pose／Camera／ACL 资源内容。Numeric Target MUST只消费正式已校验技能 artifact，不重新读取 Unity 作者图。Timeline轨道／Clip／MotionWarp直接按正式内容运行，不进入IR；技能IR只保存调用与精确内容引用。独立Timeline产品继续使用同一内容Runtime，不再生成独立Timeline IR。

#### Scenario: 编译 Corin Semantic IR Artifact

- **WHEN** Compiler Frontend 读取 Corin GameplayAbilityDefinition
- **THEN** MUST只生成一份与 Numeric Target 无关且可 canonical 读取的 Semantic IR artifact
- **AND** Float32 与 FixedQ32.32 Target MUST消费该 artifact，不得重新遍历节点生成另一套业务规则

#### Scenario: Frontend Discovery 失败

- **WHEN** 可达 authoring 存在重复 identity、循环引用、缺失 owner 或缺失 Emitter
- **THEN** Frontend MUST在 Semantic Emission 或 artifact publish 前失败并报告精确 source identity
- **AND** MUST不跳过无效元素、读取旧 cache 或调用 Target Compiler


### Requirement: Semantic IR 不得成为第二个 Runtime Interpreter

Gameplay Semantic IR MUST只存在于编译和诊断边界。Unity Editor MAY将当前 canonical artifact 保存为 `Library` generated cache，但 MUST不把它创建为 ScriptableObject、Definition 配置字段、source-controlled authoring asset 或 Player 运行依赖。Runtime Host MUST加载已完成 target lowering 的 GameplayAbilityExecutionData，MUST不在运行时解释 IR、从 IR 临时生成 operation 或在 stale Program 时回退 IR 执行。

#### Scenario: Program Artifact 过期

- **WHEN** Host 发现 Program source revision 或 target manifest 与当前 Definition 不一致
- **THEN** Host MUST拒绝创建 Session
- **AND** MUST不直接解释 Semantic IR、读取 Library cache 或执行 authoring object

#### Scenario: Library Cache 被清理

- **WHEN** 当前 技能数据／表现资源 与 authoring source revision 匹配但 `.csir` cache 不存在
- **THEN** Runtime MUST继续只按 技能数据／表现资源 合同启动
- **AND** Editor 在需要 Target build 或 IR inspection 时 MUST通过正式 Frontend 重建 artifact，不把 cache 缺失解释为 Runtime fallback


### Requirement: Semantic Identity 与 Target Artifact 必须可追溯

技能 IR MUST记录 Ability identity、编译／操作集版本、TickRate、实际技能依赖修订、SemanticHash、能力要求和 canonical 内容身份。Float32／Fixed 技能产物 MUST保留同一语义身份及自己的 NumericProfile／ABI／数据与局部布局 Hash。来源 MUST能定位原图／节点／端口／调用。控制配置、角色授予与纯表现资源 MUST不混入技能内容 Hash；实际 provider 接口在实例绑定时精确检查。

#### Scenario: 比较 Float 与 Fixed Artifact

- **WHEN** 两个 Program 来自相同 source revision 与 Semantic IR artifact 但使用不同 NumericProfile
- **THEN** 两者 MUST具有相同 SemanticHash
- **AND** MUST具有不同 AbilityDataHash 与可能不同的 AbilityLayoutHash

#### Scenario: Semantic IR Cache 身份不匹配

- **WHEN** cache 中的 AbilityId、CompilerVersion、OperationSetVersion、SourceRevision 或 SemanticHash 与当前 build expectation 不一致
- **THEN** artifact loader MUST拒绝该 cache
- **AND** MUST不按 Definition 名称、文件时间或旧 AbilityDataHash 近似接受


### Requirement: Semantic IR Artifact 必须原子生成并可由普通 DotNet 读取

技能 Frontend MUST使用唯一 canonical codec 输出 artifact，并保持同源重复编译一致性、编码／解码和 Hash 检查。发布 MUST以精确 Ability identity 原子替换，普通 .NET MUST读取相同 bytes，不复制 DTO、schema 或 Unity serializer。此次移除的是 Character 编译根，MUST不移除技能错误定位或产物完整性。

#### Scenario: Frontend 重复编译未修改 Definition

- **WHEN** 同一 Definition、CompilerVersion、OperationSetVersion 与 source dependencies 未变化
- **THEN** 两次 Frontend build MUST生成相同 canonical IR bytes 与 SemanticHash
- **AND** artifact store MUST只发布完整通过校验的 bytes

#### Scenario: Artifact 写入中断

- **WHEN** 新 artifact 在 encode、磁盘写入或重新读取校验阶段失败
- **THEN** 当前 cache MUST不被部分文件替换
- **AND** Target build MUST失败而不是读取临时文件或旧版本兼容格式


### Requirement: MotionWarp authoring 必须编译为唯一 numeric-neutral operation

MotionWarpClip MUST作为正式 Timeline 内容保存 Translation Mode、Target Offset Space、目标参数、Rotation Mode／Method、Limit Policy、实际曲线／rate 和对同 owner MotionCurveClip 的稳定引用，不生成 TimelineMotionWarp operation 或到 MotionCurve operation 的引用。技能只保留 Timeline 调用；MotionWarp Runtime 按内容和正式资源绑定执行，Unity对象和 mutable target 不进入 portable 内容。

#### Scenario: 编译带 MotionWarp 的动作 Timeline

- **WHEN** Timeline包含合法MotionCurveClip和引用它的MotionWarpClip
- **THEN** 正式Timeline内容 MUST保留两个独立Clip记录，技能IR不包含它们的operation
- **AND** MotionWarpClip MUST通过稳定引用唯一指向同owner的MotionCurveClip
- **AND** 内容来源定位 MUST能返回两个authoring clip


### Requirement: MotionWarp轨迹solver必须保持Numeric-Neutral

MotionWarp MUST保持唯一数值中立算法和既有源轨迹、目标、空间、限制与累计差值规则。Float32／Fixed MUST从相同正式 Timeline 内容及 RootMotionCurveAsset 映射准备自己的数值绑定，不遍历作者资产、不发明目标专用mode，也不生成另一份时间轴操作程序。累计状态只属于当前 Timeline／动作实例并接入领域快照。

#### Scenario: 同一Corin IR降低两个Numeric Target

- **WHEN** Corin Timeline内容包含SkewToTarget、ApproachDirection与ProgressCurve rotation
- **THEN** Float32与Fixed 内容运行 MUST包含相同业务mode、offset空间、窗口和Limit Policy
- **AND** 两者 MAY使用各自数值常量、curve codec和领域状态格式
- **AND** Fixed Target MUST不降级为旧总残差算法

#### Scenario: IR包含未消费字段

- **WHEN** Translation Mode或Rotation Method不消费某条curve或rate
- **THEN** 内容校验与绑定 MUST拒绝含糊配置或从canonical descriptor中排除该字段
- **AND** Timeline内容Hash MUST不依赖Editor残留的未消费数据


### Requirement: MotionWarp 必须成为两个 Numeric Target 的显式 capability

MotionWarp 内容和资源绑定 MUST声明实际需要的能力。Float32／Fixed 的准备入口 MUST明确支持或拒绝不支持的内容，不依赖操作集中的 TimelineMotionWarp opcode；Network Model 不得忽略不支持的 Warp 内容或改变模式。

#### Scenario: Target backend 缺少 MotionWarp

- **WHEN** 待绑定Timeline内容包含MotionWarpClip
- **AND** 某Numeric Target没有完整实现该运动模式和状态格式
- **THEN** Target内容准备 MUST失败
- **AND** MUST不生成会在运行时跳过Warp的内容运行


### Requirement: MotionWarp source 与 Action Context 必须在 Semantic 阶段闭合

MotionWarp source Clip、调用上下文、激活目标和目标模式要求 MUST在正式 Timeline 内容校验及实例绑定时闭合。缺失、重复或跨 owner 引用 MUST失败；技能 IR 不重新编译 Timeline，不通过源资产相同推断相同 Clip 调用。运行只消费成功绑定的内容。

#### Scenario: Shared Timeline 被普通状态复用

- **WHEN** 一个包含MotionWarp的shared Timeline同时被动作状态和无Action Context状态引用
- **THEN** 内容校验与绑定 MUST拒绝该内容运行
- **AND** MUST不假定运行时只会走合法call site


## REMOVED Requirements

### Requirement: Semantic IR必须表达Character composition roots

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Semantic IR必须使用numeric-neutral Equipment schema

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

## ADDED Requirements

### Requirement: 技能领域引用必须独立于角色目录

技能 IR MUST只保存实际使用的输入、状态、效果、装备或动作接口要求，不内联角色完整配置。绑定 MUST由已安装领域模块解析；未装备但已声明可用的装备能力 MUST仍可按正式授予和模块目录绑定，不能通过运行时目录扫描猜测技能。

#### Scenario: 技能请求装备变化
- **WHEN** 技能包含正式装备请求节点
- **THEN** 技能数据 MUST保存请求语义和引用要求，装备模块唯一拥有目录和状态
- **AND** 装备目录改变 MUST更新对应玩法身份而非复制全部技能数据
