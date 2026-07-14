# btsmtl-tree-inspector-information-architecture Specification

## MODIFIED Requirements

### Requirement: TreeWindow 运行时模式必须保持窗口级边界

Authoring / Live Debug MUST 是整个 TreeWindow 的模式，而不是 Data 页、Inspector 页或 Graph Settings 的局部状态。Live Debug 下 authoring 命令 MUST 保持只读。TreeWindow MUST 使用共享 RuntimeDebugSession 的 target、channel、history 和只读 snapshot，并持有只属于当前 TreeWindow 的 Graph runtime binding。

#### Scenario: 在 Live Debug 中切换左侧页签

- **WHEN** 作者在 Live Debug 模式下从 Data 切换到 Inspector 或反向切换
- **THEN** 页签切换 MUST 不改变共享 target 或 Trace history
- **AND** 页签切换 MUST 不重置当前 TreeWindow 的 Graph Follow / Pin binding
- **AND** 作者不得通过任一页签写入 Graph、Blackboard、Input 或 runtime state

#### Scenario: Graph 与 Timeline 同时打开

- **WHEN** 作者同时打开 TreeWindow 和 TimelineEditorWindow
- **THEN** TreeWindow MUST 只修改自己的 Graph runtime binding
- **AND** TimelineEditorWindow 的 Timeline playback binding MUST 保持不变
- **AND** 两个窗口 MUST 在同一 shared history position 显示各自 overlay

#### Scenario: 创建 TreeWindow

- **WHEN** Editor 创建 TreeWindow 与其 Inspector 视觉树
- **THEN** USS MUST 使用当前 Unity 支持的选择器
- **AND** 创建过程 MUST 不因 :first-child 或 :last-child 产生 stylesheet parser error

#### Scenario: Play Mode domain reload 后恢复当前 Graph

- **WHEN** 当前 TreeWindow 经历 Play Mode domain reload 并重建 UI
- **THEN** 窗口 MUST 只按已保存的 serialized owner、property path 与 GraphAuthoringId 恢复当前 Graph
- **AND** 窗口 MUST 重建自己的 Graph runtime binding，不得恢复旧 runtime instance
- **AND** locator 缺失或 identity 不一致时 MUST 停止恢复，不得按名称、路径近似或窗口顺序选择其它 Graph
