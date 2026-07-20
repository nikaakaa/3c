# 实施清单

## 1. 实施前基线

### Program与Semantic IR

| 项目 | 实施前 | 当前代码目标 |
|---|---:|---:|
| Semantic IR artifact | v8 | v9 |
| Compiler | `character-simulation-compiler/18` | `character-simulation-compiler/19` |
| Operation Set | `character-gameplay-operations/7` | `character-gameplay-operations/8` |
| Float32 Target ABI | 5 | 6 |
| Fixed Q32.32 Target ABI | 4 | 5 |
| Float32 Program artifact | v11 | v12 |
| Fixed Program artifact | v11 | v12 |

Body Motion实施前没有Semantic descriptor、Program descriptor或`AirborneVerticalMotion`能力。Motion accumulator在Motion Modifier后直接构造`CharacterMotionRequest`。

### Character与World状态

| 状态边界 | 实施前 | 当前代码目标 |
|---|---:|---:|
| Float32 Character State identity | `character-state/float32/v5` | `character-state/float32/v6` |
| Fixed Character State identity | `character-state/fixed-q32.32/v4` | `character-state/fixed-q32.32/v5` |
| Float32 WorldState codec | v2 | v3 |
| Float32 WorldSnapshot codec | v2 | v3 |
| Float32 SessionSnapshot codec | v1 | v2 |
| Fixed WorldState codec | v2 | v3 |
| Fixed WorldSnapshot codec | v3 | v4 |
| Fixed SessionSnapshot codec | v2 | v3 |

实施前`WorldBodyState`只有Position、Yaw、actual Velocity、Grounded与Collision，没有会影响下一Tick的独立垂直动力状态。

### ServerAuthoritative状态

| 边界 | 当前代码目标 |
|---|---:|
| Prediction Correction | v4 |
| Prediction History | v3 |
| Prediction Journal | v2 |
| Authority Baseline | v3 |
| Network Checkpoint | v2 |
| Authority Replication Egress | v3 |
| Remote Presentation Egress | v3 |

Baseline、History、Checkpoint与Egress的Body canonical payload都显式写入`VerticalVelocity`，旧版本没有兼容reader或缺字段默认。

### Deterministic Rollback状态

| 边界 | 当前代码目标 |
|---|---:|
| Model semantic version | 5 |
| Protocol version | 5 |
| Pipeline revision | 5 |
| Protocol codec | v5 |
| State hash egress schema | v2 |
| Restore source schema | v2 |

Rollback完整Fixed WorldSnapshot保存Position、actual Velocity、VerticalVelocity、Grounded、Collision与KCC stable support state。Input codec仍是v3，因为输入payload没有Body字段。

## 2. Authoring与Corin

