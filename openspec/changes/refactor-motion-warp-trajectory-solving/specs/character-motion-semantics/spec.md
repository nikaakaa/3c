## RENAMED Requirements

- FROM: `MotionWarp 必须在窗口进入时固定总修正`
- TO: `MotionWarp 必须在窗口进入时固定累计轨迹上下文`

## MODIFIED Requirements

### Requirement: MotionWarp 必须在窗口进入时固定累计轨迹上下文

Warp窗口首次active时，Runtime MUST从committed Body、源MotionCurve在Warp窗口StartFrame与EndFrame之间的累计姿态、对应ActionInstance的immutable target snapshot及compiled descriptor建立唯一累计轨迹上下文。上下文 MUST保存窗口开始Body姿态、源窗口起始姿态、有效Target Pose、Limit结果、previous Warped Cumulative Pose、progress、generation、ActionInstance与source identity。后续Tick MUST采样当前Source Window Pose，通过唯一Translation/Rotation Solver生成当前Warped Cumulative Pose，再用当前与previous累计pose之差修正同一Action channel。Runtime MUST不冻结独立world-space position/yaw residual后再按变化Body yaw重积分源delta。

#### Scenario: 大角度yaw修正同时存在源位移

- **WHEN** 源MotionCurve在Warp窗口内同时包含ActorLocal平面位移和yaw
- **AND** Rotation Solver增加额外yaw修正
- **THEN** Translation Solver MUST消费同一个累计yaw结果生成Warped Cumulative Pose
- **AND** 当前Tick输出 MUST由相邻Warped Cumulative Pose做差
- **AND** 源delta MUST不再按当前Body yaw二次旋转

#### Scenario: Rollback恢复到Warp窗口中间

- **WHEN** Snapshot恢复到MotionWarp窗口中间
- **THEN** 下一Tick MUST从保存的窗口上下文与previous Warped Cumulative Pose继续
- **AND** MUST不重新捕获目标、不重复应用历史progress或重建不同有效Target Pose

#### Scenario: Warp窗口早于源MotionCurve结束

- **WHEN** MotionWarp EndFrame早于source CurveEndFrame
- **THEN** Warp目标时刻 MUST是MotionWarp EndFrame
- **AND** 窗口后的source MotionCurve delta MUST继续沿正式Action channel运行
- **AND** Runtime MUST不按CurveEndFrame偷换Warp终点

## ADDED Requirements

### Requirement: MotionWarp必须把目标姿态生成与轨迹解算分离

Runtime MUST先依据target snapshot、Offset Space与offset生成Target Pose，再由Translation Mode与Rotation Mode/Method生成累计轨迹。Offset MUST不作为独立末尾delta重复应用；solver MUST不查询Scene Transform、Animator、Camera、Network Model或concrete WorldSolver。

#### Scenario: 同一Target Pose切换轨迹方法

- **WHEN** 两个Clip使用相同target snapshot、offset空间和offset但选择Scale与Skew
- **THEN** 两者 MUST得到相同窗口结束Target Pose
- **AND** 中间轨迹 MAY按各自solver不同
- **AND** Target生成规则 MUST不因solver改变

### Requirement: MotionWarp碰撞损失不得在后续Tick追补

MotionWarp MUST只提交相邻作者累计pose之间的当前Tickdelta。WorldSolver裁掉某Tick位移后，Modifier MUST不把actual Body与作者累计pose的差加入后续request，不得在Finalize或Presentation补偿。Trace MUST能关联请求delta与Solver actual result。

#### Scenario: Warp路径被墙体阻挡

- **WHEN** WorldSolver阻止一个Warp Tick的部分位移
- **THEN** committed Body MUST使用Solver actual result
- **AND** 下一TickMotionWarp MUST只提交下一段作者累计delta
- **AND** MUST不提高速度追赶被阻挡的目标

### Requirement: MotionWarp限制结果必须是typed业务结果

当目标需要量超过compiled最大平面或yaw修正时，Runtime MUST严格执行`ApplyClamped`或`PreserveSource`。ApplyClamped MUST计算有效受限Target Pose并输出`AppliedClamped`；PreserveSource MUST保持resolved source且不建立Warp state。未知策略、非法basis或solver前置条件失败 MUST fail-stop，不能被当成PreserveSource。

#### Scenario: Clamp只达到有效目标

- **WHEN** ApplyClamped限制了目标位置或yaw
- **THEN** 累计轨迹终点 MUST达到受限有效Target Pose
- **AND** Trace MUST同时记录原始Target Pose、限制和有效Target Pose

### Requirement: MotionWarp必须只替换匹配source的运动部分

Runtime MUST用相邻Warped Cumulative Pose得到`warped source delta`，并取得匹配resolved owner在当前Tick与Warp窗口交集内实际进入Action channel的`raw source delta`。Modifier MUST只把两者之差作为correction应用到现有resolved Action channel，使窗口交集内的source部分变成warped结果，同时保留窗口外source delta与同channel其它合法贡献。Runtime MUST不把完整warped delta叠加到raw source上，也不得覆盖整个resolved channel。

#### Scenario: Action channel包含source与额外Additive贡献

- **WHEN** Warp source成为resolved owner
- **AND** 同Action channel还有合法Additive motion
- **THEN** Modifier MUST只将owner raw source替换为warped source
- **AND** Additive motion MUST继续保留
- **AND** 最终Action channel MUST不包含重复source delta

#### Scenario: 一个Tick跨越Warp窗口结束

- **WHEN** 当前逻辑Tick的Timeline segment从Warp窗口内跨到EndFrame之后
- **THEN** Modifier MUST只扣除该Tick在Warp窗口内的raw source delta
- **AND** EndFrame之后的source delta MUST继续保留在Action channel
- **AND** MUST不因整Tick owner替换而丢失窗口外轨迹
