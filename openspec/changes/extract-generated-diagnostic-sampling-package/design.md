## Context

见[proposal.md](proposal.md)。本change在3C repo-local OpenSpec范围内实施，只修改3C。独立KK package与`pik`消费分别由自己的仓库change管理；三个change共享migration identity，但每个Git仓库只提交自身Owner文件。

前置约束：Foot字段迁移任务可以继续增加Extractor，但在本change窗口必须冻结package／Generator／Foot using／asmdef／生命周期文件；独立package必须已完成内建生命周期处理器与Schema-driven CSV，不得把旧Bridge／Host Adapter合同迁回3C。

## Goals / Non-Goals

**Goals:**

- 让3C从框架源码Owner变为纯UPM消费者。
- 把Foot接入改为三个typed生命周期Event与Attribute声明。
- 一次删除旧package、Tools、namespace、Analyzer与Repository Policy allowlist。
- 保持PoseGraph、Foot算法、Performance顶层Player／Controller Owner不变。

**Non-Goals:**

- 不创建或修改独立仓库与`pik`；它们由关联change实施。
- 不新增Foot字段、评分规则、World Query或运行算法。
- 不兼容旧Schema、Program、packet或Capture request identity。

## Decisions

### Decision 1: 3C只使用一个外部file dependency

3C `Packages/manifest.json`固定引用：

```text
file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling
```

路径相对3C `Packages`目录解析。packages lock必须只保存`com.kk.generated-diagnostic-sampling`。不使用submodule、复制package或file／Git双配置，因为它们都会在3C重新产生框架工作树或双来源。

### Decision 2: Foot消费者执行破坏性KK重命名

Foot的using与asmdef从`ThirdPerson.GeneratedDiagnosticSampling*`一次改到`KK.GeneratedDiagnosticSampling*`。Generator assembly、MVID和Sampler descriptor变化会产生新Schema／Program identity；3C删除未完成旧Capture与请求，不提供旧Reader。OpenSpec历史证据可保留旧commit/hash文字，但不得成为编译引用。

### Decision 3: 三个Event替代Foot Bridge

Foot只在正式Owner处发布：

```text
CaptureStarted
-> CommittedSample after successful PoseGraph Seal
-> CaptureStopped(Completed | Cancelled | Faulted)
```

Started冻结Program／Schema／容量／interest；CommittedSample携带短租约`CharacterFootIkCommittedCaptureViewLease`、frame lineage与`CharacterFootIkCaptureMetadata`，Left／Right由声明式样本维度展开；Stopped提交最终outcome。KK Generator生成typed handler并自动Session、租包、Capture、提交和封存。任何`CharacterFootIkCaptureBridge`或领域Session wrapper都必须删除。

### Decision 4: Foot Analyzer／Publisher位于采样完成之后

KK Host自动生成主表、Geometry子表、CSV和Sampler／Capability manifest。Foot Analyzer与Publisher只读取这些生成产物并发布独立报告，不参与Host Finalizer，不影响基础Capability是否Completed。旧ColumnName只用于迁移对账；迁移完成后Column／CsvBinding／旧Reader整体删除。

### Decision 5: 删除旧Owner与切换消费者属于同一3C提交

3C不能提交“新dependency已接入但旧package仍跟踪”或“旧package已删但Foot仍引用ThirdPerson”的中间状态。实现可在工作区分步完成，但最终只在portable build、Unity refresh、identity搜索和OpenSpec校验全部通过后提交一个闭合迁移。

## Risks / Trade-offs

- [Foot并行字段迁移冲突] → 先取得明确冻结窗口，只迁移using／asmdef／Event与框架依赖；已有字段内容原样保留。
- [Generator identity变化导致旧产物不可读] → 明确删除未完成旧请求和staging，不实现兼容Reader；历史归档保持只读文件证据。
- [外部file dependency目录缺失] → 以独立package已构建commit作为任务前置；缺失时停止，不恢复3C本地副本。
- [Analyzer仍依赖旧CSV Header] → 在同一迁移中切到生成artifact／typed manifest，旧Column绑定整体删除，不保留双Reader。

## Migration Plan

1. 记录独立package、3C和`pik`关联change／commit，确认KK package源码与Analyzer闭合。
2. 冻结Foot重叠文件，更新3C manifest／lock、using、asmdef、Event Definition和Generated Program引用。
3. 删除Foot Bridge、Host Adapter、Column／CsvBinding与旧Reader控制面，Analyzer／Publisher改为只读生成产物。
4. 删除3C本地package、Tools和Repository Policy allowlist，更新相关active change Owner口径。
5. 依次执行portable build、build server shutdown、3C Unity refresh、Console核对、旧identity搜索和OpenSpec严格校验。
6. 提交3C迁移并记录独立package与`pik`精确关联commit。

回退只恢复整个3C迁移前commit，并要求关联仓库回到匹配identity；不在运行时保留旧新选择。
