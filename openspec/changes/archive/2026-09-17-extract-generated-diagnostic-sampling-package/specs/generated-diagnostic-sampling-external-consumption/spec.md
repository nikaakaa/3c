## Purpose

定义3C作为Generated Diagnostic Sampling独立0.5.2 KK package消费者时的唯一依赖、多Fact Root接入、generated `DiagnosticEvent` handler、Host分析产物与Disabled零闭包合同。

## ADDED Requirements

### Requirement: 3C必须只消费独立0.5.2 KK package

3C Unity项目 MUST通过唯一file dependency解析`com.kk.generated-diagnostic-sampling` 0.5.2，并对账独立commit `e43af24`、Analyzer SHA-256 `47EE5F876377EBE453E98009E5AEA6D95FECC8DC4491B1A9EBE881812C55AA87`与MVID `9b3d5a64-d7e9-46c4-a687-52e87a5bc84a`。3C MUST不跟踪通用Runtime、Host、Generator、Analyzer binary、Tools或其镜像。

#### Scenario: Unity解析采样依赖

- **WHEN** 3C刷新Package Manager与脚本程序集
- **THEN** 它 MUST只加载独立仓库0.5.2 package和匹配Analyzer
- **AND** 旧ThirdPerson package、embedded源码、第二Analyzer或0.1至0.3 ABI MUST不存在于解析图

### Requirement: Foot必须直接声明多个既有Fact Root

Foot Capability MUST通过重复`DiagnosticFactRoot(rootId, type)`声明一个Dimension需要的多个既有readonly事实根，并单独声明Metadata类型。普通字段 MUST只在现有业务readonly field或调用方可读不可写属性上声明`DiagnosticField`；默认identity MUST由Capability、Table、Fact Root和成员路径生成。3C MUST不为采样创建View、Projection、DTO、字段副本或普通Getter／Extractor。

#### Scenario: 同一脚包含多个事实来源

- **WHEN** Foot Dimension需要Landing、Motion、Goal、Solved、Pelvis和Metadata事实
- **THEN** Capability MUST把它们声明为独立强类型Fact Root
- **AND** Generator MUST从真实成员生成统一Schema和直接成员访问，不要求消费方拼装CommittedFoot或Dimension View

### Requirement: Commit必须只调用DiagnosticEvent partial触发点

Foot Program MUST声明Event identity、稳定Left／Right Dimension ID和Sampler。正式同步Commit成功后，3C MUST只调用一个带`DiagnosticEvent`的private static partial void方法，并把目标、真实lineage及左右两组现有Fact Root以`in`参数传入。Generator MUST按Event identity和完整签名生成target-scoped dispatcher与匹配Program handler；handler内部完成packet rent、Capture、submit和失败传播，Host workflow在Start冻结Metadata并唯一控制Session Start／Stop。昂贵事实Owner MAY声明可选partial interest Query；3C MUST不定义采样Started／CommittedSample／Stopped Event DTO、Consumer／Binding、Bridge、Side选择、领域Session wrapper、通用Event Bus或逐字段复制。

#### Scenario: 一个正式Foot帧提交左右样本

- **WHEN** 正式表现帧在同步Commit边界成功提交
- **THEN** 调用方 MUST只调用一行Foot `DiagnosticEvent` partial方法并传入目标、真实lineage、Left事实组和Right事实组
- **AND** generated dispatcher／Program handler MUST按Dimension与Fact Root稳定顺序生成直接读取和typed packet写入；无订阅必须立即返回

### Requirement: Derived必须只表达真正公式

Foot普通成员读取、既有业务计算属性、enum与Unity值类型转换 MUST由Generator处理。`DiagnosticDerivedField` MUST只保留跨成员计算、条件选择或新诊断数值，并且只声明实际读取的`in` Fact Root参数及最后一个`in Metadata`。只转发`root.Member`的方法 MUST删除。

#### Scenario: 普通成员与计算公式并存

- **WHEN** 一个值已经存在于readonly事实成员而另一个值需要组合两个事实
- **THEN** 前者 MUST只使用成员上的`DiagnosticField`
- **AND** 后者 MAY使用参数名匹配Fact Root ID的`DiagnosticDerivedField`

### Requirement: Host必须按Schema自动生成基础采样产物

KK Host MUST从generated Schema与sealed packet自动生成主表、固定子表、CSV、Sampler manifest和Capability manifest。Foot消费方 MUST把Completed基础产物作为唯一采样输出，删除采样链中的旧单体Analyzer／Publisher、评分报告与Diagnosis Store，不得声明Adapter、Column、CsvBinding、第二Schema或回写Capture状态。基础采样产物 MUST不冒充领域诊断结论；独立`add-schema-driven-diagnostic-analysis` MAY只读消费Completed产物并在Host侧恢复Foot Operator、Plan和报告。

#### Scenario: Host完成Foot基础产物

- **WHEN** generated Lifecycle封存匹配0.4 Schema的packet
- **THEN** Host MUST不依赖Foot字段映射即可生成CSV与manifest
- **AND** 后续离线Analyzer失败 MUST不触发第二次采样或改变基础Capability结果

### Requirement: 迁移必须删除旧Owner与旧ABI

3C切换0.4 package的同一迁移 MUST删除旧embedded package、Tools、精确Repository Policy allowlist、旧namespace、Event／View、Projection、Bridge、Adapter、Column／CsvBinding、普通Getter／Extractor、旧Reader和旧Generated Program identity。系统 MUST不保留wrapper、type forwarder、兼容packet、同步脚本或fallback。

#### Scenario: 迁移完成后的源码搜索

- **WHEN** 对3C跟踪文件搜索旧Owner、旧ABI和映射链
- **THEN** 结果 MUST为零
- **AND** 旧identity只能存在于明确标记为历史且不会进入构建的证据中

### Requirement: Disabled与Capture构建必须形成不同静态闭包

Capture Player MUST显式定义`KK_DIAGNOSTIC_SAMPLING`与`KK_DIAGNOSTIC_FOOT`并只包含匹配0.4 Foot闭包。Disabled Player MUST不定义两项符号；Generator MUST生成零代码，Sampling Runtime与Foot Diagnostics程序集 MUST不进入Player。Cecil／IL2CPP Gate MUST证明业务程序集零Diagnostic Attribute和Sampling AssemblyRef，Player零Annotations／Runtime／领域Diagnostics／Generated Program／Lifecycle／Session／packet／queue／interest及Capability／Field identity。

#### Scenario: 纯性能基线关闭Foot采样

- **WHEN** 构建不定义采样符号的真实3C Player
- **THEN** Managed与IL2CPP产物检查 MUST证明采样闭包为零
- **AND** MUST不通过运行时bool、空实现、Linker猜测或构建后删除模拟关闭
