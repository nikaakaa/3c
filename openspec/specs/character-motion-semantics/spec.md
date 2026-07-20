# character-motion-semantics Specification

## Purpose
定义角色运动的唯一执行语义：Target operation产生`SimulationMotionContribution`，Target Motion accumulator先解析channel并执行Program Motion Modifier，再生成`ResolvedGameplayMotion`；Target Body Motion Integrator执行Prepare后生成唯一`CharacterMotionRequest`，WorldSolver返回实际body result，Body Motion Finalize提交垂直动力状态，Program Finalize提交`CharacterBodySample`与Motion GameplayFact；Unity Transform只在Solver/Presentation边界对齐。

## Requirements

### Requirement: Motion 必须经过 Contribution、Request、Solver Result 和 Body Sample

Compiled Locomotion与Timeline MotionCurve operation MUST只提交当前Numeric Target的`SimulationMotionContribution`。同一Actor/Tick的全部contribution MUST由唯一Target Motion accumulator按固定channel解析为`ResolvedMotionChannel`；每个channel MUST经过Operation Set规定的唯一Motion Modifier阶段，再由固定channel合成为`ResolvedGameplayMotion`。唯一Target Body Motion Integrator MUST读取committed `WorldBodyState`、compiled Body Motion descriptor与TickDelta，在Solver前产生唯一`CharacterMotionRequest`和同Step plan。正式WorldSolve Pass MUST把Session全部Actor request组成唯一batch；Solver返回实际结果后 MUST由同一Target Integrator Finalize垂直动力状态，随后Program Finalize MUST更新Character state并产生committed body observation。Graph、Timeline、Action、Modifier、Presentation与concrete Solver MUST不直接实现重力、写Transform或调用其它Solver。没有eligible Modifier时，ResolvedGameplayMotion MUST与原Contribution仲裁结果逐字段一致。

#### Scenario: Locomotion、Dodge 和 MotionWarp 同 Tick 输出

- **WHEN** Locomotion与Dodge Timeline同Tick提交motion contribution
- **AND** Dodge source成为Action channel resolved owner且其MotionWarp eligible
- **THEN** 唯一Motion accumulator MUST先按channel、priority、weight与blend规则解析channel
- **AND** MotionWarp MUST只修正resolved Action channel
- **AND** Body Motion Integrator MUST在全部Modifier之后加入重力
- **AND** WorldSolver MUST只消费最终唯一request

#### Scenario: Program 没有 Motion Modifier

- **WHEN** Program不包含eligible Motion Modifier operation
- **THEN** channel合成与ResolvedGameplayMotion MUST与迁移前玩法Motion结果逐字段一致
- **AND** CharacterMotionRequest MUST继续由Body Motion Prepare唯一生成

### Requirement: MotionContribution 必须携带完整仲裁语义

`SimulationMotionContribution` MUST表达稳定 source identity、displacement、yaw、ActorLocal或World space、weight、priority、channel、blend mode与ConsumeLowerChannels。字段 MUST实际参与 accumulator解析；系统 MUST不保存只用于展示却不参与运行结果的 priority、weight或consume配置。

#### Scenario: Timeline MotionCurve 提交位移

- **WHEN** Timeline MotionCurve clip在当前Tick产生delta
- **THEN** contribution MUST携带其Timeline/Track/Clip source identity和完整仲裁字段
- **AND** MUST不绕过accumulator覆盖最终request

### Requirement: MotionContribution 必须区分 Delta 与低层 Channel 占用

Contribution MUST分别表达本Tick是否具有有效位移/yaw delta，以及是否通过 `Override + ConsumeLowerChannels` 声明channel占用。零delta Override claim MUST可以成为winner并清除已累计低层motion；零delta Additive或WeightedBlend MUST被忽略且不得消费低层channel。

#### Scenario: 攻击 Recovery 保持原地

