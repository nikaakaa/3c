## MODIFIED Requirements

### Requirement: TreeClip 与 Scope Variable 必须是 Timeline Window 唯一作者入口

Decision TreeClip 与 owner-local Bool Frame scope variable MUST继续作为 Timeline Window 唯一时间作者入口。Projection MUST只保存 WindowType、WindowId、Digest 和 Action Context provenance；authority、history、replication 和 packet policy MUST来自当前 Network Model profile，不得保存在 ActionProfile、TreeClip 或 declaration。ConditionRuleGraph MAY通过 `ActionWindowActiveInfoNode` 按 WindowType 只读同一逻辑帧已经暂存的 projection candidate；该读取 MUST不生成第二份 Window fact、Blackboard key、cache 或 registry。RootTree Blackboard MUST不为了每段攻击暴露一次性 Cancel/MoveCancel declaration。

#### Scenario: Attack HitWindow

- **WHEN** TreeClip 在本 tick 写入 HitWindow declaration
- **THEN** projection MUST生成对应 ActionWindow fact
- **AND** ServerAuthoritative model policy MUST决定是否进入 history/packet

#### Scenario: Transition 读取 RecoveryEarly

- **WHEN** Attack inline Timeline 的 TreeClip 在当前帧写入 projected `RecoveryEarly`
- **THEN** ConditionRuleGraph MUST能在同帧通过 typed WindowType 查询得到 true
- **AND**查询与 EndFrame fact MUST引用同一 ActionInstance、WindowId 和 Digest
- **AND**系统 MUST不要求 RootTree 声明 `AttackNRecoveryEarly` 兼容变量

#### Scenario: 未投影的普通局部变量

- **WHEN** TreeClip 写入 Projection=None 的 owner-local scope variable
- **THEN**普通 Blackboard reader MAY读取该变量
- **AND** `ActionWindowActiveInfoNode` MUST不把它识别为 ActionWindow

## REMOVED Requirements

### Requirement: Dodge Action 必须通过 pipeline blackboard 公布 locomotion ownership

**Reason**: 该要求把 full-body locomotion ownership 绑定到 Dodge 名称、`IsDodging` 和 `CanDodgeMoveCancel`，使攻击与未来技能需要复制同一所有权合同，并把无输入恢复错误路由到 RunEnd。

#### Scenario: 删除 Dodge 专用 ownership 口径

- **WHEN** Corin Action 迁移到通用 locomotion ownership
- **THEN**系统 MUST不再创建或读取 `IsDodging`、`CanDodgeMoveCancel` 或同义兼容 key
- **AND** Locomotion MUST不按 ActionId 或 Dodge 类型分支

## ADDED Requirements

### Requirement: Full-body Action 必须通过唯一 pipeline blackboard 事实公布 locomotion ownership

Corin 的 Attack、Dodge 与未来 full-body Action MUST通过唯一 pipeline Blackboard `HasActionLocomotionOwnership` 让渡 locomotion。Action MUST在 ActionInstance 成功激活后设置 true，并在所有 source exit 路径对称设置 false。Locomotion MUST只读取该 ownership fact，不得复制 Action request、ActionProfile、Timeline、motion curve、window 或 lifecycle。`ResumeLocomotionThroughRunEnd` 与任何按动作种类选择恢复状态的路由事实 MUST被删除。

#### Scenario: Full-body Action 激活后让渡所有权

- **WHEN** Attack 或 Dodge 成功激活 ActionInstance
- **THEN**对应 OnEnter MUST写入 `HasActionLocomotionOwnership=true`
- **AND** Locomotion StateMachine MUST进入无表现输出的 ActionOverride

#### Scenario: Action 正常完成或被替换

- **WHEN** full-body Action 正常完成、被 State transition 替换或被上层 tree stop
- **THEN** source OnExit MUST写入 `HasActionLocomotionOwnership=false`
- **AND** Locomotion MUST按当前 Move input 在 RunLoop 或 Idle 中收回所有权

#### Scenario: 单一 Action 业务真相

- **WHEN** Locomotion 处理 full-body Action 活跃期间的所有权
- **THEN** Locomotion MUST NOT创建第二个 Action state 或引用 Action Timeline
- **AND** Action request MUST继续只由 target activation 接受点消费
