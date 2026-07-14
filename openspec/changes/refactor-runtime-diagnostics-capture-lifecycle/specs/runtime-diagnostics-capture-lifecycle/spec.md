# runtime-diagnostics-capture-lifecycle Specification

## ADDED Requirements

### Requirement: Runtime diagnostics 必须以 interest 按需启用采集

每个 registered runtime diagnostics target MUST 默认没有有效采集 interest，并且 effective channel MUST 为 `None`。Graph、Timeline、Host Inspector 或正式 Capture command MUST 通过共享 RuntimeDebugSession 获取和释放 target-level interest；interest MUST 明确携带 Live State 或 Capture kind、channel 集合和 Capture detail。Session MUST 对同一 target 的所有 active interest 计算唯一并集，并且 MUST NOT 让窗口直接写 target 的全局 channel。

没有 active interest 时，runtime producer MUST 在构造 payload、解析 source handle、格式化诊断字符串或写入 store 前停止诊断工作。interest 的获取、释放、target 切换、窗口关闭和 target 终止 MUST 有明确 owner 与清理路径，不得遗留常驻 channel。

#### Scenario: 没有诊断观察者

- **WHEN** CharacterPipeline 已注册 diagnostics target，但 Graph、Timeline、Host Inspector 和 Capture 都没有 active interest
- **THEN** target 的 effective channel MUST 为 `None`
- **AND** Tree、Timeline、Animation、Blackboard、Motion producer MUST 不构造或保存 diagnostics payload
- **AND** gameplay 与 presentation 结果 MUST 保持不变

#### Scenario: Graph 与 Timeline 同时观察

- **WHEN** 同一 target 的 Graph Live Debug 请求 Graph/StateMachine，Timeline Live Debug 请求 Timeline/Animation
- **THEN** target MUST 只拥有一个 effective interest 并集
- **AND** 两个窗口 MUST 不创建第二个 Buffer、target 或采集器
- **AND** 任一窗口关闭后 MUST 只释放自己声明的部分

### Requirement: Live State 必须只表达当前正式事实

Live State MUST 按 source identity、runtime instance identity、domain 与事实种类保存最新正式事实，而不是追加成无界或滚动的 event history。每条 current record MUST 携带正式 position、sequence、payload 与 activation/terminal 状态。等价状态 MUST 去重；连续 Timeline time、animation sample 和 fade progress MAY更新 current record，但 MUST覆盖同一 record。

Live State store MUST 提供单调 revision 和 cursor delta。首次 consumer 同步或 cursor 失效时，系统 MAY 返回完整当前状态表；该表 MUST 受活跃 source/instance 数量限制，MUST NOT 通过回放历史 event 构造。

#### Scenario: Timeline visual time 连续变化

- **WHEN** 一个 active Timeline 在连续表现帧更新 visual time
- **THEN** Live State MUST 更新该 playback 的当前 visual time
- **AND** MUST NOT 为每个表现帧追加 Live history event
- **AND** Timeline view MUST 能显示最新 visual playhead

#### Scenario: Runnable 保持 Running

- **WHEN** RunnableNode 在多个 logic tick 内保持同一个 Running 状态
- **THEN** Live State MUST 不因相同状态重复产生无意义 mutation
- **AND** node 进入、完成、停止或状态改变时 MUST 更新对应当前事实

### Requirement: Capture 必须由作者显式开始并保留有界历史

Capture MUST 只能由共享 RuntimeDebugSession 的正式 command 开始。每次 Capture MUST 拥有独立 capture identity、明确 detail、有限 segment capacity 和单调 cursor。Stop Capture 或 target 终止后，Editor MUST 持有不可变 Capture snapshot，runtime MUST 释放可写 capture store。Capture history 的 scrub MUST 只改变 Editor read position，MUST NOT 回滚 gameplay、Timeline、Animation 或 runtime target。

