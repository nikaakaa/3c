# Design: Deterministic Rollback最终产品闭包

## Context

当前代码已经存在Gameplay Lab场景、Local Fixed与Rollback Variant、共享`CorinDeterministicKcc.asset`、`DeterministicCollisionWorldAuthoring`、Rollback Build adapter和双Peer/Relay产品。缺口不是再设计一套Runtime，而是把这些入口从“分别能找到相似配置”收敛成“共同引用同一组正式身份”。

## Decision 1: Variant是产品装配引用，不是第二作者真相

Gameplay Lab的两个Variant只引用正式Composition、Source、Character Definition、Fixed Program、Projection、KCC与collision artifact。Variant不得内联复制Program payload、KCC参数或场景几何。

业务取舍：独立Variant便于选择Local Fixed或Rollback运行方式，但共享底层资产才能保证差异只来自Network Model，而不是角色或世界配置漂移。

## Decision 2: Character产品先发布，Rollback产品后打包

正式顺序：

```text
Corin authoring
  -> Semantic IR
  -> Fixed Program + Presentation Projection
  -> Gameplay Lab shared Variant closure
  -> KCC + Collision Artifact identity
  -> Rollback Network Test Product manifest
```

Network adapter只消费已经发布并校验的输入，不调用Character编译器、Pose编译器、KCC迁移器或Collision Baker。

## Decision 3: 可见环境是唯一Collision作者源

Gameplay Lab场景中唯一`DeterministicCollisionWorldAuthoring`及其surface marker同时服务Local Fixed与Rollback Variant。显式Bake生成一个规范artifact；两个Variant只引用该artifact及其hash。不得为Player Build生成隐藏碰撞地图。

## Decision 4: 产品闭包只消费现有KCC

Fixed KCC已经进入Rollback运行组合。本change不修改Motor、台阶算法或KCC配置，只让Local Fixed、Rollback Variant与Product manifest引用现有同一KCC identity；任何后续台阶算法重构都是独立工作，不阻塞本产品闭包。

## Decision 5: MovingTurn由短Timeline独占Body Root Motion

MovingTurn的Gameplay入口只允许从RunLoop以接近反向的输入进入。唯一门禁同时要求方向输入存在、朝向误差达到135°、Attack Action Context未激活且Dodge Action Context未激活；动作仍拥有角色期间不得在其下方隐藏选择MovingTurn，动作退出后若反向输入仍成立则由同一边只选择一次。135°为camera-relative输入留下45°触发余量，而Timeline完成已经是唯一释放条件，因此不会恢复旧版同阈值进入和退出造成的提前释放。RunEnd重新收到输入时先回到规范RunLoop，再由同一`RunLoop -> MovingTurn`门禁决定，不复制条件。它不把180°作者曲线缩放成任意目标角，而是在60Hz Timeline中播放0–28帧：前25帧完成固定180° yaw，后3帧保留姿态收束；X/Z直接使用Root Motion Baker从`Animator.deltaPosition`采样得到的Unity米制值，29个贡献累计为`(-0.9001478, 0, 0.4623734)`，不能再乘`0.01`、只删除横向分量或另造修正轨迹。Timeline完成是唯一释放条件，之后按Run意图回到RunLoop或WalkLoop，停止输入则进入WalkEnd。

Pose Graph只播放对应Turn Sequence，不再用RootOrientationWarp第二次解释yaw。Presentation中的RunStart与RunEnd可以在观察到Gameplay已经提交MovingTurn后转入同一Turn Pose，用于吸收逻辑提交和表现观察的帧差；这不是额外Gameplay入口。RunStart、RunLoop与RunEnd进入Turn使用0.12秒Inertialization，使0.4667秒短动作不会把过半时长消耗在淡入；Turn退出到RunLoop、WalkLoop或Idle使用0.30秒Inertialization，使残余姿态在普通移动恢复后继续衰减。Idle、WalkLoop与RunLoop保留连续相位，有限状态仍在进入时重置。这样Body轨迹、可见动画和Rollback中的确定性运动都来自同一Gameplay Timeline，Pose Sequence继续只按PresentationDelta播放，不读取Gameplay MotionCurve采样时钟。

## Decision 6: Fixed包装产物与Fixed运行定义必须保持不同所有权

`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`是显式Fixed Build的唯一包装产物目标；`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgramRuntime.asset`是Composition引用的`FixedProgramRuntimeDefinition`，不得作为Program包装产物写入。Gameplay Lab从精确Fixed Program创建Presentation Contract并直接校验Projection的ProgramId、SourceRevision、SemanticHash与ProjectionRevision，不再借Float32包装资产的发布元数据判断Fixed闭包是否过期。

业务取舍：Float32与Fixed可以拥有各自ProgramHash和ABI，但两次显式发布结束后必须共享同一Program identity、SourceRevision与SemanticHash，Projection再绑定该共享语义。这样本地与Rollback选择数值目标时不会引入第二份角色语义，也不会让正确的Fixed产品被Float32专用状态误判为过期。

## Failure Model

- Corin authoring、Semantic IR、Fixed Program或Projection identity不一致：停止在产品Build前。
- Local Fixed与Rollback Variant引用不同KCC、Program、Projection或collision artifact：拒绝闭包。
- collision artifact与可见authoring hash不匹配：要求显式Bake，不自动修复。
- Product candidate缺失或混入旧identity：原子发布失败，保留上一正式Product。
- Run发现manifest或exact closure不匹配：启动任何进程前失败。

## Final Chain

```text
Document v3 authoring
  -> explicit Character Fixed Build
  -> shared Gameplay Lab closure
  -> existing Deterministic KCC identity closure
  -> explicit Rollback Product Build
  -> manifest-only Run
  -> Relay + Peer A + Peer B
```