- Definition GUID：`c7a7c1e3f7e64d81b5a04a90cbeb8d4e`。
- Definition路径：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- Body Motion Profile路径：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinBodyMotionProfile.asset`。
- Profile GUID：`f3b855f07e654466a8777141908fbef2`。
- Profile semantic version：1。
- `GravityAcceleration`：-25。
- `MaximumFallSpeed`：40。

Definition显式引用RootTree、Input、GameplayEffect、Body Motion、Animation Presentation、Action、Gameplay Behavior以及generated Program/Projection。Gravity与fall speed只存在Profile，不内联到Definition、Scene、Network Model、Solver或Blackboard。

实施前Corin generated wrapper仍记录：

- SourceRevision：`d416c304e5501a0df48b18dad9bc8422b714952bf35ac1a258648e6cb8998e67`。
- SemanticHash：`c72c394532876a177147685dbffeed5a3d6eedbf58f465989de5a7301ea54af9`。
- Float ABI：5。
- ProgramHash：`6179910592f9ed5d488d899b3b6f191f59c24726c6c56ae74228c484986e3c03`。
- LayoutHash：`6d385072ad833d689717f2757fcbe4134d885a6c6570432bf24122452cd1863b`。

这些是迁移前基线，不是最终产物。正式`CharacterSimulationBuildOrchestrator`与Fixed Target Compiler已生成最终产物：

- SourceRevision：`f73a622d0ebd694285b3150bc779afdc660f407b19fbe474d2b6c8b7621e3a34`。
- SemanticHash：`9a70ce7c67fadeb69af816d13cd333c70a20403a52bc0bbe4eb6e5723308802d`。
- Float32 ProgramHash：`cafe3c6ff07c114b370b998ad6b758c1f2a3018590b2f0e3d8977b36d34e87c3`。
- Float32 LayoutHash：`92662b682e7ed331e8cda6c911a4f38f3944eecdf49d4dc9e8d4d594d3d08ef7`。
- Fixed ProgramHash：`d8640f089c5a9f98f8f5315f57d71f515a8a1cda0cc9c88b1760c2e673f1dbf0`。
- Fixed LayoutHash：`f1c62affc958bfcb82639d4a44639f2c9c765a3be9be7b8552be5c5ea070f71f`。
- Body Motion Revision：`668e493d213e8a3c90569da1c84d3439456db13f44c66176fe06f0d24cc2401b`。

正式生成路径：

- Semantic IR：`Library/CharacterSimulation/SemanticIr/c7a7c1e3f7e64d81b5a04a90cbeb8d4e.csir`。
- Float32 Program：`Library/CharacterSimulation/Programs/c7a7c1e3f7e64d81b5a04a90cbeb8d4e/float32-ieee754-abi6.csim`。
- Fixed Program：`Library/CharacterSimulation/Fixed/c7a7c1e3f7e64d81b5a04a90cbeb8d4e.fixed-program`。
- Unity wrappers：`Assets/Configs/Character/Corin/Pipeline/Definition/Generated/`。

产品identity没有独立手填副本：

- Local composition从Corin Definition当前Program与Unity Solver descriptor构造Session identity。
- Unity Authority产品由`UnityAuthorityNetworkTestProductAdapter`在构建时读取当前Program identity与Authority source/pipeline identity。
- Deterministic Rollback的`CorinFixedProgram.asset`与`CorinRollbackComposition.asset`已由正式Prepare入口更新到上述Fixed Program identity。
- DotRecast Authority Scene Manifest分别保存Program required capabilities与Solver capabilities；当前Program要求`AirborneVerticalMotion`而DotRecast不声明它，因此正式Composition拒绝，不生成兼容产品配置。

## 3. 唯一运行链路

### 实施前

```text
MotionContribution
-> channel resolve
-> Motion Modifier
-> CharacterMotionRequest
-> ResolveBatch
-> Solver final body
-> Program Finalize
```

### 实施后

```text
MotionContribution
-> ResolvedMotionChannel
-> Motion Modifier
-> ResolvedGameplayMotion
-> Target Body Motion Prepare
-> CharacterMotionRequest + transient integration plan
-> Session唯一ResolveBatch
-> Solver actual displacement + stable Grounded + Above/Below
-> Target Body Motion Finalize
-> committed WorldBodyState.VerticalVelocity
-> Program Finalize
```

Prepare使用固定半隐式顺序：

```text
candidate = max(previousVerticalVelocity + gravity * dt, -maximumFallSpeed)
gravityDelta = candidate * dt
requestY = gameplayY + gravityDelta
```

Finalize只让稳定Grounded清除向下速度，让Above清除向上速度；普通Below接触、坡面几何上升和actual Velocity.Y都不能重建或清除垂直动力。

## 4. Solver能力

| Solver | Version | AirborneVerticalMotion | 处理方式 |
|---|---:|---|---|
| Unity CharacterController | 2 | 支持 | 消费完整XYZ request，映射CollisionFlags，调用Float32唯一Finalize |
| Deterministic KCC | 5 | 支持 | Fixed capsule query返回稳定Grounded与方向碰撞，调用Fixed唯一Finalize |
| DotRecast Navigation Surface | 4 | 不支持 | Composition在Runtime创建前拒绝；非零Y若越过组合边界则Solver明确失败 |

DotRecast不丢弃Y、不假Grounded、不关闭Body Motion、不隐藏调用Unity Physics或Fixed KCC。Corin在DotRecast获得正式空中World backend前不能组成Active Session，这是批准设计的明确业务代价。

## 5. Presentation、Diagnostics与Agent

- Presentation继续只消费actual Position、Velocity与Grounded；没有`VerticalVelocity`反写VisualRoot或Gameplay路径。
- Structured Trace按Prepare、Solve、Finalize记录Profile/source、gameplay Y、previous/candidate/committed VerticalVelocity、gravity delta、requested/applied Y、Grounded与Collision。
- Runtime Source Map将Body Motion独立映射到Profile asset，不伪装成Graph或Timeline节点。
- Agent schema v13 Snapshot只读输出Profile identity、content revision、两个参数、semantic version、required capability与Compiler状态。
- Agent Patch catalog、lowerer、handler与MCP bridge不提供Body Motion Profile mutation。

## 6. 旧路径清理口径

- 不存在Gravity Node、Gravity MotionContribution、Blackboard gravity或Network Model gravity开关。
- Motion accumulator不再直接构造最终World request。
- Unity与Fixed Solver没有私有gravity常量或第二套积分公式。
- `VerticalVelocity`不从actual `Velocity.Y`、Grounded、坡面、Animation root motion或Presentation推导。
- 旧Program、WorldState、Snapshot、Prediction、Baseline与Rollback reader不保留兼容分支。
- Timeline纯动画Preview不创建Body Motion；完整Gameplay Preview只经过正式Session和能力校验后的Solver。
