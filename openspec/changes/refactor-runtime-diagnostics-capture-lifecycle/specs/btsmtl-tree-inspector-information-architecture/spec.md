# btsmtl-tree-inspector-information-architecture Specification

## MODIFIED Requirements

### Requirement: TreeWindow 运行时模式必须保持窗口级边界

`Authoring / Live Debug` MUST 是整个 TreeWindow 的模式，而不是 Data 页、Inspector 页或 Graph Settings 的局部状态。Live Debug 下 authoring 命令 MUST 保持只读。TreeWindow MUST 使用共享 RuntimeDebugSession 的 target、shared incremental provider、Live/Frozen/Capture/Ended 状态和 Capture history position，并持有只属于当前 TreeWindow 的 Graph runtime binding。

TreeWindow 进入 Live Debug、切换 Graph、关闭窗口或退出 Live Debug 时 MUST 精确获取或释放自身 Graph + StateMachine Live State interest。它 MUST 不在每个 Editor update 重新计算 source request、扫描完整 diagnostics event、清空全部 overlay 或重建全部菜单；provider change set 只允许更新当前 binding 命中的 Node、Edge 和 StateMachine 状态。

#### Scenario: 在 Live Debug 中切换左侧页签

- **WHEN** 作者在 Live Debug 模式下从 Data 切换到 Inspector 或反向切换
- **THEN** 页签切换 MUST 不改变共享 target、provider 或 Capture history position
- **AND** 页签切换 MUST 不重置当前 TreeWindow 的 Graph Follow / Pin binding
- **AND** 作者不得通过任一页签写入 Graph、Blackboard、Input 或 runtime state

#### Scenario: Graph 与 Timeline 同时打开

- **WHEN** 作者同时打开 TreeWindow 和 TimelineEditorWindow
- **THEN** TreeWindow MUST 只修改自己的 Graph runtime binding 与 interest 生命周期
- **AND** TimelineEditorWindow 的 Timeline playback binding MUST 保持不变
- **AND** 两个窗口 MUST 从同一 provider revision 或 shared Capture position 显示各自 overlay

#### Scenario: 无相关诊断变更

- **WHEN** shared provider 的 change set 不包含当前 Graph source 或当前 binding instance
- **THEN** TreeWindow MUST 不清空或重绘无关 Node/Edge overlay
- **AND** MUST 不重建 Target/Instance 菜单
- **AND** MUST 不因 diagnostics 触发全量窗口刷新
