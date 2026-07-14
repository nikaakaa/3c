# character-animation-pipeline Specification

## MODIFIED Requirements

### Requirement: 动画层预览只读取调试 Snapshot
系统 MUST 支持从动画混合模型导出 `AnimationLayerFrameSnapshot` 或等价调试数据，用于编辑器显示每 tick 的动画层混合。Snapshot MUST 只作为调试和预览输出，MUST NOT 参与 gameplay 决策、transition 条件或最终动画应用。Timeline 编辑器预览 MUST 从与正式动画层运行时一致的 preview session 获取 snapshot，MUST NOT 从 `TimelinePlayer` 或独立 PlayableGraph 读取混合结果。

#### Scenario: 生成每 tick 预览数据
- **WHEN** 动画混合模型在某帧生成结果
- **THEN** 系统 MAY 导出包含每层贡献列表、来源、clip 时间、权重和最终结果的 snapshot
- **AND** 编辑器预览 MUST 从 snapshot 读取显示数据
- **AND** snapshot MUST 来自正式动画层运行时或与其规则一致的 editor preview session

#### Scenario: 运行时禁用调试历史
- **WHEN** 项目不需要动画层预览或调试历史
- **THEN** 系统 MAY 不保留历史 snapshot
- **AND** 正式运行时混合结果 MUST 不依赖 snapshot 存在

### Requirement: 不新增 Timeline 播放分裂路径
系统 MUST 只保留一条角色管线 Timeline 播放主链路：节点提交请求，BTSMTL 内部 TimelinePlaybackScheduler 推进，轨道采样输出数据，PresentationStage 应用表现。BTSMTL Timeline 编辑器预览 MUST 复用同一套轨道采样、动画贡献和动画层规则。系统 MUST NOT 新增并行 Workbench、旧 SO/config、TimelinePlayer autonomous tick、独立 PlayableGraph 预览权威或第二套 TimelineNode 播放路径。

#### Scenario: 迁移旧直接播放逻辑
- **WHEN** `TimelineNode` 直接播放逻辑已被管线请求链路替代
- **THEN** 实现阶段 MUST 删除旧字段、旧绑定和旧评估调用
- **AND** 系统 MUST NOT 保留兼容分支继续支持节点直接播放

#### Scenario: 迁移旧编辑器预览逻辑
- **WHEN** Timeline 编辑器仍引用 `TimelinePlayer`、`Timeline.TimelinePlayer`、`PlayableGraph` 或 `TimelinePlayer.RunningTimelines`
- **THEN** 实现阶段 MUST 将这些入口迁移到 `TimelinePreviewSession` 或删除
- **AND** 编辑器预览 MUST NOT 成为第二套 Timeline 播放权威

