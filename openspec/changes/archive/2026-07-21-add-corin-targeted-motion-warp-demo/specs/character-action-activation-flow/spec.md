## MODIFIED Requirements

### Requirement: ActionProfile 必须类型化声明目标快照要求

ActionProfile MUST 使用 `ActionTargetRequirement` 明确声明 `None`、`OptionalSnapshot` 或 `SnapshotRequired`，MUST NOT 使用自由字符串 TargetPolicy。Action catalog、Semantic IR 和两个 Numeric Target MUST 保存同一 typed 值。未知值或缺失值 MUST 在 artifact 发布前失败。配置 MotionWarp 的动作 MUST声明 `OptionalSnapshot` 或 `SnapshotRequired`；声明 `None` 时 MUST 在发布前失败。

`OptionalSnapshot` MUST 表达正式业务策略：有候选目标时 ActionInstance 固定保存目标快照并允许 MotionWarp；无候选目标时动作仍可激活，MotionWarp MUST保留源 MotionCurve 且输出 typed 原因。该语义 MUST NOT 通过捕获异常、静默禁用或运行时 fallback 实现。

#### Scenario: 普通无目标闪避

- **WHEN** Dodge ActionProfile 声明 `None`
- **THEN** admission MAY 在没有 target snapshot 时成功
- **AND** 该动作 MUST NOT 包含 MotionWarp

#### Scenario: 可选目标攻击没有目标

- **WHEN** Attack ActionProfile 声明 `OptionalSnapshot`
- **AND** candidate target snapshot 为 None
- **THEN** admission MUST允许创建无目标快照的 ActionInstance
- **AND** 对应 MotionWarp MUST保留源 MotionCurve

#### Scenario: 可选目标攻击获得目标

- **WHEN** Attack ActionProfile 声明 `OptionalSnapshot`
- **AND** candidate target snapshot 有效
- **THEN** admission MUST允许动作激活
- **AND** ActionInstance MUST固定保存该快照供 MotionWarp 使用

#### Scenario: 必需目标攻击缺少快照

- **WHEN** ActionProfile 声明 `SnapshotRequired`
- **AND** candidate target snapshot 为 None
- **THEN** admission MUST返回 `TargetSnapshotRequired` 或等价 typed 原因
- **AND** MUST NOT 创建 ActionInstance 或启动 Timeline

### Requirement: 动作准入查询与提交必须读取同一目标候选

`CanActivateAction` 与 `ActivateActionInstance` MUST把同一显式 Blackboard `ActionTargetSnapshot` 或显式 None 传入唯一 portable admission evaluator。纯查询与最终提交 MUST对 `None`、`OptionalSnapshot` 和 `SnapshotRequired` 得到一致结果；系统 MUST NOT允许查询忽略目标而提交阶段再失败，也 MUST NOT在激活后从 Scene、Transform、Presentation 或 registry 补查目标。

#### Scenario: Transition 查询通过后激活必需目标动作

- **WHEN** Transition 条件使用 `CanActivateAction` 检查 `SnapshotRequired` 动作
- **AND** target snapshot 在同一准入输入中有效
- **THEN** 最终 `ActivateActionInstance` MUST使用同一候选快照
- **AND** 创建的 ActionInstance MUST固定保存该快照

#### Scenario: Transition 查询通过后激活可选目标动作

- **WHEN** Transition 条件使用 `CanActivateAction` 检查 `OptionalSnapshot` 动作
- **THEN** 最终 `ActivateActionInstance` MUST读取同一 Blackboard declaration
- **AND** 查询与提交 MUST对目标存在或缺失得到相同语义
