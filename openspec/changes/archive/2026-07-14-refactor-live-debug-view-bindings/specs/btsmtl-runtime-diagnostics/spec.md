# btsmtl-runtime-diagnostics Specification

## MODIFIED Requirements

### Requirement: 每个 runtime target 必须拥有有界 Trace Buffer

每个 Character runtime diagnostics target MUST 拥有独立有界 Trace Buffer。Buffer MUST 支持实时消费、暂停观察和容量范围内的历史回看；达到容量后 MUST 按明确顺序丢弃最旧完整 debug frame，不得增长为无界列表。target 结束时 runtime MUST 释放其 Buffer；Editor MUST 仅保留从该 Buffer 复制出的不可变结束 snapshot，不得继续持有 runtime target 或可写 Buffer。

#### Scenario: Buffer 达到容量

- **WHEN** 新事件进入已经达到容量的 Trace Buffer
- **THEN** Buffer MUST 丢弃最旧完整 frame 或 tick segment
- **AND** MUST NOT 留下无法重建的半个 segment
- **AND** gameplay runtime MUST 继续执行

#### Scenario: runtime target 销毁

- **WHEN** CharacterPipeline deactivate 或 dispose
- **THEN** Trace Buffer MUST 发布 target lifecycle 终止并释放 runtime 持有数据
- **AND** editor Session MUST 从最后一个已分析 Trace 生成只读 Ended snapshot
- **AND** Ended snapshot MUST 不接收新事件或持有 runtime Buffer

### Requirement: RuntimeDebugSession 必须统一目标、历史与只读捕获

Editor MUST 使用唯一 RuntimeDebugSession 或等价 service 管理 registered target、显式 target、channel、实时/暂停、历史位置和分析后的只读 Trace snapshot。Graph、Timeline 和 Host Inspector MUST 消费该 Session 的同一 target 与历史 snapshot，不得各自扫描 runtime service、持有 runtime clone 或重建第二份 Trace。

Session MUST NOT 保存全局 runtime instance、全局 Follow 或全局 Pin。Graph、Timeline 等观察页面 MUST 通过各自的 editor-only view binding 保存 source、Follow / Pin 与 runtime instance。

#### Scenario: Graph 与 Timeline 同时观察

- **WHEN** 同一 Character target 的 Graph 窗口 Follow 一个 Graph，Timeline 窗口 Follow 一个 Timeline playback
- **THEN** 两个窗口 MUST 使用同一 target、channel 和 history position
- **AND** 两个窗口 MUST 保持各自 runtime instance，不得互相覆盖 Follow 或 Pin
- **AND** 两个 overlay MUST 只读取同一 Trace snapshot

#### Scenario: 查看历史位置

- **WHEN** 用户暂停 Session 并设置历史位置
- **THEN** Graph、Timeline 和 Host Inspector MUST 观察同一个历史 snapshot
- **AND** 各窗口的本地 Pin / Follow 选择 MUST 保持各自语义
- **AND** history 操作 MUST NOT 回滚 runtime actor

#### Scenario: target 结束后查看最后状态

- **WHEN** 已附着 target 注销
- **THEN** Session MUST 标记该 snapshot 为 Ended
- **AND** Graph 与 Timeline MAY继续显示最后一次 source-mapped overlay
- **AND** 作者显式附着新 target 或清除 Session 前，Ended snapshot MUST 保持只读

## ADDED Requirements

### Requirement: Debug Target 自动附着必须基于显式角色或唯一精确匹配

Graph 或 Timeline 进入 Live Debug 时 MUST 用当前 source identity 与 content hash 解析 target。场景选择包含 CharacterPipelineHost 或其子对象时，该 Host MUST 被视为作者的显式 target 意图。没有显式 Host 时，系统只可在唯一 registered target 的 Source Map 包含当前 source 且 content hash 精确匹配时自动附着。

系统 MUST NOT 按 target 注册顺序、显示名称、场景遍历顺序、Graph 名称、Timeline 名称、asset path 或近似 source path 自动选择 target。

#### Scenario: 场景中显式选择 Corin

- **WHEN** 作者选择 Corin 的 CharacterPipelineHost 或其子对象并进入当前 Graph 的 Live Debug
- **THEN** 系统 MUST 尝试附着该 Host 对应的 registered target
- **AND** source map 与 content hash 精确匹配时 MUST 附着该 target
- **AND** 不匹配或未注册时 MUST 显示明确原因
- **AND** 系统 MUST NOT 改选另一个角色

#### Scenario: 没有显式 Host 且只有一个匹配角色

- **WHEN** 场景选择不包含 Host，且恰有一个 registered target 与当前 source identity/content hash 精确匹配
- **THEN** Session MUST 自动附着该 target
- **AND** UI MUST 显示该 target 是唯一精确匹配结果

#### Scenario: 多个匹配角色

- **WHEN** 场景选择不包含 Host，且多个 registered target 与当前 source 精确匹配
- **THEN** Session MUST NOT 自动选择其中任意一个
- **AND** UI MUST 显示候选 target 并等待作者显式选择

#### Scenario: source revision 不一致

- **WHEN** 已选择 target 不包含当前 source 或 Source Map content hash 与当前作者内容不同
- **THEN** overlay MUST 停止绘制该 source
- **AND** UI MUST 分别显示 source 缺失或 revision mismatch
- **AND** 系统 MUST NOT 使用名称、index 或近似 path fallback

### Requirement: 每个 Live Debug 视图必须拥有本地 runtime instance binding

每个 Graph 或 Timeline Live Debug 页面 MUST 持有 editor-only 的 source binding。binding MUST 只在该页面内保存 Follow 或 Pin instance，并从共享 Session snapshot 解析正式 runtime instance。binding MUST NOT 写入 authoring asset、runtime target 或其它视图的 selection。

#### Scenario: 一个 Graph 多次 activation

- **WHEN** 同一 Graph source 在共享 snapshot 中有多个 State activation 或 Graph runtime instance
- **THEN** Graph 窗口 MUST 在自己的 binding 中显示 Follow 或可 Pin 的实例选择
- **AND** Timeline 窗口的 playback binding MUST 不被改变

#### Scenario: 一个 Timeline 多次 playback

- **WHEN** 同一 Timeline source 在共享 snapshot 中有多个 Timeline playback instance
- **THEN** Timeline 窗口 MUST 在自己的 binding 中显示 Follow 或可 Pin 的 playback 选择
- **AND** Graph 窗口的 instance binding MUST 不被改变
