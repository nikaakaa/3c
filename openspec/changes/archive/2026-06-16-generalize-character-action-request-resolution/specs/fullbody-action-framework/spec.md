## ADDED Requirements

### Requirement: FullBody Action 请求模型保持纯数据
系统 MUST 在输入缓冲与动作解析之间使用纯数据动作请求模型。该请求模型 MUST 只表达请求类型、来源输入键、origin step、expire step、priority hint、source order、variant hint 和必要值类型 payload。该请求模型 MUST NOT 持有目标 `ActionStateId`、动画 key、motion spec、Unity scene object、Animator、Animancer runtime、controller 或 presenter 引用。

#### Scenario: Attack provider 只提交请求
- **GIVEN** 输入缓冲中存在未过期 Attack 输入
- **WHEN** Attack request provider 读取该输入
- **THEN** provider MUST 输出 `ActionRequestType.Attack` 或等价动作请求
- **AND** 输出 MUST 保留 `InputRequestKind.Attack` 作为来源输入键
- **AND** 输出 MUST NOT 包含 `Action.Attack01`、`Action.Attack02`、`Action.Attack03` 或任何动画 key

#### Scenario: Dodge provider 只提交请求
- **GIVEN** 输入缓冲中存在未过期 Dodge 输入
- **WHEN** Dodge request provider 读取该输入
- **THEN** provider MUST 输出 `ActionRequestType.Dodge` 或等价动作请求
- **AND** 输出 MUST 保留 Dodge 输入的 origin step 与 expire step
- **AND** 输出 MUST NOT 直接成为最终 target state 或 animation request

### Requirement: FullBody Action Provider 与 Resolver 分离
系统 MUST 将动作请求候选收集与动作解析拆分为独立接口。request provider MUST 只负责从输入缓冲、外部请求或 runtime facts 生成动作请求候选；request resolver MUST 负责基于动作请求、当前状态上下文和正式配置解析出可仲裁的纯数据动作结果。arbiter 主流程 MUST NOT 通过硬编码分支把 Attack、Dodge、Jump 或 HitReact 输入直接映射到具体 target state。

#### Scenario: Resolver 输出可仲裁动作结果
- **GIVEN** provider 输出了一个有效动作请求
- **WHEN** request resolver 消费该请求、当前状态上下文和正式配置
- **THEN** resolver MAY 输出 target state、request fact、interrupt request、animation seed 和 motion seed
- **AND** resolver 输出 MUST 保持纯数据
- **AND** resolver MUST NOT 读取 Unity scene object、Animancer runtime 或 InputAction

#### Scenario: 新动作不修改 arbiter 主流程
- **WHEN** 后续新增 Attack、Jump 或 HitReact
- **THEN** 新动作 MUST 通过新增 provider 和 resolver 接入
- **AND** `CharacterActionRequestSubmissionArbiter` 或等价主流程 MUST NOT 新增直接面向具体动作的 target-state switch
- **AND** 多动作候选仍 MUST 使用统一 priority、resistance、timing window 和稳定 tie-break 规则

### Requirement: Dodge 通过通用请求解析路径保持行为
现有 Dodge 行为 MUST 迁移到通用 action request provider/resolver 路径。Dodge provider MUST 只提交 Dodge 请求；Dodge resolver MUST 解析 directional/backstep variant、world direction、priority、target state、animation seed 和 motion seed。迁移后 directional dodge、backstep、rejected request 保留和输入消费语义 MUST 与迁移前一致。

#### Scenario: Directional Dodge 行为保持
- **GIVEN** 输入缓冲中存在 Dodge 输入且当前移动事实支持 directional dodge
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 directional dodge resolved action
- **AND** accepted 后进入的 target state、request fact、motion seed 和 animation seed MUST 与迁移前一致

#### Scenario: Backstep Dodge 行为保持
- **GIVEN** 输入缓冲中存在 Dodge 输入且当前移动事实支持 backstep
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 backstep dodge resolved action
- **AND** accepted 后进入的 target state、request fact、motion seed 和 animation seed MUST 与迁移前一致

### Requirement: Attack 与 Jump 扩展不得绕过通用路径
Attack、Jump 或其它后续动作 MUST 以动作请求和 resolved action 的形式进入 FullBody Action 框架。输入 provider MUST NOT 直接决定连段阶段、跳跃状态、动画 key 或 motion spec；这些结果 MUST 由对应 resolver 基于正式配置和当前状态上下文输出。

#### Scenario: 轻攻击连段由 resolver 决定阶段
- **GIVEN** 输入缓冲中存在 Attack 输入
- **AND** 当前状态与 combo window 支持进入下一段轻攻击
- **WHEN** Attack request resolver 解析该请求
- **THEN** resolver MUST 决定 `Action.Attack01`、`Action.Attack02` 或 `Action.Attack03`
- **AND** Attack provider MUST NOT 直接决定该连段阶段

#### Scenario: Jump 不新增平行入口
- **GIVEN** 输入缓冲中存在 Jump 输入
- **WHEN** Jump action 能力接入 FullBody Action 框架
- **THEN** Jump MUST 通过 request provider 和 resolver 输出 resolved action
- **AND** MUST NOT 新增绕过 CharacterFramePipeline 的 MonoBehaviour 入口
