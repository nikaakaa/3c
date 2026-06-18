## 0. 范围确认
- [x] 0.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 0.2 读取 `AGENTS.md`、`openspec/AGENTS.md`、`openspec/project.md` 和 goal 文档。
- [x] 0.3 确认本变更只改身体 slot 结果模型，不实现 UpperBody runtime、Facial slot、Editor UI、Timeline UI 或 preview。
- [x] 0.4 确认不改 `CharacterRuntimeCore` 入口，不改 `CharacterFramePipeline` phase 顺序。

## 1. 影响分析
- [x] 1.1 对 `BodyOccupancyDecision` 跑 GitNexus impact 并记录风险。
- [x] 1.2 对 `CharacterFramePlan` 跑 GitNexus impact 并记录风险。
- [x] 1.3 对 `DefaultBodyArbiter.Decide` 跑 GitNexus impact 并记录风险。
- [x] 1.4 搜索 `BaseLayerOwner`、`UpperBodyOwner` 和旧 action-side owner 的生产代码和测试引用。

## 2. Runtime Slot Contract
- [x] 2.1 在 `CharacterBodyArbitration` 中引入明确 slot 结果读取面，例如 `BaseSlotOwner` 和 `UpperBodySlotOwner`。
- [x] 2.2 评估后不新增 `CharacterBodySlot` / `CharacterBodySlotOwner`，将旧 action-side owner 收敛为 `CharacterBodyDomain.CommittedAction`，claim factory 使用 `CommittedActionFullBody` 表达 FullBody claim。
- [x] 2.3 让 FullBody claim 被采纳时输出 action-side `BaseSlot` owner，并压制 `UpperBodySlot`。
- [x] 2.4 让无 FullBody claim 时 Locomotion 继续拥有 `BaseSlot`。
- [x] 2.5 让 UpperBody claim/candidate 只影响 `UpperBodySlot`，不隐式接管 `BaseSlot`。
- [x] 2.6 删除 `BaseLayerOwner` / `UpperBodyOwner` 兼容读取，正式代码只暴露 slot 结果。

## 3. 命名边界
- [x] 3.1 评估是否在本 change 重命名旧 action-side owner。
- [x] 3.2 已运行 GitNexus impact 和 `rename` dry-run；由于自动 rename 会混淆 claim factory 与 candidate factory，最终按 dry-run 审查结果做受控 patch。
- [x] 3.3 将正式 action-side owner 改为 `CharacterBodyDomain.CommittedAction`，并将 claim factory 改为 `CommittedActionFullBody`。
- [x] 3.4 记录后续真正要做的命名清理，不把更大范围旧 runtime 类型清理混入本 change。

## 4. 自动测试
- [x] 4.1 增加或更新测试：Dodge FullBody claim 被采纳后，`BaseSlotOwner` 是 action-side owner。
- [x] 4.2 增加或更新测试：FullBody claim 被采纳后，`UpperBodySlotOwner` 为空或被压制。
- [x] 4.3 增加或更新测试：无 FullBody claim 时，Locomotion 拥有 `BaseSlot`。
- [x] 4.4 增加或更新测试：UpperBody claim 不接管 `BaseSlot`。
- [x] 4.5 增加静态测试：runtime 不出现未审批 `FaceBody`、`FacialOwner`、`FacialCandidate`、`FacialClaim` 或 `FacialSlot`。
- [x] 4.6 增加静态测试：Dodge 不通过 FullBody behavior node、旧 FullBody 主树或第二角色入口表达。

## 5. 文档和 OpenSpec
- [x] 5.1 更新 `AGENTS.md` 和 `openspec/project.md`，明确新设计必须使用 slot contract。
- [x] 5.2 更新 goal 文档，记录最终 slot/claim/source/channel/presentation 分层和遗留命名迁移计划。
- [x] 5.3 更新 spec delta，确保 `action-domain-runtime`、`character-frame-pipeline`、`dodge-action` 不再把 FullBody 写成 owner/slot/node。

## 6. 验证
- [x] 6.1 运行相关 EditMode 测试。
- [x] 6.2 运行 `openspec validate refine-character-body-slot-data-model --strict --no-interactive`。
- [x] 6.3 运行最终相关 OpenSpec validate。
- [x] 6.4 运行 GitNexus `detect_changes({scope:"all"})` 并记录影响范围。