Capture detail MUST 至少区分 Boundary、Evaluation 与 Continuous。默认 Boundary 只记录生命周期、状态切换和选择边界；条件求值只在 Evaluation 中记录；逐 tick/逐帧时间、sample、fade 与 interpolation 只在 Continuous 中记录。

#### Scenario: 未开始 Capture 时打开 Live Debug

- **WHEN** 作者只打开 Graph 或 Timeline Live Debug
- **THEN** 系统 MUST 提供当前 Live State
- **AND** MUST NOT 隐式保存当前之前或之后的 Capture history
- **AND** history scrub 控件 MUST 不把 Live State 伪装成录制历史

#### Scenario: 作者录制连续动画细节

- **WHEN** 作者明确开始 Continuous Capture 并观察 Timeline/Animation
- **THEN** Capture MUST 记录正式 Timeline logic/visual time、animation sample、fade 与 presentation interpolation
- **AND** Stop Capture 后 Graph、Timeline 与 Host MUST 在同一冻结 capture position 读取数据

#### Scenario: Capture 容量达到上限

- **WHEN** 新 capture segment 使有界容量溢出
- **THEN** store MUST 按完整逻辑 tick 或表现帧 segment 丢弃最旧数据
- **AND** MUST 不留下无法重建的半个 segment
- **AND** MUST 不改变 gameplay runtime

### Requirement: 共享 provider 必须只消费增量并发布 change set

每个 attached target MUST 由唯一 Editor-only shared provider 消费 Live State 与 Capture cursor。provider MUST 在初次附着时按 target revision 缓存严格 Source Map，并在每个 Editor update 最多消费一次该 target 的新增数据。revision 未变化时 provider MUST 不复制完整 event list、不重建 Source Map、不重建全部 instance 索引且 MUST 不通知视图。

provider MUST 将新增数据映射为版本化 change set，至少表达变更 source、runtime instance、Graph/StateMachine state、Timeline playback summary、Host channel summary、target/menu revision 与 Capture history revision。Graph、Timeline 和 Host Inspector MUST 只读取该 provider，不得各自扫描 runtime store 或重建第二份 read model。

#### Scenario: 两个窗口接收同一 target 更新

- **WHEN** 同一 target 的 Live State revision 前进且 Graph 与 Timeline 都处于 Live Debug
- **THEN** shared provider MUST 只消费一次 runtime delta
- **AND** Graph 与 Timeline MUST 从同一个 provider revision 各自读取相关 change set
- **AND** 两个窗口 MUST 不各自复制或分析完整历史

#### Scenario: 无变化的 Editor update

- **WHEN** target 的 Live State revision、Capture revision 和 target metadata 都未改变
- **THEN** provider MUST 不分配新的完整 view model、event list 或 Source Map snapshot
- **AND** Graph、Timeline 与 Host Inspector MUST 不因 diagnostics 执行全量 overlay/menu 刷新

### Requirement: Live 冻结、Capture 历史与 Ended 必须具有不同语义

冻结 Live MUST 只冻结当前 provider read model，并停止对应 Live interest；恢复 Live MUST 重新获取 interest。Capture history 只来自已开始并停止的 Capture snapshot。target Ended 时 provider MUST 冻结最后已消费的 Live State 和已有 Capture snapshot，释放 runtime target/store 引用，并禁止继续 Live 或继续 Capture，直到作者显式附着新 target 或 Clear Session。

#### Scenario: 冻结当前 Live 状态

- **WHEN** 作者冻结 Live Debug 而没有 active Capture
- **THEN** Graph 与 Timeline MUST 显示同一时刻的 current state
- **AND** target MUST 不继续为该冻结视图采集 Live State
- **AND** UI MUST 不显示不存在的历史位置

#### Scenario: target 结束

- **WHEN** attached CharacterPipeline 注销 diagnostics target
- **THEN** provider MUST 保留只读 Ended current state 和已冻结 Capture snapshot
- **AND** MUST 不继续持有 runtime Graph、Node、Timeline、Buffer 或可写 store
- **AND** 作者显式附着新 target 或 Clear Session 前，Ended view MUST 保持只读
