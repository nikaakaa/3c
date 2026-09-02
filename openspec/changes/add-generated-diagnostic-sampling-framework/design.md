## Context

见[proposal.md](proposal.md)。独立仓库`D:/Unity_Project_1/generated-diagnostic-sampling`的0.5.2 release已经实现multi Fact Root、path-scoped Field identity、业务`DiagnosticEvent`、可选partial interest Query、generated lifecycle、typed packet、Schema-driven Host／Analysis、不完整评分固定权重和Disabled portable gate；3C当前仍在清理旧Consumer／Binding链。本change以独立0.5.2合同为唯一框架真相，只规划3C消费与最终Player闭包，不恢复项目内第二份实现。

约束固定为Unity 2022.3、Roslyn 3.8兼容Source Generator、.NET Standard 2.0 Generator边界和IL2CPP AOT。采样不得反向驱动业务状态；Performance工作流只有一个Build、Player、Controller、顶层Capture和Comparer；项目不接受反射fallback、运行时表达式、兼容wrapper或第二采样路径。

## Goals / Non-Goals

**Goals:**

- 一个Dimension直接消费多个现有强类型Fact Root与一个Metadata，不创建诊断View或Event DTO。
- 普通字段只在现有真实只读成员声明一次Attribute，现有业务计算getter可直接采样。
- Generator生成静态Capture、业务`DiagnosticEvent` typed dispatcher、Program handler和Session生命周期，Editor与IL2CPP执行同一Program identity；Start／Stop由Host workflow控制。
- Runtime packet、Host CSV／manifest与BuildIdentity形成可验证闭包，Disabled Player完全退出采样。
- 3C Foot只作为首个多来源消费方，不让通用框架认识Foot、Landing、PIK或PoseGraph。

**Non-Goals:**

- 不建立通用对象序列化器、运行时反射、表达式树编译、动态对象图、字符串成员路径或字段字典。
- 不统一各领域的Frame、Tick、Completion、availability、lineage或评分语义。
- 不替代BTSMTL Trace、Unity Profiler、WPR、日志、线上遥测或数据库。
- 不实现旧单View／Dimension View／生命周期Event DTO ABI兼容。
- 不让框架主动读取Module、Workspace、Transform、World Query或Vendor对象。

## Decisions

### Decision 1: Annotations、Generator、Runtime和Host保持单向分层

依赖方向固定为：

```text
Annotations
  ├─ Existing Domain Facts + Definitions
  ├─ Source Generator -> Generated Program + Lifecycle
  ├─ Capture Runtime -> Session + Packet + Writer
  └─ Editor Host -> Schema Finalizer -> Analyzer / Publisher
```

Annotations只保存Conditional Attribute与最小enum，不引用Runtime、Host、`UnityEditor`或领域程序集。Generator只在编译期间运行。Runtime只拥有Session、packet lease、有界队列、Writer和状态机。Host保持Editor-only并内建Schema驱动CSV／manifest。领域Analyzer／Publisher只读取Completed artifact。

选择分层而不是一个运行时Manager，是因为编译期发现、Player热路径、后台写入和Host格式化具有不同依赖与寿命；它也让Disabled构建可以按程序集闭包证明退出。

### Decision 2: Capability注册Metadata与多个Fact Root

每个Capability使用一个`DiagnosticCapability(id, revision, metadataType)`和多个`DiagnosticFactRoot(rootId, type)`。Fact Root ID在Capability内唯一，Generator按ordinal稳定排序。Fact Root是现有业务事实，不是采样专属DTO；框架只认识Dimension、Fact Root、Metadata和Field，不解释Root ID的领域含义。

Program使用`DiagnosticCaptureProgram`声明Event identity、稳定Dimension ID数组和Sampler类型；业务使用`DiagnosticEvent`标记自己的Post-Commit partial方法，参数直接列出目标、真实lineage和多个`in` Fact Root。Metadata只由Lifecycle Start提供并冻结。Generator生成：

```text
BusinessDiagnosticEvent(in target, in lineage, in dimension0.root0, ...)
  -> typed dispatcher
  -> matching generated Program handler
  -> rent / Capture each dimension / submit / Fault
Host workflow -> Session Start / Stop
```

参数顺序由Event签名、Program Dimension顺序和Fact Root ID顺序共同锁定。业务只在同步Commit调用栈调用一行partial方法；typed dispatcher按目标过滤，匹配handler使用业务lineage为每个Dimension自动租packet、调用同一Capture并提交。昂贵事实Owner可额外声明约定的partial Query，在帧开始取得同一目标的订阅兴趣；普通Event不强制Query。消费方不定义`CaptureStarted`、`CommittedSample`、`CaptureStopped`领域Event DTO，不持有Session或packet，也不使用通用Event Bus。

选择生成显式多参数入口，是用较长但编译期固定的调用签名换取零View构造、零逐字段复制和完整AOT调用图。

