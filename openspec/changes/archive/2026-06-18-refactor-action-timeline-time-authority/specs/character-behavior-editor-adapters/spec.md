## ADDED Requirements

### Requirement: Timeline Editor 使用 Seconds Authoring 与 Tick Preview
Committed Action Timeline Editor MUST 以 seconds 作为 timeline authoring 主编辑单位，并将 tick grid、local tick 和量化结果作为预览与诊断信息展示。Editor MAY 复用 Ref 的 frame ruler、field view、track view 和 clip view 结构，但 MUST NOT 让 frame 术语成为正式 authoring 字段或 runtime 采样权威。

#### Scenario: 拖拽写回 seconds
- **WHEN** 设计者在 Committed Action Timeline Editor 中移动或缩放 clip
- **THEN** editor MUST 将结果写回该 clip 的 start seconds / end seconds 或批准的等价 seconds authoring 字段
- **AND** 保存后 action definition compiler MUST 能把这些 seconds 量化为 runtime tick 区间

#### Scenario: 预览显示 local time 和 local tick
- **WHEN** 设计者拖动 preview locator 到某个位置
- **THEN** editor MUST 显示对应 local time seconds
- **AND** MUST 显示按当前 fixed tick interval 量化得到的 local tick
- **AND** preview outcome MUST 调用正式 evaluator 或批准的等价 evaluator

#### Scenario: Tick grid 不成为 authoring 权威
- **WHEN** editor 显示 tick grid 或 frame-like 小刻度
- **THEN** grid MUST 只作为 seconds authoring 的辅助视图
- **AND** editor MUST NOT 将 UI 像素或 render frame 编号直接保存为 runtime timeline 权威数据

#### Scenario: Ref UI 迁移不恢复 frame authoring
- **WHEN** `port-ref-timeline-ui-to-unity-2022-compatible-editor` 迁移 Timeline field、track 或 clip view
- **THEN** 迁移后的 UI MUST 使用本项目 seconds authoring adapter
- **AND** MUST NOT 重新引入 `durationFrames/startFrame/endFrame` 作为正式 authoring 主字段
