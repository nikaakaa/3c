## Why

当前Foot IK采样正在从手写字段Getter、左右脚Side选择、单体CSV列绑定和诊断DTO迁移到通用Generated Diagnostic Sampling。采样输入已经存在于成功提交后的PostCommit只读事实中；继续为采样复制View、Projection或字段映射既增加热路径成本，也让IL2CPP AOT、Schema一致性和Disabled发布剥离无法可靠证明。

## What Changes

- Foot声明一个character-foot-ik Capability，并直接注册foot、input、formal-input、formal-output、effector、leg、pelvis、pelvis-goal、solver、stride、primary-support与frame等多个强类型Fact Root。框架只认识Capability、Dimension、Fact Root、Metadata、Field和Table，不认识Foot领域。
- 需要采样的现有真实readonly成员只添加一个path-scoped DiagnosticField。稳定Field identity由Capability／主表或子表／Fact Root／成员路径生成，类型与codec由CLR成员类型推断；删除Foot的Projection、诊断DTO、逐字段Getter／Extractor、Column与CsvBinding。
- Foot不保留运行时DiagnosticDerivedField。不能直接从Committed成员读取的统计、评分和报告公式由Host完成基础产物后交给Analyzer／Publisher计算，不进入Player字段提取链。
- Ground Geometry不再通过最大行数把Contact、Envelope和Surface拼成一张合成表。现有真实class page分别声明ground-contacts、ground-envelope和ground-surfaces三张固定容量表，行真实成员仍只添加DiagnosticField。
- Foot声明Full Sampler和Capture Program。Roslyn Source Generator在编译期合并所有Fact Root的Schema，生成左右维度共用的直接成员访问、typed packet layout、Lifecycle和HandleCommitted；Unity编译器与IL2CPP AOT编译生成的普通C#。
- Foot只保留薄CharacterFootIkGeneratedCapture：Runtime在成功Seal后的同步Commit边界直接取得已经存在的Left／Right与公共事实根，以in传给生成的HandleCommitted。采样链不创建或消费CharacterFootIkCommittedCaptureViewLease、采样View、DTO、字段副本、Side分派、Session wrapper、Host Adapter或第二Writer。
- 通用Host负责从sealed packet与生成Schema自动输出主表、三张Ground子表、Sampler manifest和Capability manifest。Foot Analyzer／Publisher只消费这些基础产物，负责领域评分、诊断与报告，不再映射字段。
- **BREAKING** 删除三个Foot typed Event、Left／Right Dimension View／Metadata配对、Metadata Side、旧合成Ground Geometry表和旧Schema兼容读取。新产物使用新的Schema与Program identity；历史封存包保持不可变。
- Disabled Player不定义KK_DIAGNOSTIC_SAMPLING与KK_DIAGNOSTIC_FOOT：Generator零输出，Sampling Runtime与Foot Diagnostics程序集不进入Player，Conditional Attribute不进入业务metadata，Annotations不得成为Player根引用。Cecil与IL2CPP Gate必须检查零Attribute、零Sampling AssemblyRef、零Generated Program／Session／packet／queue／identity字符串。

## Capabilities

### New Capabilities

- character-foot-ik-diagnostic-sampling: 定义Foot IK如何把既有PostCommit多来源事实交给通用生成程序，并由通用Host产出基础采样文件、由领域Analyzer／Publisher完成下游分析。

### Modified Capabilities

- character-foot-placement-presentation: 规定采样只读取消费成功提交后的现有事实，不要求PoseGraph或Foot为了采样构造View、DTO、Event或第二事实页。

## Impact

- Runtime事实：现有Foot、Landing、Motion、Goal、Pelvis、FBBIK、Final Publication与Ground page真实成员只增加Conditional Annotations；不改变业务结果、算法、执行顺序或内存布局。
- Foot采样插件：保留Capability、Sampler、Program、Metadata和薄GeneratedCapture；删除旧Getter、Projection、Dimension View、typed Event、Side选择、合成Geometry表、Column／CsvBinding、领域Writer与Host Adapter。
- 通用框架：唯一拥有Roslyn Generator、Schema、Lifecycle、typed packet、Session、Writer、Host Finalizer、CSV和manifest。
- 下游工具：Analyzer／Publisher只负责Foot评分、诊断和报告；目前尚未完成新基础产物接线。
- 构建：字段、Program和GeneratedCapture已经通过编译；通用Host接入、Analyzer／Publisher迁移、Performance工作流和Disabled／Capture Player硬门禁仍未完成。
- 与current spec对比：character-foot-placement-presentation、character-animation-pipeline与character-pose-graph-runtime-architecture已同步为成功Seal后的同步Commit边界直接绑定多Fact Root；其中保留的Capture View只服务Live／Trace／Gizmo，不再属于采样合同。