### Decision 3: 普通字段按Fact Root与成员路径生成

普通声明使用`DiagnosticField(revision, unit, groups)`，默认identity为：

```text
Capability/main/FactRoot/member/path
Capability/Table/FactRoot/row/member/path
Capability/main/metadata/member/path
```

成员名确定性转为kebab-case。同一类型作为不同Fact Root或经不同公开路径复用时得到不同identity，叶子Attribute仍只写一次。`AvailabilityMember`引用当前类型的同级可读成员并在同一Root／Path下解析；对外已经冻结的绝对identity才使用显式字符串重载。

Generator只沿调用方可读路径查找带合同的成员。Fact Root必须是readonly struct；普通字段可以是readonly field、getter-only auto-property或现有无setter计算getter。计算getter已经属于业务事实，其执行只发生在Capture调用内；Generator不得因它不是auto-property而拒绝，也不得把它复制成Derived转发方法。无订阅或Disabled时不会调用这些getter。

固定一对多事实在现有readonly struct buffer或外部只读class page上声明`DiagnosticTable`。class page只需稳定`Count`和只读索引器；行内叶子仍用`DiagnosticField`。Generator不要求把page复制为新数组或表DTO。

### Decision 4: Derived只保留真正公式并只声明实际Root

`DiagnosticDerivedField`只用于无法由现有成员直接读取的诊断公式。方法参数按名字映射Fact Root ID，只列实际读取的Root且全部使用`in`，最后必须是`in Metadata`；Table公式再接row index。Generator校验Root名称、类型、重复参数、availability、依赖闭包和环。

enum编码、Unity Vector／Quaternion转换、已有计算getter、成员转发、Side分发和字段搬运都不是Derived理由。一个公式需要两个Root时直接接收两个Root，不创建组合View；需要的Root未注册时编译失败并要求修正Capability，而不是从其它DTO或Metadata绕取。

### Decision 5: Generator产生完整AOT静态闭包

Generator从Roslyn symbol构造normalized descriptor，依次完成Fact Root与字段发现、成员路径／codec／availability校验、Sampler union、dense typed handle、Table layout、Schema、Capture、Lifecycle和Program identity。Capture形态为：

```text
Capture(in Root0, in Root1, ..., in Metadata, ref DiagnosticCapturePacket)
```

函数体直接访问各Root成员并写packet；相同Field identity在一个sample中只求值一次。运行时不存在表达式树、Reflection、`DynamicInvoke`、`MethodInfo.Invoke`、字符串路径执行、每字段Delegate或领域switch。Schema identity闭合Capability、Dimension Set、Fact Root Set／type identity、Sampler、字段路径、Derived来源、codec revision、Generator binary、生成source hash和packet layout。

### Decision 6: Runtime只拥有Session、packet和封存状态

每个Capability Session状态为`Prepared -> Capturing -> Finalizing -> Completed/Faulted/Cancelled`。Host workflow Start冻结Program、Schema、Sampler interest、cadence、packet capacity、queue、transport与输出闭包并订阅匹配typed dispatcher；generated Program handler同步读取Event提供的`in`事实、取得versioned struct lease、Capture并消费式提交；Host workflow Stop退订并发起非阻塞封存。

主线程不等待Writer、格式化、Analyzer或Publisher。Overflow、sequence断裂、非法状态或Writer失败使对应Capability Faulted并保留已有证据。框架不知道Left／Right、Presentation Frame、Simulation Tick或跨Capability对齐关系；这些只存在于Dimension ID、opaque lineage和上层工作流。

### Decision 7: Host只按Schema处理sealed packet

Player Writer只封存packet stream、Schema引用、runtime manifest与failure证据。Host Reader先验证hash和identity闭包，再由`DiagnosticHostFinalizer`按Sampler Schema生成主表、固定子表、UTF-8无BOM RFC 4180 CSV、Sampler manifest和Capability manifest。主表带sample key与Dimension，子表额外带row index；Vector／Quaternion稳定展开组件，不可用字段输出空单元格。

Host不重新读取业务成员、不调用Derived公式、不持有Fact Root／Metadata，也不需要Host Adapter、Column、Header或CsvBinding。领域Analyzer／Publisher只在基础Capability manifest为Completed后读取产物并拥有自己的结果身份，不回写采样状态。

### Decision 8: Disabled由编译与产物Gate形成零闭包

Annotations使用`Conditional("KK_DIAGNOSTIC_SAMPLING")`；Generator在全局define缺失时零输出；Capture Runtime与领域Diagnostics asmdef使用define constraints；Host为Editor-only。Capture构建再定义Capability专属符号并静态包含选中Program。

