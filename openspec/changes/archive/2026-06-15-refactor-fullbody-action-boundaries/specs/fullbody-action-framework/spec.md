## ADDED Requirements
### Requirement: FullBody 请求候选模块化准入
FullBody 主调度入口 MUST 通过一组纯数据 request submission provider 构建本帧可仲裁请求。`CharacterActionRequestSubmissionArbiter` MUST 只负责收集候选、调用统一仲裁入口、按确定性规则选择 accepted request，并输出可被统一状态机消费的 `CharacterInputRequestFact` 或等价事实。它 MUST NOT 在主流程中手写 Dodge、TurnBack、Attack、Jump 或 HitReact 的候选构建分支。

#### Scenario: Dodge 候选由专用 builder 贡献
- **GIVEN** 输入缓冲中存在未过期 Dodge 输入
- **WHEN** FullBody request submission arbiter 处理本帧请求
- **THEN** Dodge request candidate builder MUST 生成 Dodge 候选请求
- **AND** gate 主流程 MUST NOT 直接调用 `BuildDodgeRequestFact`
- **AND** accepted 后 MUST 输出 `InputRequestKind.Dodge` request fact

#### Scenario: TurnBack 候选由专用 builder 贡献
- **GIVEN** Locomotion facts 中存在有效 TurnBack intent
- **WHEN** FullBody request submission arbiter 处理本帧请求
- **THEN** TurnBack request candidate builder MUST 生成 TurnBack 候选请求
- **AND** gate 主流程 MUST NOT 直接调用 `BuildTurnBackRequestFact`
- **AND** accepted 后 MUST 输出 `InputRequestKind.TurnBack` request fact

#### Scenario: 新请求不修改 gate 主流程
- **WHEN** 后续新增 Attack 或 Jump 请求候选
- **THEN** 新请求 MUST 通过新增 request candidate builder 接入
- **AND** MUST NOT 要求在 gate 主流程中新增 `InputRequestKind.Attack` 或 `InputRequestKind.Jump` 分支
- **AND** 多个 accepted request MUST 按 priority 和稳定 tie-break 规则选择一个

### Requirement: Action Motion Resolver 只消费通用规格
Action motion resolver MUST 只消费状态机 frame 产出的通用 `ActionMotionSpec`、delta time、timeline facts 和必要的前帧纯数据 facts。Dodge、Attack、Jump 或其它动作的配置解析 MUST 在 spec 构建阶段或动作专用 adapter 中完成，resolver MUST NOT 读取动作专用配置类型或按具体 action id 分支重算 duration、distance、rotation。

#### Scenario: Dodge motion 数值进入通用 spec
- **GIVEN** 当前状态为 Dodge Directional 或 Backstep
- **WHEN** FullBody pipeline 构建 action motion input
- **THEN** Dodge 的 duration、distance、rotateToDirection 和 run latch 语义 MUST 已经进入通用 motion spec
- **AND** `ActionMotionResolver` MUST NOT 读取 `DodgeActionConfig`
- **AND** resolver 输出的 `ActionMovementCommand` MUST 与迁移前数值一致

#### Scenario: 新动作 motion 不修改 resolver 分支
- **WHEN** 后续新增 Attack 或 Jump motion spec
- **THEN** resolver MUST 能按通用 spec 计算 command、completed 和 run latch 结果
- **AND** MUST NOT 要求新增 `ActionStateIds.Attack`、`ActionStateIds.Jump` 或等价业务分支

#### Scenario: 运动执行出口保持唯一
- **WHEN** action motion resolver 输出运动结果
- **THEN** FullBody pipeline MUST 继续通过现有 `ActionMovementCommand -> IActionMovementExecutor` 出口执行
- **AND** resolver MUST NOT 直接调用 `CharacterController.Move`
- **AND** resolver MUST NOT 直接写角色 Transform
