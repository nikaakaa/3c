# MotionWarp 轨迹解算实现盘点

## 基线资产

- Corin Definition：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`
- 迁移前 Agent Snapshot：`agent-character-controller-synthesis.v14`
- 迁移前 source revision：`5bec9eea9e295c028c177f191528809381905aa57446163149637411b2ce8f69`
- 可达 Graph：163
- 可达 MotionWarp：5，分别属于 Attack1 到 Attack5。
- 五条旧配置均为`MatchTargetPlanarPosition + FaceTarget`，target-local offset为`(0, 1.25)`，PositionWeight与YawWeight均为1，最大平面修正为1.5，最大yaw修正为90度。
- Attack1到Attack5的旧Warp窗口依次为`0..49`、`0..48`、`0..81`、`0..89`、`0..125`。
- 每条Warp均绑定对应主攻击MotionCurve，未绑定后摇MotionCurve。
- 五条主攻击MotionCurve的Gameplay Ease均为`0/0`，WeightCurve均为`(0,1,0,0) -> (1,1,0,0)`，满足单位Warp source合同。
- 五条旧Position/Yaw Progress均为`(0,0,1,1) -> (1,1,1,1)`，没有逐段作者差异。

## 五段攻击时间事实

| 段 | 主动画 / 主MotionCurve | 后摇动画 / 后摇MotionCurve | Hit TreeClip | Cue | ComboAccept | RecoveryEarly / Late |
|---|---|---|---|---|---|---|
| Attack1 | `0..49` / `142218e6-6644-479b-b721-19d91be03a15` | `49..168` / `7df37ded-43d4-4c6c-83a4-0227e64ccb8a` | `18..45` | `18..19`、`24..25` | `50..93` | `43..162` / `73..162` |
| Attack2 | `0..48` / `7e17c38a-c154-4d62-811f-ca2edc377b21` | `48..173` / `a841768b-147c-445c-a951-1480f50ffae8` | `18..45` | `18..19`、`24..25` | `49..92` | `42..167` / `72..167` |
| Attack3 | `0..81` / `421c43dd-230b-4ac9-8214-ff047b7f62e4` | `81..206` / `f59c86ff-ff00-41e7-8e7a-448c0082293e` | `18..45` | `18..19`、`24..25` | `82..125` | `75..200` / `105..200` |
| Attack4 | `0..89` / `45192fb7-d872-48eb-8ab0-434d62b1ca30` | `89..282` / `a5153916-106f-48f0-941a-187bd493d2d4` | `18..45` | `18..19`、`24..25` | `90..133` | `83..276` / `113..276` |
| Attack5 | `0..125` / `5b7d7ca8-8497-4c3d-97ee-471bf105275a` | `125..212` / `bd4efe09-442f-45b6-8ccf-a5b4f533aab1` | `18..45` | `18..19`、`24..25` | 无 | `119..206` / `149..206` |

动画资产依次为`Corin_Pipeline_Attack1..5_Inplace.anim`，后摇资产依次为`Corin_Pipeline_Attack1..5_End_Inplace.anim`，都位于`Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/`。以上迁移前事实来自正式Agent Full Snapshot；正式v15迁移没有沿用旧`0..主动画结束`窗口，而是按主MotionCurve、Hit事实与动作落脚节奏逐段配置。

## Corin v15正式迁移

- Agent schema：`agent-character-controller-synthesis.v15`
- Domain：`CharacterController`
- Root identity：`c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- Patch输入source revision：`43530a4e0b5f25ab9cff39665ce9027810721ae19b351beb0c769b905849bbde`
- Apply后source revision：`2c4701c71623bb196be5f6eda9e639f393f6e217e497c8148a744cc4e067b41c`
- Apply后semantic hash：`4d2cebf9b465f49c06cf32adf1b630e2818b2a2c1ce3213fca5d3670e2bba454`

同一份15-operation Patch依次通过`dry_run_patch`、`apply`、`export_snapshot`和`validate`。Apply调用因正式产品重建和Unity domain reload超过MCP等待时间，但没有重试；随后的Snapshot完整证明15项事务均已保存，避免重复提交。

