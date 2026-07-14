## MODIFIED Requirements

### Requirement: StateMachine transition 必须提交动画 owner handoff

系统 MUST 让 StateMachine runtime 为每次 state activation 提供稳定逻辑 owner，并为嵌套 execution path 维护当前 presentation leaf owner。每次逻辑 transition MUST 提交正式 animation transition request；request MUST 携带根 animation transition domain、source/target 逻辑 owner、已解析或待解析的 source/target leaf owner、definition 与 cause。TransitionRuntime MUST 按 domain 保持 WaitingTarget、capture、strategy、supersede 和 retirement，同一 domain MUST NOT 因父子 StateMachine runtime id 不同而并行推进两套 transition。

Target activation 的 State body 至少实际 tick 一次后 MUST 提交 TargetReady。若 target 是结构 State 且内部运行嵌套 StateMachineNode，TargetReady MUST 同时解析到当前 nested presentation leaf。Target leaf 缺少动画 contribution 时 MUST 暴露真实空输出，不得隐式保留 source 或播放 fallback。

#### Scenario: 父层进入嵌套 Attack

- **WHEN** 外层 Action StateMachine 从 None 切换到 Attack
- **AND** Attack 的 State body 启动内层 Attack1
- **THEN** pending handoff 的 target leaf MUST 解析为 Attack1 activation owner
- **AND** 同 tick Attack1 Timeline sample MUST 作为 incoming contribution
- **AND** 外层 Attack 结构 owner 不得被当作空动画 target

#### Scenario: 内层 combo transition

- **WHEN** Attack1 -> Attack2 transition 命中
- **THEN** request MUST 使用 Attack domain
- **AND** source/target MUST 分别为 Attack1/Attack2 leaf owner
- **AND** 旧 Attack1 逻辑 MUST NOT 为表现混合继续 tick

#### Scenario: 父子 transition 同 Tick 提交

- **WHEN** 内层 leaf 因父层 replacement 先提交 terminal release
- **AND** 外层 State transition 在同一 logic tick 提交 leaf -> replacement request
- **THEN** 同 domain 后提交 request MUST supersede 前一个 request
- **AND** 新 request MUST 从当前最终视觉结果 capture
- **AND** 两个 request MUST NOT 并行叠加权重

#### Scenario: Target leaf 没有动画

- **WHEN** resolved target leaf 已获得正式执行机会但没有合法 contribution
- **THEN** handoff MUST 进入正式空输出
- **AND** 系统 MUST NOT 回退到父结构 owner、隐藏 Idle 或旧 source contribution

#### Scenario: 父层离开嵌套 Attack 到 Empty

- **WHEN** 内层 Attack graph 完成且外层 Attack -> None transition 命中
- **THEN** source leaf MUST 是最后 active Attack1 或 Attack2 owner
- **AND** target MUST 是显式 Empty
- **AND** transition definition MUST 来自最终拥有该 domain handoff 的正式 edge
