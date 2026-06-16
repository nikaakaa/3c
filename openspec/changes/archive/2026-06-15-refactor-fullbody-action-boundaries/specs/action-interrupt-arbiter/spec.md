## ADDED Requirements
### Requirement: 请求候选构建与仲裁分离
系统 MUST 将请求候选构建和请求准入仲裁分离。request candidate builder MAY 读取自身需要的输入 buffer、Locomotion facts、Action 配置和 current timeline facts 来构建候选请求；`ActionInterruptArbiter` 或等价仲裁入口 MUST 仍是 priority、resistance、force、policy、timeline window 和过期规则的唯一准入裁决者。

#### Scenario: builder 只生成候选
- **WHEN** request candidate builder 发现一个可提交请求
- **THEN** builder MUST 只生成纯数据 candidate request 或等价输入
- **AND** MUST NOT 直接切换状态机状态
- **AND** MUST NOT 直接消费输入缓冲
- **AND** MUST NOT 直接播放动画或执行运动

#### Scenario: 仲裁器产生 accepted/rejected 决策
- **GIVEN** request candidate collection 提供 0..N 个候选请求
- **WHEN** FullBody request submission arbiter 处理候选
- **THEN** 每个需要准入裁决的候选 MUST 经过 `ActionInterruptArbiter`
- **AND** rejected 候选 MUST NOT 生成状态机 request fact
- **AND** accepted 候选 MAY 参与本帧最高优先级选择

#### Scenario: 候选集合稳定排序
- **GIVEN** 多个候选请求在同一帧被接受
- **WHEN** gate 选择本帧 request fact
- **THEN** 选择规则 MUST 使用 request priority
- **AND** 同 priority MUST 使用 builder 顺序、origin step 或等价稳定 tie-break
- **AND** 相同输入序列 MUST 产生相同 accepted request fact 序列
