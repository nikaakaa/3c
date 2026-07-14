# btsmtl-timeline-editor-preview Specification

## MODIFIED Requirements

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

`TimelineEditorWindow` MUST 提供语义明确且互斥的 Authoring Preview 与 Live Debug 模式。Authoring Preview MUST 继续由 `TimelinePreviewSession` 驱动；Live Debug MUST 通过 RuntimeDebugSession 的 shared incremental provider 观察真实 scheduler current state 或显式 Capture history，不得调用 preview evaluator、修改 runtime playback 或改写其它 Graph / Timeline 窗口的 binding。

Live Debug 进入时 MUST 由该 Timeline 窗口的本地 binding 声明 Timeline + Animation Live State interest；进入 Capture 或停止 Capture 时 MUST 使用共享 Session 的正式 command，不得创建窗口私有 history。退出 Live Debug、切换 Timeline locator 或关闭窗口时 MUST 释放该窗口 interest。

#### Scenario: Live Debug

- **WHEN** 用户选择 Live Debug
- **THEN** TimelineEditor MUST 以当前 Timeline identity/content hash 请求正式 target 解析
- **AND** 成功附着时 MUST 使用该窗口本地 binding 与 shared provider 观察真实 playback
- **AND** Timeline 编辑内容 MUST 只读
- **AND** `TimelinePreviewSession` MUST 不参与该模式

#### Scenario: 同时打开 Graph 与 Timeline

- **WHEN** 当前 Timeline Live Debug 与同一 target 的 Graph Live Debug 同时存在
- **THEN** Timeline MUST 与 Graph 共用同一个 target provider 和 target effective interest
- **AND** Timeline MUST 只保存本窗口 playback Follow / Pin
- **AND** 系统 MUST 不创建第二个 trace scan、provider 或 Capture

### Requirement: Timeline Live Debug 必须显示真实 runtime membership

Timeline Live Debug MUST 从 shared provider 的正式 current playback summary 或冻结 Capture view 显示 playback instance/generation、发起 Graph / Node source、可用的 activation context、active Track/Clip、TreeClip phase/runtime、AnimationProducerSample、PendingFirstSample/Current/Outgoing/Retired 与 terminal state。它 MUST 不根据当前 authoring time 重新采样来猜测 membership。

在 Live State 下，Timeline MUST 增量读取当前 logic/visual time、Track/Clip 和动画 lifecycle；在 Capture history 下，它 MUST 读取当前 shared history position 的记录。Timeline MUST 不扫描完整 event list、重建 playback summary 或在每个 Editor update 重建 target/playback 菜单。

#### Scenario: visual time 位于两个 logic tick 之间

- **WHEN** PresentationFrame 以 interpolation alpha 计算 visual Timeline time
- **THEN** Timeline Live Debug MUST 从 current state 显示分别的 logic time 与 visual time
- **AND** animation playhead MUST 使用 visual time
- **AND** gameplay window/TreeClip decision 标记 MUST 使用 logic tick

#### Scenario: 停止 Continuous Capture 后查看动画细节

- **WHEN** 作者停止包含 Timeline/Animation Continuous detail 的 Capture
- **THEN** Timeline Editor MUST 在 shared Capture history position 显示记录的 visual time、sample、fade 与 lifecycle
- **AND** MUST 不调用 TimelinePreviewSession 或 authoring evaluator
- **AND** 拖动 history MUST 不改变 runtime playback
