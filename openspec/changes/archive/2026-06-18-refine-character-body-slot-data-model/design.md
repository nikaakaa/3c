## Context
当前角色主线是 `CharacterRuntimeCore -> CharacterFramePipeline -> CharacterBehaviorSubmissionRunner -> BodyArbiter -> CharacterFramePlan -> output applier`。Locomotion 和 CommittedAction 已经是 sibling sources，但身体仲裁结果仍暴露 `BaseLayerOwner` 和旧 action-side owner 这类旧口径。

这会让后续编辑器和 Timeline 很难判断自己应该画什么：是画 FullBody 节点、Animancer layer、Action source，还是画 gameplay slot。正确方向是把 BodyArbiter 的输出改成“slot ownership result”，再由表现层消费。

## Goals
- 让 runtime 数据模型显式表达 `BaseSlot` 和 `UpperBodySlot`。
- 让 `BodyOccupancyDecision` / `CharacterFramePlan` 的正式读取面使用 slot 口径。
- 让 `FullBody` 只作为 claim，不再作为 slot、source、node、owner 或 layer。
- 让 `Action.Dodge` 的结果表达为 CommittedAction 接管 `BaseSlot`，并压制 `UpperBodySlot`。
- 删除 `BaseLayerOwner` / `UpperBodyOwner` 兼容读取，不让新代码继续扩散旧 layer 口径。
- 为后续 Editor / Timeline / compiler 提供可消费的数据契约。

## Non-Goals
- 不实现 UpperBody runtime source。
- 不实现 Facial / FaceBody slot。
- 不新增 Animancer upper-body 或 facial layer。
- 不实现 Character Behavior Editor 或 Committed Action Timeline Editor UI。
- 不接入 Ref/wly970123 runtime runner、`TimelinePlayer` 或 PlayableGraph。
- 不把 BBB 的 FullBody / UpperBody 状态树结构迁入当前 gameplay。

## Decisions

### Decision: slot 是正式仲裁结果
角色身体仲裁结果 MUST 以 slot 为中心表达。目标模型至少包含：

```text
Slot:
  BaseSlot
  UpperBodySlot

Owner:
  None
  Locomotion
  CommittedAction
  UpperBodyAction 或批准的等价名称
```

`BaseSlot` 是基础身体输出位置。普通移动时由 Locomotion 拥有；Dodge 等全身动作被采纳时由 CommittedAction 拥有。

`UpperBodySlot` 是上身扩展位置。当前只保留合同和压制关系，不代表 UpperBody runtime source 已完成。

当前实施读取面使用 `BaseSlotOwner`、`UpperBodySlotOwner` 和 `UpperBodySlotSuppressed`。旧 `BaseLayerOwner` / `UpperBodyOwner` 不再保留为兼容读取。

### Decision: claim 与 owner 分离
`BodyOccupancyKind.FullBody` 是 claim kind，不是 owner。它表示某个 source 请求全身占用。claim 被采纳后的结果不是 “FullBody-as-owner”，而是：

```text
BaseSlotOwner = CommittedAction
UpperBodySlotOwner = None
UpperBodySuppressed = true
```

正式代码 MUST 使用 `CharacterBodyDomain.CommittedAction` 表达 CommittedAction source 赢得 BaseSlot 的 owner。`FullBody` 只允许出现在 claim kind 或 claim factory 名称中，例如 `BodyOccupancyKind.FullBody` / `CommittedActionFullBody`。

### Decision: layer 命名退出正式契约
`BaseLayerOwner`、`UpperBodyOwner` 和类似 layer 口径命名不能再作为正式设计术语。当前实施 MUST 删除这些兼容属性，新测试、新文档、新 editor/compiler contract MUST 使用 slot 口径，例如 `BaseSlotOwner` / `UpperBodySlotOwner`。

### Decision: 表现层消费 slot 结果
Animancer base layer、upper-body masked layer、Timeline track、GraphView lane、AvatarMask、VFX/SFX presenter 都是 presentation layer 或 authoring view。它们 MUST 消费 slot result、channel output 或 compiled definition，不得决定 claim 是否被采纳。

### Decision: Facial 不在本模型默认落地
Facial 能力不能因为“身体分层”这个词就自动进入 BodyArbiter。它未来必须先被定义成以下之一：

```text
1. Channel
2. Presentation slot
3. Gameplay slot
```

本 change 只要求当前 runtime 不出现未审批的 `FaceBody`、`FacialOwner`、`FacialCandidate`、`FacialClaim` 或 `FacialSlot`。

## Migration Plan
1. 跑 GitNexus impact，确认 `BodyOccupancyDecision`、`CharacterFramePlan`、`DefaultBodyArbiter` 和测试影响。
2. 在 runtime model 中引入明确 slot result 读取面；必要时新增 slot/owner enum 或等价类型。
3. 将 BodyArbiter full-body claim 的输出映射到 `BaseSlotOwner = CommittedAction` 或批准等价 owner。
4. 将新测试和 touched consumers 改用 slot 口径。
5. 保留旧 layer 属性为兼容读取，并记录后续删除或重命名计划。
6. 更新 docs/spec，明确 editor/timeline/compiler 后续只能消费 slot contract。
7. 跑定向 EditMode 测试、OpenSpec validate 和 GitNexus detect_changes。

## Risks / Trade-offs
- Risk: 直接重命名旧 action-side owner 影响较大。
  - Mitigation: 先跑 GitNexus impact 和 rename dry-run，再分开处理 claim factory、candidate factory、domain owner 和 preemption fact，避免把 claim kind 与 owner 混成一个名字。
- Risk: 同时清理所有旧 `BaseLayerOwner` 调用会扩大范围。
  - Mitigation: 本 change 先通过 rg 确认旧属性只剩兼容转发和对应测试，再删除兼容读取。
- Risk: Editor/Timeline 想直接跟随视觉层。
  - Mitigation: spec 明确视觉层不是 gameplay slot，UI 只能展示或编辑 authoring 数据，再编译为 slot/channel contract。
