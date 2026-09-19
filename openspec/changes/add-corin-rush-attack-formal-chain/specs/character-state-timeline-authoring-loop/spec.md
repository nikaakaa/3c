## MODIFIED Requirements

### Requirement: Corin RushAttack必须使用独立正式状态Timeline链路

Corin `RushAttack` MUST 是独立正式 Ability / FSM 链，MUST 拥有 ZZZ 的 8 个业务状态：`Attack_Rush`、`Attack_Rush_Explode`、`Attack_Rush_End`、`Attack_Rush_Enhance`、`Attack_Rush_Enhance_Loop`、`Attack_Rush_Enhance_End`、`Attack_Rush_Enhance_Explode` 和 `Attack_Rush_Enhance_Explode_End`。每个状态 MUST 拥有状态本地 Timeline producer；7 个实际 Motion 可复用，但播放身份和 `StateId` MUST 不合并。Rush MUST NOT 追加到 NormalAttack 五段 Timeline，也 MUST NOT 改为 Locomotion PoseStateMachine 状态。

Rush 状态 Timeline MUST 使用 ZZZ 源总帧 70、70、80、44、86、70、70、118 作为状态时长基准，MUST 用正式 Logic TreeClip / FSM 条件表达 frame=13/23/70、frame=35、frame=43/81/86、frame=40/55/70 和 frame=55/118 边界。原 `special_*` 目标 MUST 交给 Hit、Evade、Aid、Switch 等正式宿主 owner；缺失 owner 时 MUST 显式失败或保持不可用，MUST NOT 伪造路由。

#### Scenario: Rush独立于NormalAttack

- **WHEN** 作者打开 Rush 或 NormalAttack authoring
- **THEN** Rush 的 8 个状态 MUST 位于独立 Rush Ability / Timeline 链
- **AND** NormalAttack 的五段 Timeline MUST NOT 增加 Rush 状态或 Rush cue

#### Scenario: 普通与强化Rush分支

- **WHEN** Dodge owner 按原始条件提交 Rush activation
- **THEN** `Badge_S01` MUST 只选择 `Attack_Rush` 或 `Attack_Rush_Enhance` 其中一个入口
- **AND** 后续状态 MUST 携带 `Rush` 或 `Rush_Enhance` 分支身份

#### Scenario: 状态本地命中cue

- **WHEN** Rush Timeline 经过 AttackProperty 事件帧
- **THEN** Logic ActionCue MUST 保留原始 `Corin_Attack_Rush*` key、`StateId`、状态本地帧和 `BranchId`
- **AND** 伤害、碰撞、相机和其它攻击 payload MUST 留在 GameplayEffect / Ability / Camera owner

#### Scenario: Loop与共享Motion不合并身份

- **WHEN** `Attack_Rush_Enhance_End` 与 `Attack_Rush_Enhance_Explode_End` 复用或引用共享 Motion 证据
- **THEN** 两个状态 MUST 保留独立 Timeline producer、播放身份和 StateId
- **AND** Loop 重入 MUST 保持同一个 `Attack_Rush_Enhance_Loop` 状态身份
