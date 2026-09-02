## Context

见[proposal.md](proposal.md)。独立Owner已经在`507ccbdb3c780f45d36b21bb044eedb4b53ec4a9`发布`com.kk.generated-diagnostic-sampling` 0.4.0；Analyzer SHA-256为`26577FD50C2DED7C7572E97C223777BCEF9C545BAB30379C5B6172C727AC20C1`，MVID为`5152d24c-57cd-4f31-9b50-837bfcb93de5`。0.4删除单View／Event ABI，一个Dimension直接接收多个既有Fact Root与Metadata。

3C已经切换外部file dependency并删除旧本地Owner，但Foot真实字段迁移、multi-root Commit接线、旧映射清理、Host消费和真实Player Gate仍在实施。本change不得复用旧0.1／0.3 Unity编译证据宣称0.4闭环。

## Goals / Non-Goals

**Goals:**

- 让3C只消费独立0.4.0 package与唯一Analyzer。
- 一个Foot Dimension直接接收Landing、Motion、Goal、Solved、Pelvis、Metadata等既有强类型事实，不构造诊断View或Event DTO。
- 普通字段只在真实readonly成员声明一次`DiagnosticField`。
- Commit点只以`in`传递左右事实并调用generated `HandleCommitted`。
- Host从Schema自动生成CSV和manifest，Foot只保留评分与报告。
- Disabled真实Player以产物Gate证明采样闭包完全退出。

**Non-Goals:**

- 不修改独立package或复制Generator到3C。
- 不让通用框架认识Foot、Landing、PIK或其它领域概念。
- 不新增Foot求解事实、算法、第二Commit路径或采样专用DTO。
- 不兼容0.1至0.3 Schema、Event／View ABI、旧packet或Reader。

## Decisions

### Decision 1: 3C固定消费一个0.4.0外部Owner

3C `Packages/manifest.json`固定引用：

```text
file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling
```

packages lock只能解析这个package。独立输入必须对账完整commit、0.4.0版本、Analyzer SHA-256、MVID和assembly identity；不使用embedded副本、submodule、双file／Git配置或消费者自建Analyzer。

### Decision 2: Capability声明多个既有Fact Root

Foot使用一个`DiagnosticCapability`声明identity、revision和Metadata类型，并以多个`DiagnosticFactRoot(rootId, type)`声明每个Dimension需要的既有readonly事实根。框架只认识Dimension、Fact Root、Metadata和Field，不认识Fact Root名称所代表的领域语义。

普通字段推荐使用path-scoped `DiagnosticField(revision, unit, groups)`，Generator以`Capability/main/FactRoot/member/path`生成稳定identity。同一类型经不同Root或路径复用时无需重复叶子Attribute。对外已经冻结的绝对identity才允许显式ID。

### Decision 3: Commit直接调用generated HandleCommitted

Program只声明稳定Dimension ID和Sampler。Generator按Program Dimension顺序及Fact Root ID稳定顺序生成：

```text
Lifecycle.Start(request)
Lifecycle.HandleCommitted(
  in lineage,
  in left.root0, ... in left.metadata,
  in right.root0, ... in right.metadata)
Lifecycle.Stop(outcome)
```

3C只在正式同步Commit成功边界把当前已经存在的事实以`in`传入。Generated入口内部完成packet rent、直接成员读取、编码、submit和结构化失败传播。3C不定义采样Lifecycle Event DTO、Dimension View、Bridge、Side选择、领域Session wrapper或逐字段复制。

### Decision 4: Derived只表达真正公式

普通成员、既有业务计算属性、enum和Unity Vector／Quaternion由Generator直接读取并推断codec。`DiagnosticDerivedField`只用于跨成员计算、条件选择或新的诊断数值；参数名映射实际Fact Root ID，参数类型使用`in`，最后接收`in Metadata`。只做`return root.Member`的Getter包装必须删除。

### Decision 5: Host按Schema自动完成基础产物

Player只运行generated Lifecycle与Capture并封存packet／runtime manifest。Editor Host直接读取Schema和sealed packet，自动生成主表、固定子表、UTF-8 RFC 4180 CSV、Sampler manifest和Capability manifest。Foot Analyzer／Publisher只读取Completed产物计算评分和发布报告，不声明Column、CsvBinding、Adapter或第二Schema。

### Decision 6: Disabled使用零闭包硬门禁

Capture Player显式定义`KK_DIAGNOSTIC_SAMPLING`与`KK_DIAGNOSTIC_FOOT`；普通发布和纯性能基线不定义。Conditional Attribute不写入业务metadata，Generator生成零代码，Sampling Runtime与Foot Diagnostics asmdef退出Player。

验收必须检查Managed与IL2CPP产物：业务程序集零Diagnostic Attribute、零Sampling AssemblyRef，Player零Annotations／Runtime／Foot Diagnostics程序集、零Generated Program／Lifecycle／Session／packet／queue／interest类型及零Capability／Field identity。不得以运行时bool、空实现、Linker猜测或构建后删除代替。

### Decision 7: 旧Owner与旧ABI一次删除

3C不保留旧embedded package、Tools、Event／View、Projection、Bridge、Adapter、Column／CsvBinding、普通Getter／Extractor、旧Reader或兼容packet。迁移可以在工作区分步，但最终提交必须只有一条0.4正式链。

## Risks / Trade-offs

- [generated入口参数较多] → 参数只出现在同步Commit接点，并换取零DTO、零字段复制与完整静态类型。
- [path identity随业务成员改名变化] → 成员路径属于Schema合同；真正跨版本冻结的字段使用显式绝对identity并升级Schema。
- [真实成员分散在多个程序集] → Annotations保持极薄引用，Runtime只进入Capture构建；Disabled最终以AssemblyRef和Player产物检查裁决。
- [旧Analyzer证据误判完成] → 只接受0.4.0精确commit和hash，旧0.1／0.3编译、CSV或packet只能作为历史。
- [Capture直接读取有成本] → 成本只存在于Capture BuildIdentity；Disabled构建完全退出，不能用运行时关闭伪装性能基线。

## Migration Plan

1. 对账独立0.4.0 commit、Analyzer hash／MVID和唯一file dependency。
2. 将Foot Capability改成多Fact Root，普通Attribute迁到真实成员，公式改成multi-root Derived。
3. Program声明左右Dimension，在同步Commit点调用generated `HandleCommitted`。
4. 删除Event／View／Bridge／Adapter／Getter／Extractor／Column／CsvBinding和旧Reader。
5. Analyzer／Publisher切到Host自动CSV、typed artifact和manifest。
6. 完成portable、Unity、Capture Player、Disabled Cecil／IL2CPP、identity搜索和strict验收。
7. 用户验收后更新current truth并归档；完成前保持active。

回退只恢复整个3C迁移前commit，不在运行时保留旧新选择。
