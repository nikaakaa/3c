# character-motion-semantics Specification Delta

## ADDED Requirements

### Requirement: MovingTurn必须是Run-only固定180°动作

Corin MovingTurn的Gameplay StateMachine MUST只允许从RunLoop进入。输入存在、输入方向与Body朝向误差达到显式Blackboard配置`MovingTurnAngleThreshold=135°`、Attack Action Context未激活且Dodge Action Context未激活时，Gameplay StateMachine MUST进入MovingTurn。任一Action Context仍激活时 MUST不选择该边；动作退出后若其它条件仍成立 MUST只选择一次。WalkLoop、WalkStart、WalkEnd、RunStart、RunEnd和Idle MUST不直接进入MovingTurn；RunEnd重新收到输入时 MUST先进入RunLoop，再由唯一RunLoop门禁决定是否进入MovingTurn。Presentation Pose StateMachine MAY在观察到已提交MovingTurn事实时从RunStart或RunEnd进入同一Turn Pose，但不得借此创建第二Gameplay入口。MovingTurn MUST播放固定180°作者动作，不得按当前朝向到目标输入方向的任意角度缩放yaw曲线。

#### Scenario: Run中输入接近反向

- **WHEN** RunLoop中的输入方向与Body朝向误差达到135°
- **THEN** Gameplay StateMachine MUST进入MovingTurn
- **AND** MUST执行固定180°转身
- **AND** 普通Walk中的相同输入 MUST继续使用正式Locomotion转向而不进入MovingTurn

#### Scenario: 动作期间持续保持反向输入

- **WHEN** Attack或Dodge Action Context仍激活且玩家持续保持满足135°门槛的方向输入
- **THEN** `RunLoop -> MovingTurn` MUST不被选择
- **AND** 动作退出后其它门禁仍成立时 MUST恰好选择一次MovingTurn入口

### Requirement: MovingTurn必须由28帧Timeline独占Body Root Motion

Corin MovingTurn MUST使用60Hz有限Timeline作为Body平移与yaw的唯一作者。Timeline MUST保留源Root Motion曲线0–28帧，前25帧 MUST完成固定180° yaw，后3帧 MUST保持180°用于姿态收束。X/Z MUST来自同一源曲线的同一时间段，并保持Root Motion Baker输出的Unity米制值；29个贡献的累计X/Z MUST为`(-0.9001478, 0.4623734)`，累计yaw MUST为180°，不得再乘`0.01`、只清零横向分量或使用另一条修正轨迹。Gameplay输入转向和Pose `RootOrientationWarp` MUST不再同时修改该状态的朝向。

#### Scenario: Fixed与Float32执行同一转身

- **WHEN** 同一MovingTurn分别由Float32和Fixed Program执行
- **THEN** 两者 MUST从同一typed Timeline payload采样等价的X/Z/yaw增量
- **AND** Body MUST通过现有World Solver与KCC提交确定性结果
- **AND** Pose Graph MUST只播放Turn Sequence，不得第二次解释LocalYaw
- **AND** Pose节点与Graph运行内存 MUST不进入Rollback snapshot或网络协议

### Requirement: MovingTurn必须在Timeline完成后立即恢复正式Locomotion

MovingTurn MUST只以`state_root_completed`作为释放门禁，不得再以Facing Error释放。保持输入时 MUST按`HasDirectionalDodgeRunIntent`互斥进入RunLoop或WalkLoop；停止输入时 MUST在Timeline完成后进入WalkEnd。Presentation从RunStart、RunLoop或RunEnd进入Turn MUST使用typed Transition payload显式保存的0.12秒Inertialization；Turn到RunLoop、WalkLoop或Idle MUST使用0.30秒Inertialization，并在退出过渡开始后由目标Locomotion恢复普通输入转向和移动。Idle、WalkLoop与RunLoop MUST保留各自连续播放相位，进入这些循环状态时不得强制重置到frame 0；WalkStart、RunStart、RunEnd与Turn等有限状态 MUST继续在进入时重置。Transition时长与状态重置策略 MUST可通过共享Pose StateMachine作者表面与Document v3修改，不得在Runtime硬编码。

#### Scenario: 前闪避期间与退出后的反向输入

- **WHEN** DodgeForward建立Run意图且Dodge Action Context仍激活
- **AND** 玩家保持反向输入
- **THEN** MovingTurn MUST不在动作下方提前选择
- **AND** Dodge Action Context退出后 MUST由唯一RunLoop门禁触发MovingTurn
- **THEN** Timeline MUST完整执行28帧
- **AND** 完成后 MUST立即进入RunLoop并恢复普通移动

#### Scenario: Turn退出到循环Locomotion

- **WHEN** Turn Timeline完成并开始退出到RunLoop、WalkLoop或Idle
- **THEN** 目标循环Pose MUST从其持续相位继续采样而不是回到frame 0
- **AND** 退出Inertialization MUST使用typed payload中的0.30秒时长
- **AND** Turn有限Pose MUST在下一次进入时从头播放

#### Scenario: 转身期间松开Run意图或停止输入

- **WHEN** MovingTurn执行期间Run意图失效但仍保持方向输入
- **THEN** Timeline完成后 MUST进入WalkLoop
- **AND** **WHEN** 方向输入停止
- **THEN** Timeline完成后 MUST进入WalkEnd
