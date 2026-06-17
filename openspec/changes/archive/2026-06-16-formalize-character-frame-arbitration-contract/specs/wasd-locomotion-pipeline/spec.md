## ADDED Requirements
### Requirement: Locomotion 作为角色级兄弟提交者
Locomotion 在目标架构中 MUST 作为 Character frame owner 下的 sibling submitter 提交移动意图、移动事实、基础移动候选输出和 Locomotion animation 请求。Locomotion 可以被 FullBody Action 的角色级仲裁结果压制，但 MUST NOT 被定义为 FullBody Action framework 的长期内部子职责。

#### Scenario: Locomotion 提交候选输出
- **WHEN** Locomotion runtime 处理本帧移动输入
- **THEN** Locomotion MUST 能提交移动意图、世界方向、gait、phase、motion candidate 和 animation candidate
- **AND** 这些数据 MUST 进入 Character frame owner 或等价角色级汇集入口
- **AND** Locomotion MUST NOT 直接提交最终 movement 或 animation 副作用

#### Scenario: 被压制时不执行副作用
- **GIVEN** Locomotion 已提交基础移动候选输出
- **AND** CharacterFramePlan 标记该候选输出被 FullBody occupancy claim 压制
- **WHEN** output applier 执行本帧
- **THEN** Locomotion motion candidate MUST NOT 被提交给 motion executor
- **AND** Locomotion animation candidate MUST NOT 被提交给 base layer Presenter
- **AND** Locomotion runtime MUST NOT 通过独立 direct tick 补交同一输出

#### Scenario: Locomotion 不读取 FullBody 私有状态
- **WHEN** Locomotion 判断本帧是否应提交候选输出
- **THEN** 它 MAY 读取角色级 frame context、accepted request facts 或 arbitration result
- **AND** MUST NOT 直接读取 `PlayerFullBodyActionController`、`FullBodySubmissionBuilder` 或 FullBody 私有字段作为压制权威

#### Scenario: Direct tick 只保留非正式用途
- **WHEN** 项目保留 Locomotion direct tick、诊断或测试入口
- **THEN** 该入口 MUST 标记为非正式主线
- **AND** MUST NOT 与 Character frame owner 竞争最终 movement、animation 或 camera output
