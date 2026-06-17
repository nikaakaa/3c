## ADDED Requirements

### Requirement: Character Frame Pipeline 只消费动作请求解析结果
`CharacterFramePipeline` MUST 只消费 request submission 阶段输出的纯数据结果。动作请求的收集、解析和准入 MUST 在 pipeline 的 request submission 边界内完成；pipeline 主体 MUST NOT 直接读取 Attack、Dodge、Jump 或 HitReact 配置，也 MUST NOT 直接决定这些动作的 target state、动画 key 或 motion spec。

#### Scenario: Pipeline 不认识具体动作解析
- **GIVEN** 本帧存在 Attack、Dodge 或 Jump 输入请求
- **WHEN** `CharacterFramePipeline` 执行 GameplayDecision 或等价 request submission phase
- **THEN** 具体动作解析 MUST 已由 provider/resolver 与 action arbiter 完成
- **AND** pipeline MUST 只接收 accepted request fact、interrupt decision 或等价 pure data submission
- **AND** pipeline MUST NOT 新增具体动作解析分支

#### Scenario: 输出阶段不反推动作请求
- **GIVEN** request submission 已输出 accepted resolved action
- **WHEN** pipeline 进入 BuildMotion、ExecuteMotion、PresentationBridge 或 WriteSnapshotAndEvents
- **THEN** 输出阶段 MUST 只消费状态机 frame、motion result、animation request 和 runtime facts
- **AND** 输出阶段 MUST NOT 重新读取输入缓冲来决定 Attack、Dodge 或 Jump

#### Scenario: 没有第二条 action 入口
- **WHEN** 新动作通过通用 request provider/resolver 接入
- **THEN** 它 MUST 继续进入唯一 CharacterFramePipeline
- **AND** MUST NOT 新增第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter
