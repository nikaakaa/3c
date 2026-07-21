## MODIFIED Requirements

### Requirement: Remote Body表现与预测接触必须消费同一选择流

ServerAuthoritative Prediction Schedule MUST是Remote Body tick选择的唯一owner。Schedule为Current step产生并成功提交的selected Body frame MUST进入Remote Presentation Egress；声明`ObservedKinematicActorContact`能力的Composition还 MUST把同一选择转换为World观察约束。Remote Presentation MUST通过唯一 presentation-only visual pose convergence/filter 消费相邻 committed selected frame，在渲染帧插值，并在selected target被新权威信息替换后从当前visual pose有界收敛。filter MAY在零Current step的表现帧继续朝既有committed target收敛，但 MUST不重新读取原始authority Body选择另一tick、不维护独立Body delay cursor、不改变可靠事件horizon，也 MUST不把visual pose、visual velocity或error写回WorldSolver、Prediction state或contact body。

#### Scenario: 远端Actor阻挡本地owner

- **WHEN** Prediction使用Actor B的selected frame裁剪Actor A位移
- **THEN** Client A显示的Actor B Body target MUST来自同一selected frame
- **AND** visual filter MAY只改变到该target的渲染帧收敛过程
- **AND** MUST不出现碰撞体使用外推位置而可见角色使用另一延迟时间线

#### Scenario: 新权威样本替换短时外推

- **WHEN** 新remote authority Body使后续selected frame改变
- **THEN** canonical contact MUST从新frame立即参与后续World step
- **AND** Presentation MUST从当前visual pose有界收敛到新selected target
- **AND** 收敛参数 MUST来自Presentation Profile而不是Network Model

#### Scenario: Restore后执行Replay与Current

- **WHEN** 一个成功outer transaction先重放过去step再提交新的Current step
- **THEN** Remote Presentation MUST只接收Current step的selected Body frame
- **AND** Replay frame MUST不让可见远端角色倒退到历史tick

#### Scenario: Prediction当前产生零Current step

- **WHEN** clock correction使当前outer transaction没有新的Current step
- **THEN** visual filter MAY继续朝已经提交的Body target收敛或保持
- **AND** MUST不自行从原始authority样本选择新Body target

#### Scenario: Prediction执行HardRecovery

- **WHEN** formal HardRecovery替换当前Prediction分支
- **THEN** Model Egress MUST显式重置Remote selected Body stream
- **AND** visual filter MUST清除旧target、visual velocity和error state
- **AND** 后续成功Current step MUST以显式新anchor建立视觉区间

#### Scenario: 观察视觉误差

- **WHEN** visual pose尚未收敛到当前selected target
- **THEN** diagnostics MUST同时报告selected tick、target pose、visual pose和error
- **AND** diagnostics MUST不反向修改filter、Prediction或World state