- **WHEN** Attack MotionCurve已经到达累计曲线终点
- **AND** clip仍处于正式占权区间且配置ConsumeLowerChannels
- **THEN** Timeline MUST提交零delta Action channel claim
- **AND** Locomotion contribution MUST在该Tick被消费

### Requirement: MotionCurveClip 必须分开曲线结束与占权结束

`MotionCurveClip` MUST显式保存满足 `StartFrame < CurveEndFrame <= EndFrame` 的CurveEndFrame。累计位置与yaw曲线 MUST只在StartFrame到CurveEndFrame之间采样；CurveEndFrame到EndFrame之间 MUST保持曲线终值，并按Override/ConsumeLowerChannels配置继续提交零delta claim。非法CurveEndFrame MUST作为配置或编译错误，不得按EndFrame猜测补齐。

#### Scenario: 位移曲线早于 Recovery 结束

- **WHEN** 攻击位移曲线先于Timeline clip结束
- **THEN** delta MUST在CurveEndFrame停止
- **AND** Action channel ownership MAY按clip配置持续到EndFrame

### Requirement: Motion accumulator 必须使用固定 Channel 与 Blend 规则

Target Motion accumulator MUST按 `Locomotion -> Action -> GameplayResult` 的稳定channel顺序解析，且只支持 `Additive`、`WeightedBlend` 与 `Override`。Override winner MUST按priority选择；同priority MUST保持Program traversal产生的稳定顺序。系统 MUST不引入脚本公式、动态resolver注册或按Network Model改变仲裁规则。

#### Scenario: GameplayResult 覆盖动作位移

- **WHEN** Action与GameplayResult channel同时具有合法Override contribution
- **THEN** GameplayResult channel MUST在Action之后解析
- **AND** ConsumeLowerChannels MUST决定是否替换已累计低层结果

### Requirement: Timeline MotionCurve 必须是动画位移的唯一事实来源

Compiled Timeline MotionCurve operation MUST按SimulationTick与canonical fraction求值并产生motion contribution。AnimationTrack、AnimationClip、Animancer fade、PresentationFrame与Animator root motion MUST不产生或修改Gameplay位移。

#### Scenario: Dodge Timeline 同时包含动画和MotionCurve

- **WHEN** Dodge Timeline采样AnimationTrack与MotionCurve
- **THEN** AnimationTrack MUST只生成Presentation producer command
- **AND** MotionCurve MUST独立进入统一motion accumulator

### Requirement: Motion 来源必须可追踪

Structured Trace与Source Map MUST关联source operation、Timeline/Track/Clip identity、ActionInstance、contribution、ResolvedGameplayMotion、Body Motion Prepare、final request、World batch、solver result、Body Motion Finalize与committed body sample。Diagnostics MUST不读取pending evaluation私有集合、mutable integration plan或Solver mutable object。

#### Scenario: 排查 Dodge 覆盖 Locomotion

- **WHEN** Dodge contribution消费Locomotion channel
- **THEN** Trace MUST显示winning source、consume原因、最终request与actual result

### Requirement: 旧 BBB Motion 数据不得恢复

正式 Character runtime MUST不引用 `BBBNexus.MotionClipData`、`BBBNexus.WarpedMotionData`、旧 `PlayerSO` motion配置、`MotionProposal` 或以AnimationClip root motion作为Gameplay位移数据源。需要新运动行为时 MUST通过Semantic IR operation、Timeline MotionCurve或正式World request扩展当前链路。

#### Scenario: 新增动作位移

- **WHEN** 作者为新攻击或闪避配置位移
- **THEN** MUST使用当前MotionCurve与Program编译链
- **AND** MUST不复制旧BBB motion资产或恢复第二套motion runtime

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

MotionWarp MUST只修改Body Motion Prepare之前的`ResolvedGameplayMotion`。Solver input/output MUST不增加MotionWarp、Timeline、Action或target字段。碰撞阻止Warp请求时，Body Motion Finalize与Program Finalize MUST提交Solver actual body result；任何阶段 MUST不在Solver之后补偿目标差值。

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