五段攻击都使用`SkewToTarget + ApproachDirection + FaceTarget + ProgressCurve + ApplyClamped`，目标偏移为`(0, 1.25)`，最大位置修正为`1.5`，最大yaw修正为`90`度，yaw offset为`0`。窗口按各自主MotionCurve在命中阶段前后的累计位移活动独立确定，后摇MotionCurve不参与Warp。

| 段 | Warp Clip / Source Motion | Warp窗口 | Position Progress中间值 | Yaw Progress中间值 |
|---|---|---|---|---|
| Attack1 | `eb07c8dd-3527-4470-8bc4-c44f6665d937` / `142218e6-6644-479b-b721-19d91be03a15` | `7..32` | `0.327076 / 0.710115` | `0.571906 / 0.842683` |
| Attack2 | `c7ee8732-c30a-4388-8025-e2cdd04e5d3b` / `7e17c38a-c154-4d62-811f-ca2edc377b21` | `5..29` | `0.407064 / 0.827479` | `0.638016 / 0.909659` |
| Attack3 | `2864f821-fd2b-4536-8845-bec7ad6caf06` / `421c43dd-230b-4ac9-8214-ff047b7f62e4` | `6..39` | `0.202649 / 0.305615` | `0.450166 / 0.552825` |
| Attack4 | `04efef4b-5857-4b87-96a6-347df3303c35` / `45192fb7-d872-48eb-8ab0-434d62b1ca30` | `9..42` | `0.300857 / 0.700294` | `0.548504 / 0.836836` |
| Attack5 | `06edc605-eeb1-4887-8114-20917970daeb` / `5b7d7ca8-8497-4c3d-97ee-471bf105275a` | `10..40` | `0.337895 / 0.734712` | `0.581287 / 0.857153` |

两条progress curve均固定包含`(0,0)`与`(1,1)`，表中是`time=0.333333`和`time=0.666667`的逐段值；未复用公共线性模板。Attack1位于共享Timeline资产`Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset`，Attack2到Attack5位于`Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset`的inline Timeline。

## 唯一代码链

```text
MotionWarpClip
  -> CharacterSimulationTimelineEmitterRegistry
  -> TimelineMotionWarp Semantic operation + typed source reference
  -> ProgramMotionModifierCompiler
  -> ProgramMotionModifierDescriptor
  -> Float32/Fixed Program lowering
  -> ProgramExecutionLayout
  -> TimelineControlRuntime active sample
  -> Action channel resolved owner eligibility
  -> Float32MotionWarpTarget / FixedMotionWarpTarget
  -> warped window-intersection delta - raw window-intersection source delta
  -> existing ResolvedMotionChannel correction
  -> ResolvedGameplayMotion
  -> Body Motion request
  -> selected WorldSolver
```

MotionWarp不直接写Transform、Body state或Presentation，不调用WorldSolver，也不进入Animancer。WorldSolver只接收最终Body Motion request；碰撞裁掉的位移不会在后续Tick追补。

## Portable 语义

- Translation Mode：`Disabled`、`ScaleToTarget`、`SkewToTarget`、`LinearToTarget`。
- Target Offset Space：`TargetLocal`、`ApproachDirection`、`ActorStartLocal`、`World`。
- Rotation Mode：`Disabled`、`FaceTarget`、`MatchTargetYaw`。
- Rotation Method：`ProgressCurve`、`ConstantRate`、`ScaleSourceYaw`。
- Limit Policy：`ApplyClamped`、`PreserveSource`。
- 已删除PositionWeight、YawWeight、TargetLocalPlanarOffset和PositionMode语义。
- 未被当前mode消费的progress curve、yaw rate与offset字段不进入Semantic IR、Program或hash。
- Authoring validator和Semantic发布均拒绝ScaleToTarget零平面源终点与ScaleSourceYaw零源yaw；Float32和Fixed runtime保留同一invariant检查。
- MotionWarp source必须使用无Ease且恒为1的Gameplay WeightCurve；动画CrossFade继续由Presentation独立处理。
- Float32和Fixed在逻辑Tick跨越Warp边界时，只替换Tick与窗口交集内的source delta，保留窗口外轨迹。

## 累计轨迹状态

每条MotionWarp拥有16个连续typed state slot：

