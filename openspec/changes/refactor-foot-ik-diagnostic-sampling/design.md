## Context

见proposal.md。Foot的正式输入、Landing、Motion、Ground Path、Goal、Pelvis、FBBIK与Frame lineage已经在成功Seal后的同步Commit边界同时可读。旧采样又建立采样专用结构、按Side选择左右脚、逐字段搬运到Column并手写CSV，导致同一事实拥有第二套结构和Schema。

通用Generated Diagnostic Sampling已经提供多Fact Root Capability、path-scoped DiagnosticField、Roslyn Source Generator、typed packet、Lifecycle和Schema-driven Host基础。3C当前已经完成真实字段标记、Foot Program生成与薄Capture编译；Host、Analyzer／Publisher、Performance接线和真实Player剥离门禁尚未闭合。

## Goals / Non-Goals

**Goals:**

- 直接采样现有业务事实，不创建采样专用数据结构或逐字段复制。
- 一个Dimension允许同时传入多个强类型Fact Root，并让左右Dimension共享同一Schema。
- 普通字段只由真实成员上的一个DiagnosticField声明。
- Ground Contact、Envelope和Surface直接使用真实class page表。
- 编译期生成直接成员访问和HandleCommitted，运行时不反射、不解释字段路径。
- 通用Host拥有基础CSV与manifest；Foot只在下游拥有评分、诊断与报告。
- Disabled Player从编译闭包中剥离生成代码、Runtime、Foot Diagnostics和Annotations引用。

**Non-Goals:**

- 不修改Foot、Pelvis、Goal、FBBIK或Final Publication业务数学。
- 不让PoseGraph认识Capability、Sampler、packet、Host或采样状态。
- 不保留三个领域生命周期Event、Side metadata、Projection、Getter、Column、CsvBinding、领域Host Adapter或旧合成geometry表。
- 不在Player运行时计算Analyzer评分或报告公式。
- 不提供旧Schema兼容reader、fallback或运行时开关。

## Decisions

### Decision 1: Capability直接声明多个Fact Root

character-foot-ik Capability通过DiagnosticFactRoot注册现有事实类型。当前根包含effector、foot、formal-input、formal-output、frame、input、leg、pelvis、pelvis-goal、primary-support、solver和stride；Capture Metadata只保存采样会话固定身份。无字段的foot-steps根及其AnimationBiomechanicalStepReadPage构造已删除。

Dimension不是新的业务类型。Left和Right只是同一个Generated Program的两个参数集合：每套集合都按相同Fact Root id传入对应脚事实，共享pelvis、solver、stride等双脚公共事实。左右结构由生成器在编译期统一确定，不存在Metadata Side分支，也不存在两套字段identity。

采用多Fact Root而不是合并结构，是因为合并结构必然要求为采样构造和复制字段；直接传根既保持业务内存布局，也允许角色、战斗、网络和动画以后复用同一框架。

### Decision 2: 真实成员单Attribute是唯一字段声明

现有readonly field或getter-only成员上的DiagnosticField声明revision、unit、groups和可选availability。Field identity按以下路径生成：

Capability / main或table / Fact Root / 递归成员路径

Generator从成员类型推断Boolean、整数、浮点、Identity、Vector和Quaternion codec。Foot不维护字段常量表、Getter、Extractor、Projection、Column、CsvBinding或第二份单位清单。

本次Foot Schema不保留DiagnosticDerivedField。需要跨字段计算的Envelope统计、穿透、评分与报告属于采集完成后的领域分析，由Analyzer读取生成产物后计算。这样Player只采集已经提交的真实事实，不为报告逻辑增加运行时计算和Fact Root依赖。

### Decision 3: Ground Geometry拆成三张真实page表

CharacterFootGroundPathDiagnostics直接暴露现有CharacterFootGroundContactPage、CharacterFootGroundEnvelopePage和CharacterFootGroundSurfacePage。三个page提供Count与只读索引器，Generator同步读取：

| Table | Capacity | Row |
| --- | ---: | --- |
| ground-contacts | 64 | CharacterFootGroundContact |
| ground-envelope | 68 | CharacterFootGroundEnvelopeVertex |
| ground-surfaces | 512 | CharacterFootGroundSurfaceSegment |

行真实成员使用DiagnosticField，类型直接推断。主表的Frame、Completion、Dimension和Metadata通过packet与manifest关联，不再复制到每一行；行号由表顺序表达，不再生成contact-index、envelope-index和surface-index字段。

选择三张表而不是旧max(count)合成表，是因为三种集合容量、行类型和业务含义不同。合成表需要逐列判断与默认值，正是本次删除的映射层。

### Decision 4: Roslyn生成Lifecycle与HandleCommitted

Foot只声明DiagnosticSampler和DiagnosticCaptureProgram。Roslyn在编译期完成：

