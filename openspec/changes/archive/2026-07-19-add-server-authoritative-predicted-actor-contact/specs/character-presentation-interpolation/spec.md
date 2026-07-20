## ADDED Requirements

### Requirement: Remote Body表现与预测接触必须消费同一选择流

ServerAuthoritative Prediction Schedule MUST是Remote Body tick选择的唯一owner。Schedule为Current step产生并成功提交的selected Body frame MUST进入Remote Presentation Egress；声明`ObservedKinematicActorContact`能力的Composition还 MUST把同一选择转换为World观察约束。Remote Presentation MAY缓存相邻selected frame并在渲染帧插值，也 MAY在selected frame被新权威信息替换后从当前visual pose平滑收敛，但 MUST不重新读取原始authority Body选择另一tick、不维护独立Body delay cursor，也 MUST不把visual root写回WorldSolver。

#### Scenario: 远端Actor阻挡本地owner

- **WHEN** Prediction使用Actor B的selected frame裁剪Actor A位移
- **THEN** Client A显示的Actor B Body target MUST来自同一selected frame
- **AND** MUST不出现碰撞体使用外推位置而可见角色仍固定使用另一延迟时间线

#### Scenario: 新权威样本替换短时外推

- **WHEN** 新remote authority Body使后续selected frame改变
- **THEN** canonical contact MUST从新frame立即参与后续World step
- **AND** Presentation MAY只在visual root上平滑收敛

#### Scenario: Restore后执行Replay与Current

- **WHEN** 一个成功outer transaction先重放过去step再提交新的Current step
- **THEN** Remote Presentation MUST只接收Current step的selected Body frame
- **AND** Replay frame MUST不让可见远端角色倒退到历史tick

#### Scenario: Prediction当前产生零Current step

- **WHEN** clock correction使当前outer transaction没有新的Current step
- **THEN** Remote Presentation MUST完成或保持已提交Body区间
- **AND** MUST不自行从原始authority样本选择新Body target

#### Scenario: Prediction执行HardRecovery

- **WHEN** formal HardRecovery替换当前Prediction分支
- **THEN** Model Egress MUST显式重置Remote selected Body stream
- **AND** 后续成功Current step MUST提交新anchor
- **AND** Presentation MUST不根据Transform或旧buffer猜测恢复位置

### Requirement: Remote可靠表现事件必须服从selected Body horizon

Remote SampleProducer、Select、Complete、Release、GameplayFact与Cue MUST继续保留其authority tick和EventId。SampleProducer MAY提前进入采样缓存，但可靠事件 MUST不早于同tick selected Body frame已提交给Remote Presentation后发布。Presentation MUST不建立另一套Body authority timeline推进事件。

#### Scenario: 可靠Attack Select先于selected Body提交

- **WHEN** Remote Attack Select已到达但对应authority tick的selected Body尚未由成功transaction提交
- **THEN** Select MUST继续等待
- **AND** Body frame提交后 MUST按原EventId发布而不是生成新事件