1. Active
2. Initialized
3. PlaybackGeneration
4. ActionInstance
5. StartBodyPosition
6. StartBodyYaw
7. SourceWindowStartPosition
8. SourceWindowStartYaw
9. ResolvedTargetPosition
10. ResolvedTargetYaw
11. LimitResult
12. PreviousWarpedPosition
13. PreviousWarpedYaw
14. LastPositionProgress
15. LastYawProgress
16. SourceOperation

同Tick raw contribution、窗口交集source delta、resolved owner、current warped pose、modifier correction与final Action channel仍是transient。Snapshot恢复后从保存的warp-start上下文与previous warped cumulative pose继续，不重新捕获目标。

## 版本

| 合同 | 当前版本 |
|---|---:|
| Character Gameplay Operation Set | `/10` |
| Semantic IR artifact/payload | `11 / 11` |
| Float32 Program artifact/format/layout | `14 / 16 / 9` |
| Fixed Program artifact/format/layout | `14 / 16 / 9` |
| Float32 Target ABI | `7` |
| Fixed Target ABI | `6` |
| Float32 Character State file/identity | `8 / character-state/float32/v8` |
| Fixed Character State file/identity | `9 / character-state/fixed-q32.32/v7` |

旧reader、旧state payload和旧Program版本不兼容读取。

## 诊断与检查入口

- `CharacterSemanticIrInspectorWindow`显示source、Action Context、Translation Mode、Offset Space、offset、Position Progress、Rotation Mode/Method、yaw参数、限制与Limit Policy。
- MotionWarp target resolution trace记录窗口首尾、source首尾pose、模式、未限制Target Pose、有效Target Pose及limit判定。
- 每Tick applied trace记录normalized time、source current pose、previous/current warped cumulative pose、raw窗口交集delta、correction和final Action channel。
- 普通.NET `ThirdPersonSimulation.Reader`的`motion-modifiers` section读取同一新descriptor，text与JSON均不再输出旧weight字段。

## 最终产品身份与剩余缺口

Float32正式产品已由Corin v15事务重建：

- Numeric profile：`float32-ieee754`
- Target ABI：`7`
- Program id：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- Program hash：`a4a655ab383c8f29e1e6d1f32af70ed785a77405390947617774c05576d51a9a`
- Layout hash：`7da00c9e24a1066f9d36cf240221864f19d218b588159ded28b480fc1801004a`

`Assets/Configs/Simulation/DeterministicRollback/CorinFixedProgram.asset`仍是迁移前产物，source revision为`5bec9eea554b491cff72bd5ffadcf20f4b78deab04c2d45d44fba6ed7519a544`，Program hash为`a33c6fd33115d577abb9b5c90ebeb2539cdfdfa8dc68f34de341c8a5cd539573`，Layout hash为`088527185886bf51808d50f05f287c80cfcdf63d5ee20d72cc1fd3ae2bc7ebae`，Operation Set仍为`character-gameplay-operations/9`。Fixed正式重建只由Gameplay Lab的`FixedCharacterSimulationProgramBuildService.Build(definitionGuid)`入口拥有；本change不增加临时菜单、反射调用、YAML修改或第二条构建路径。因此Fixed Program重建和Float/Fixed最终identity核对仍保持未完成。

## 静态验证

- `ThirdPersonSimulation.Core.csproj`：0 warning / 0 error。
- `ThirdPersonSimulation.Float32.csproj`：0 warning / 0 error。
- `ThirdPersonSimulation.Fixed.csproj`：0 warning / 0 error。
- `ThirdPersonSimulation.Reader.csproj`：0 warning / 0 error。
- `ThirdPersonClient.Runtime.csproj`：0 error，1个既有`CS0414` warning。
- `ThirdPersonClient.Editor.csproj`：0 warning / 0 error。
- 正式Agent validate：`domain=CharacterController`、`rootIdentity=c7a7c1e3f7e64d81b5a04a90cbeb8d4e`、`compileSuccessCount=1`、`semanticValidCount=1`。
- Unity Console：0 error。
- Agent、Compiler、Runtime与Corin序列化资产均未发现旧`MotionWarpPositionMode`、`TargetLocalPlanarOffset`、MotionWarp `PositionWeight/YawWeight`或`MatchTargetPlanarPosition`路径。
- 未运行Unity batchmode。