1. 遍历全部Fact Root与Metadata成员图。
2. 校验重复Field identity、非法成员类型、循环路径、availability和Table shape。
3. 合并Sampler选择的字段与三张表。
4. 为Left和Right生成同构的直接成员访问。
5. 生成Schema、typed packet layout、Lifecycle和HandleCommitted。

CharacterFootIkGeneratedCapture是领域内唯一薄接点。Runtime在成功Seal后的同步Commit边界从Constraint、Final Publication和当前Clip Player的已提交状态取得现有事实，并把Left／Right与公共Fact Root直接传给它；它一次调用Generated Lifecycle的HandleCommitted。采样链不读取Runtime Snapshot或CharacterFootIkCommittedCaptureViewLease。Generated Program把值写入framework-owned packet后，后台不再持有业务对象或page。

这里不使用Expression、反射、dynamic、字典或字符串成员路径。生成的是普通C#，由Unity C# Compiler和IL2CPP AOT处理。

### Decision 5: 基础产物与领域分析分层

通用Host读取sealed packet和生成Schema，自动产生：

- 每个Sampler主表CSV。
- ground-contacts、ground-envelope与ground-surfaces子表CSV。
- Schema、Sampler和Capability manifest。
- 文件hash、Frame范围、Dimension和Program identity。

Host不认识Foot字段。Foot Analyzer／Publisher在Capability基础产物Completed后只读这些文件，计算Foot评分、规则诊断、明细和报告。Analyzer不得维护另一套字段映射，Publisher不得参与Capture状态或回写manifest。

该部分仍未实施完成；当前编译通过只证明字段、Program与Capture入口成立，不代表基础CSV、评分或报告已经闭环。

### Decision 6: Disabled通过编译闭包剥离

Capture构建同时定义KK_DIAGNOSTIC_SAMPLING与KK_DIAGNOSTIC_FOOT。普通发布不定义：

- Conditional DiagnosticField不写入业务程序集metadata。
- Generator不生成Foot Program。
- Foot Diagnostics asmdef和Sampling Runtime不进入Player。
- Annotations不得因Attribute源码引用成为Player根程序集。

关闭不能依赖runtime bool、空实现、Linker猜测或后处理删除。最终由Cecil检查Managed程序集引用和custom attribute，并由IL2CPP产物检查Generated Program、Runtime、Session、packet、queue、Capability／Field identity字符串为零。

## Risks / Trade-offs

- [真实class page在业务内部可写] → GeneratedCapture只在同步Commit调用栈内调用HandleCommitted，packet提交后不持有page引用。
- [多个Fact Root左右绑定错位] → Generated HandleCommitted固定左右参数顺序和相同root集合，Program编译与调用编译共同失败，不按Side运行时选择。
- [path-scoped成员改名改变Field identity] → 把改名视为Schema破坏性变更并生成新Program identity；不提供旧名别名。
- [三张表改变旧CSV形状] → Analyzer必须按新manifest消费，历史包保持不可变，不兼容读取。
- [Host或Analyzer尚未接线] → tasks保持未完成，不把字段与Capture编译成功描述为完整采样闭环。
- [Annotations源码引用被错误保留到发布] → Player Gate同时检查Attribute metadata、AssemblyRef和IL2CPP identity字符串，不依赖Conditional语义推测。

## Migration Plan

1. 完成多Fact Root Capability、真实成员Attribute、三个真实page表、Full Sampler和Program。
2. 删除全部旧Getter／Extractor、Projection、领域Derived、Side选择、三个Event、旧合成geometry表及Column／CsvBinding链。
3. 用薄GeneratedCapture在成功Seal后的同步Commit边界绑定Left／Right根并调用HandleCommitted，不经过Capture View。
4. 接入通用Host生成主表、三张子表和manifest。
5. 迁移Analyzer／Publisher，只保留领域公式、评分和报告。
6. 接入唯一Performance Build、Run、Capture与Comparer身份。
7. 对Disabled和Capture真实Player执行Cecil／IL2CPP硬门禁。
8. 用户完成端到端验收后再同步current specs并归档；本次文档更新不归档。

## Current Implementation Status

- 已完成并编译：多Fact Root Capability、真实成员单Attribute、三张真实class page表、Full Sampler、Capture Program、Generated Lifecycle、成功Seal后直接事实根绑定、左右HandleCommitted调用和薄GeneratedCapture。
- 已删除：旧字段Getter／Extractor、Projection、领域Derived、Dimension类型、三个Event、Metadata Side、旧合成Ground Geometry表。
- 未完成：通用Host在3C中的基础产物闭环、Foot Analyzer／Publisher迁移、Performance工作流接线、Disabled／Capture Player的Cecil与IL2CPP Gate。