Annotations必须在源码编译时可解析，但Disabled Player不得因Attribute语法保留Annotations根程序集。Gate使用Cecil检查业务程序集零Diagnostic custom attribute与零`KK.GeneratedDiagnosticSampling` AssemblyRef，并检查Managed／IL2CPP输出零Annotations、Runtime、领域Diagnostics、Generated Program、Session／packet／queue／interest类型及Capability／Field identity。运行时bool、空实现、未订阅Session、Linker推测或构建后删除都不能替代Gate。

### Decision 9: 3C只消费独立0.5.2发布

通用Owner固定为独立`com.kk.generated-diagnostic-sampling` 0.5.2，Owner提交`e43af24`，Analyzer SHA-256为`47EE5F876377EBE453E98009E5AEA6D95FECC8DC4491B1A9EBE881812C55AA87`，MVID为`9b3d5a64-d7e9-46c4-a687-52e87a5bc84a`。3C只保存package引用、领域Capability／Sampler／Program声明、现有业务成员Attribute、一行Post-Commit `DiagnosticEvent` partial调用和Editor workflow；Foot采样消费方不再拥有手写GeneratedCapture或旧单体Analyzer／Publisher。`add-schema-driven-diagnostic-analysis`在同一package内提供Host-only通用分析基础设施，并由独立3C Foot Analysis Editor程序集提供领域Operator、Plan和报告；固定权重Score在证据不全时只发布已知贡献、可用权重和上下界，不重分配未知维度。PoseGraph继续生产自己的正式Committed事实，不新增采样View、Event payload或consumer binding；只在帧开始读取generated target interest决定延迟事实冻结。

Performance工作流继续唯一拥有Build Request、`DiagnosticCapabilitySet`、Player manifest、Run Request、握手、顶层Capture与Comparer。框架不创建第二Player、第二Controller或第二产物根。

## Risks / Trade-offs

- [DiagnosticEvent参数较多] → 参数只出现在业务声明和同步Commit一行调用，Generator按完整签名绑定handler；换取零DTO、零字段复制和明确AOT闭包。
- [同类型多路径产生更多字段] → identity明确包含Fact Root和成员路径；不同业务位置不被错误合并。
- [业务成员改名改变path identity] → 路径是Schema合同；对外冻结字段使用显式绝对identity，改名触发Schema升级。
- [计算getter可能包含业务成本] → 只允许采样现有业务事实getter且只在Capture调用时执行；无Capture不调用，领域必须对自身getter语义负责。
- [Capture增加读取与packet写入成本] → 成本只属于Capture BuildIdentity；Disabled以产物Gate证明完全退出，两种BuildIdentity不直接做性能差值。
- [独立包与3C迁移不同步] → package版本、Analyzer SHA-256、MVID和Generator identity进入闭包；3C不得复制源码或保留0.3兼容ABI。

## Migration Plan

1. 锁定独立0.5.2 package与Analyzer identity，3C package引用只指向该正式发布。
2. 把每个Capability改为Metadata加多个`DiagnosticFactRoot`，Program直接声明Dimension IDs。
3. 把普通旧Getter迁到现有真实成员Attribute；允许现有计算getter和class page Table，公式改为只接实际Root的Derived。
4. 在同步Commit点声明并调用一行`DiagnosticEvent` partial方法，把目标、真实lineage和每个Dimension的已有事实以`in`传入；Host workflow在Start冻结Metadata，Generator生成typed dispatcher、可选partial interest Query和Program handler。
5. 删除单View、Dimension View、三个领域Event DTO、Projection、Bridge、Column／CsvBinding、Host Adapter和旧Reader兼容。
6. 执行Capture Player、Disabled Managed／IL2CPP Gate、Repository Policy、OpenSpec strict和identity闭包验收。
7. 用户完成实机验收后再安装current specs并归档；回退只恢复上一套完整提交，不保留双版本运行路径。

## Current Spec And Active Change Comparison

- current `btsmtl-runtime-diagnostics`要求诊断只读且不反向驱动运行；本框架只在同步Commit边界读取现有事实并保持该要求，不进入RuntimeDebugSession Trace Store。
- current `character-foot-placement-presentation`、`character-animation-pipeline`与`character-pose-graph-runtime-architecture`的业务真相仍是成功Seal后的唯一Post-Commit事实边界；`add-schema-driven-diagnostic-analysis`只在该边界增加一行partial Event调用。保留的Capture View只服务Live／Trace／Gizmo，不属于采样链。
- active `refactor-character-pose-graph-architecture`只拥有正式Committed Result、事务、Seal和Post-Commit边界，可独立推进；它不等待或引用Sampling Runtime。
- active `refactor-foot-ik-diagnostic-sampling`只负责0.4领域采样接入；active `add-schema-driven-diagnostic-analysis`负责Completed artifact之后的通用Host分析与Foot领域规则，二者都不得恢复View、Event DTO、Bridge或Host Adapter。
- completed未归档`add-gameplay-performance-capture-workflow`继续唯一拥有Player BuildIdentity和Comparer；后续只接入通用Capability Set，不增加Foot专属构建字段。
