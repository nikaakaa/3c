## Why

当前Foot IK采样正在从手写字段Getter、左右脚Side选择、单体CSV列绑定和诊断DTO迁移到通用Generated Diagnostic Sampling。采样输入已经存在于成功提交后的PostCommit只读事实中；继续为采样复制View、Projection或字段映射既增加热路径成本，也让IL2CPP AOT、Schema一致性和Disabled发布剥离无法可靠证明。

## What Changes

- Foot声明一个character-foot-ik Capability，并直接注册foot、input、formal-input、formal-output、effector、leg、pelvis、pelvis-goal、solver、stride、primary-support与frame等多个强类型Fact Root。框架只认识Capability、Dimension、Fact Root、Metadata、Field和Table，不认识Foot领域。
- 需要采样的现有真实readonly成员只添加一个path-scoped DiagnosticField。稳定Field identity由Capability／主表或子表／Fact Root／成员路径生成，类型与codec由CLR成员类型推断；删除Foot的Projection、诊断DTO、逐字段Getter／Extractor、Column与CsvBinding。
- Foot不保留运行时DiagnosticDerivedField；旧统计、七维评分、规则报告、Diagnosis Store或Publisher不迁入Generated Capture热路径。通用Host产物是唯一基础采样输出，独立`add-schema-driven-diagnostic-analysis`后续只读这些Completed产物，把旧规则语义迁成Host-only Operator、Plan和报告。
- Ground Geometry不再通过最大行数把Contact、Envelope和Surface拼成一张合成表。现有真实class page分别声明ground-contacts、ground-envelope和ground-surfaces三张固定容量表，行真实成员仍只添加DiagnosticField。
- Foot声明Core／Full Sampler和Capture Program。Roslyn Source Generator在编译期合并所有Fact Root的Schema，生成左右维度共用的直接成员访问、typed packet layout、目标隔离`DiagnosticEvent` dispatcher、可选partial interest Query和匹配Program handler；Unity编译器与IL2CPP AOT编译生成的普通C#。
- Runtime在成功Seal后的同步Commit边界只调用一行带`DiagnosticEvent`的private static partial void方法，以`in`传入目标、真实lineage及已经存在的Left／Right与公共事实根。Generator handler拥有左右展开、rent、Capture、submit与Fault，Host workflow在Start冻结Metadata并拥有Start／Stop。昂贵Foot事实只在帧开始的target interest为真时准备。采样链删除`ICharacterFootIkCommittedCaptureConsumer`、Capture Binding和手写`CharacterFootIkGeneratedCapture.TryCapture`转发，也不创建或消费CharacterFootIkCommittedCaptureViewLease、采样View、DTO、字段副本、Side分派、Host Adapter或第二Writer。
- 通用Host负责从sealed packet与生成Schema自动输出主表、三张Ground子表、Sampler manifest和Capability manifest。采样Launcher、固定输入回放和MCP只通过统一workflow消费这些生成产物，不恢复Foot字段映射；`add-schema-driven-diagnostic-analysis`在同一workflow之后提供显式Analyze Last／Existing和独立诊断结果，不建立第二Capture manifest。
- **BREAKING** 删除三个Foot typed Event、Left／Right Dimension View／Metadata配对、Metadata Side、旧合成Ground Geometry表和旧Schema兼容读取。新产物使用新的Schema与Program identity；历史封存包保持不可变。
- Disabled Player不定义KK_DIAGNOSTIC_SAMPLING与KK_DIAGNOSTIC_FOOT：Generator零输出，Sampling Runtime与Foot Diagnostics程序集不进入Player，Conditional Attribute不进入业务metadata，Annotations不得成为Player根引用。Cecil与IL2CPP Gate必须检查零Attribute、零Sampling AssemblyRef、零Generated Program／Session／packet／queue／identity字符串。

## Capabilities

### New Capabilities

- character-foot-ik-diagnostic-sampling: 定义Foot IK如何把既有PostCommit多来源事实交给通用生成程序，并由通用Host产出独立离线诊断可消费的基础采样文件。

### Modified Capabilities

- character-foot-placement-presentation: 规定采样只读取消费成功提交后的现有事实，不要求PoseGraph或Foot为了采样构造View、DTO、Event或第二事实页。

## Impact

- Runtime事实：现有Foot、Landing、Motion、Goal、Pelvis、FBBIK、Final Publication与Ground page真实成员只增加Conditional Annotations；不改变业务结果、算法、执行顺序或内存布局。
- Foot采样插件：保留Capability、Sampler、Program、Metadata和一行Post-Commit `DiagnosticEvent` partial触发；删除手写GeneratedCapture／Consumer／Binding、旧Getter、Projection、Dimension View、Event DTO、Side选择、合成Geometry表、Column／CsvBinding、领域Writer与Host Adapter。
- 通用框架：唯一拥有Roslyn Generator、Schema、Lifecycle、typed packet、Session、Writer、Host Finalizer、CSV和manifest。
- Editor工具：唯一workflow registry连接Launcher、固定输入回放与MCP；旧Sampler、单体Analyzer／Publisher和报告存储已从采样链删除，新的Host-only领域分析由`add-schema-driven-diagnostic-analysis`负责。
- 构建：现有multi-root Runtime、Program、Host与Editor workflow已经通过编译；`add-schema-driven-diagnostic-analysis`仍需用generated Event handler替换手写GeneratedCapture，Disabled／Capture真实Player硬门禁仍未完成。
- 与current spec对比：character-foot-placement-presentation、character-animation-pipeline与character-pose-graph-runtime-architecture继续只承诺成功Seal后的唯一同步Commit边界；`DiagnosticEvent`调用属于条件采样接点，不改变PoseGraph业务合同，其中保留的Capture View只服务Live／Trace／Gizmo。
