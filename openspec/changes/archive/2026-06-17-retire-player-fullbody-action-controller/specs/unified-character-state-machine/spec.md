## MODIFIED Requirements
### Requirement: 当前 runner 对模块模型的支撑边界
系统 MUST 在现有自研统一状态图 runner 上实现节点模块模型，而不是新增第二套状态机 runtime。现有 runner MAY 继续负责 active state、state time、variant、transition、pending path 和 restore；模块解析、输出聚合和事实采样 MUST 保持纯数据并位于明确 solver 子职责中。正式 runner owner MUST 是 `CharacterStateMachineRuntime` 或等价状态机运行时模块，并由角色级 runtime owner 装配。

#### Scenario: 保留单一 runner owner
- **WHEN** 模块化节点配置接入运行时
- **THEN** `CharacterStateMachineRuntime` 或等价状态机运行时模块 MUST 是唯一正式 runner owner
- **AND** 系统 MUST NOT 新增 parallel ECS state runner、per-action runner 或独立 Locomotion runner
- **AND** 系统 MUST NOT 通过 `PlayerFullBodyActionController` 表达 runner ownership

#### Scenario: Runner 不知道具体模块副作用
- **WHEN** runner 推进一帧状态
- **THEN** runner MUST NOT 直接播放 Animancer
- **AND** MUST NOT 直接执行 movement
- **AND** MUST NOT 直接消费 Unity 输入对象
- **AND** 模块输出 MUST 通过 `CharacterFrameSubmission` 或等价角色级帧输出提交进入 Character frame pipeline，由 output composer/applier 执行副作用
