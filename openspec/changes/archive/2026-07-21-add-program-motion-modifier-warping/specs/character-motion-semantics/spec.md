## MODIFIED Requirements

### Requirement: Motion 必须经过 Contribution、Request、Solver Result 和 Body Sample

Compiled Locomotion与Timeline MotionCurve operation MUST只提交当前Numeric Target的`SimulationMotionContribution`。同一Actor/Tick的全部contribution MUST由唯一Target Motion accumulator按固定channel解析为`ResolvedMotionChannel`；每个channel MUST经过Operation Set规定的唯一Motion Modifier阶段，再由固定channel合成生成一个`CharacterMotionRequest`。正式WorldSolve Pass MUST把Session全部Actor request组成唯一batch；Finalize MUST使用精确匹配的solver result更新World/Character state并产生committed body observation。Graph、Timeline、Action、Modifier与Presentation MUST不直接写Transform或调用具体Solver。没有eligible Modifier时，resolved request MUST与原Contribution仲裁结果逐字段一致。

#### Scenario: Locomotion、Dodge 和 MotionWarp 同 Tick 输出

- **WHEN** Locomotion与Dodge Timeline同Tick提交motion contribution
- **AND** Dodge source成为Action channel resolved owner且其MotionWarp eligible
- **THEN** 唯一Motion accumulator MUST先按channel、priority、weight与blend规则解析channel
- **AND** MotionWarp MUST只修正resolved Action channel
- **AND** WorldSolver MUST只消费最终唯一request

#### Scenario: Program 没有 Motion Modifier

- **WHEN** Program不包含eligible Motion Modifier operation
- **THEN** channel合成与CharacterMotionRequest MUST与迁移前结果逐字段一致

## ADDED Requirements

### Requirement: Motion Modifier 必须是固定且可追踪的 Program 阶段

Motion Modifier的类型、channel与执行顺序 MUST由版本化Operation Set和compiled Program descriptor声明。Runtime MUST不通过反射、字符串handler、ScriptableObject resolver、Network Model或Solver类型动态选择Modifier。Modifier MUST只读取resolved channel、committed Character state、显式Action Context、Program constants与自身typed state，并输出修正后的同一channel。

#### Scenario: 新增 WorldSolver 实现

- **WHEN** Session选择新的WorldSolver backend
- **THEN** Motion Modifier顺序与结果 MUST不改变
- **AND** 新Solver MUST只实现现有World request/result合同

### Requirement: MotionWarp 必须只修正匹配的 Action channel owner

TimelineMotionWarp MUST显式引用一个MotionCurve operation。只有该source成为当前Tick Action channel的resolved owner、Warp窗口active且Action Context有效时，Warp才 MAY修正该channel。同一Actor、Action channel与Tick最多 MUST有一个eligible Warp；动态歧义 MUST fail-stop，不得用priority或遍历顺序挑选。GameplayResult channel MUST在Warp后的Action channel之后按现有规则合成，因此受击或其它GameplayResult motion MUST不被动作Warp扭曲。

#### Scenario: Warp source 输掉 Action channel 仲裁

- **WHEN** Warp引用的MotionCurve没有成为Action channel resolved owner
- **THEN** Warp MUST不修改其它winner
- **AND** Trace MUST记录`SourceNotResolved`或等价typed原因

#### Scenario: GameplayResult 覆盖 Warped Action

- **WHEN** Action channel已被MotionWarp修正
- **AND** GameplayResult channel提交ConsumeLowerChannels的Override
- **THEN** GameplayResult MUST按现有高层channel规则覆盖Warp后的Action结果

### Requirement: MotionWarp 必须在窗口进入时固定总修正

Warp窗口首次active时，Runtime MUST从committed body、源MotionCurve剩余累计轨迹和对应ActionInstance的immutable target snapshot计算nominal authored end、desired target pose及clamped total correction，并写入typed Program state。后续Tick MUST按canonical累计progress差应用position/yaw增量。Timeline generation、ActionInstance或window lifecycle变化时 MUST按typed规则重建或清理状态。

#### Scenario: Rollback 恢复到 Warp 窗口中间

- **WHEN** Snapshot恢复到MotionWarp窗口中间
- **THEN** 下一Tick MUST从保存的generation、总修正和last progress继续
- **AND** MUST不重复应用窗口前半段修正

### Requirement: WorldSolver 必须对 Warped request 保持最终碰撞权威

MotionWarp MUST只修改WorldSolver之前的`CharacterMotionRequest`。Solver input/output MUST不增加MotionWarp、Timeline、Action或target字段。碰撞阻止Warp请求时，Finalize MUST提交Solver actual body result；任何阶段 MUST不在Solver之后补偿目标差值。

#### Scenario: 目标位于墙后

- **WHEN** Warped request试图穿过墙体到达目标
- **THEN** WorldSolver MAY阻止实际位移
- **AND** Presentation MUST显示actual body result
- **AND** Warp MUST不在Finalize或Presentation中把角色拉到目标

### Requirement: Motion Modifier来源必须进入结构化 Trace

Trace MUST关联raw contribution、resolved channel owner、modifier operation、source MotionCurve、ActionInstance、target snapshot、nominal end、desired pose、total/current correction、final request与actual solver result。Diagnostics MUST不读取mutable accumulator、Unity Transform或Solver私有对象。

#### Scenario: 排查目标攻击没有贴近

- **WHEN** 动作未到达目标
- **THEN** Trace MUST能区分source未赢得仲裁、目标缺失、修正被clamp与Solver碰撞阻挡
