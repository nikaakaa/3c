# Design: Pose Transition混合作者链

## Context

当前数据与执行链是：

```text
Pose Transition
  BlendLogic
  Duration
  BlendCurveId = "linear"
  BlendProfileId = "uniform"
        |
        +-> Standard Blend: elapsed / duration，忽略curve/profile
        |
        +-> Inertialization: 使用edge duration，改读下游Policy curve/profile
```

它造成三个直接问题：作者看到的字段不是实际执行数据、不同edge无法可靠配置不同曲线、同一Transition存在edge与node policy两个数学owner。

目标链路是：

```text
Transition Edge Authoring
  BlendLogic
  Duration
  BlendMode
  CustomCurveAsset? 仅Custom
  BlendProfileAsset
        |
        v
Projection Build
  canonical curve index
  dense profile index
  exact route rule
        |
        +-> Standard Blend Native Evaluator
        |
        +-> branch-local Inertialization Native Evaluator
```

## Goals

- 作者选中Transition edge后能够完成与UE StateMachine一致的核心配置流程。
- Standard Blend与Inertialization对同一edge使用同一份duration、curve和Blend Profile。
- Custom Curve可以通过可视化关键帧与切线编辑器编辑。
- Built-in Blend Mode、Custom Curve Asset、Blend Profile、Document和Runtime之间没有字符串猜测。
- UI修改与Document apply进入同一种typed Presentation Mutation。
- BlendStack继续只处理显式连接到自身的selection history，不成为StateMachine Blend Logic。
- 所有重操作保持显式触发。

## Non-Goals

- 不实现UE Custom Transition Graph；本change中的`Custom`只表示自定义标量混合曲线，不是第三种Blend Logic。
- 不增加StateMachine edge上的BlendStack模式。
- 不编辑AnimationClip内的Distance、Foot Phase、Root Motion或其它Animation Curve。
- 不修改Timeline Curve Editor、MotionCurve或MotionWarp曲线。
- 不新增自动Build、选中资产Build、Asset watcher或Play Mode触发编译。
- 不新增测试；用户负责Unity端到端验收。

## Decision 1: 采用UE作者心智，但保持项目运行边界

Transition Details固定为：

```text
Blend Logic      Standard Blend | Inertialization
Duration         seconds
Blend Mode       Linear | Ease In | Ease Out | Ease In Out | Custom
Custom Curve     CharacterAnimationBlendCurveAsset，仅Custom显示
Blend Profile    CharacterAnimationBlendProfile
```

`Blend Logic`选择算法；`Blend Mode`只选择标量包络。这样不会把UE的Custom Transition Graph误装成一个曲线枚举，也不会把BlendStack塞进StateMachine。

### Tradeoff

- 采用edge配置：最符合Locomotion作者判断，一眼能确认一条边如何切换；需要扩展StateMachine compiled descriptor和Native Standard Blend。
- 继续把时间数学放在下游Policy：能少改Transition数据，但作者无法判断edge真实行为，同一Policy也会让多个edge被迫共享设置，不采用。
- 把Locomotion变成Timeline/Montage：能复用有限动作曲线，但会让持续运动状态依赖Gameplay动作生命周期，不采用。

State Details固定增加：

```text
Always Reset on Entry
```

它是State内部全部Player的唯一进入生命周期配置。Transition不得按source-target pair保存reset，Sequence、Blend Space或其它Player也不得再保存独立的作者Reset开关。

### Tradeoff

- State拥有进入重置：与UE作者心智一致，同一个State从任意入口都具有稳定生命周期，作者不需要排查edge与Player谁覆盖谁；失去按edge恢复陈旧播放时间的自由度。
- Transition拥有Target Reset：同一目标可以按不同来源配置，但会让目标State生命周期随edge变化，并与Player relevancy reset产生双主，不采用。
- Player拥有Reset On Entry：实现直接，但一个State包含多个Player时会出现不同进入语义，且State无法表达整体生命周期，不采用。

## Decision 2: Built-in模式与Custom Curve资产共享一个canonical编译结果

Built-in模式不保存曲线key。Compiler通过稳定preset table生成canonical曲线：

- `Linear`：恒定斜率。
- `Ease In`：起步慢、结束快。
- `Ease Out`：起步快、结束慢。
- `Ease In Out`：首尾速度为0。
- `Custom`：读取强类型Curve资产。

所有模式最终都生成`AnimationBlendCurvePayload`，后续catalog、hash、Projection和Runtime不区分来源。

Custom Curve资产拥有稳定`CurveId`、revision与唯一曲线正文。Editor使用CurveField编辑；Compiler只接受能无损降低到现有非加权Hermite key的曲线。端点、单调性、有限值和值域任何一项不合法都必须Build失败并定位Curve资产和key。

Slot、BlendStack、直接Player Inertialization Policy同步改用该模型，项目不同时保留inline `CharacterAnimationBlendCurve`与Curve Asset两种作者格式。编译后的`AnimationBlendCurvePayload`继续作为唯一运行数学格式。

### Tradeoff

- 独立Curve资产：与UE Curve Float心智一致，可复用、可单独预览，Transition只保存强类型引用；Document需要通过Asset Catalog解析引用。
- edge内联Unity `AnimationCurve`：少一个资产，但复用、revision和Document diff都更差，而且会在Transition JSON中嵌入大量key，不采用。
- 继续使用手写Hermite key数组：最接近现有runtime结构，但没有成熟编辑体验，不采用。

## Decision 3: Blend Profile必须是强类型正式资产

Transition不再保存任意`BlendProfileId`字符串，而是直接引用`CharacterAnimationBlendProfile`。Profile仍绑定精确RigId/revision，并显式覆盖全部Physical与Virtual Pose Bone。Uniform也是一个正式Profile资产，不通过null或缺失字段隐式表示。

共享Details的AssetReference必须根据Capability picker kind限制对象类型：Custom Curve只能选择`CharacterAnimationBlendCurveAsset`，Blend Profile只能选择`CharacterAnimationBlendProfile`。错误类型在Mutation前拒绝。

### Tradeoff

- 强类型资产引用：作者能打开真实owner，Compiler能验证Rig，无法输入不存在的字符串；需要Document用稳定资产identity投影引用。
- 保留identity文本：JSON较短，但人工UI无法确认目标，拼写合法也不代表资源存在，不采用。

## Decision 4: Standard Blend在Native阶段执行curve与per-bone profile

PoseStateMachine managed runtime继续唯一拥有transition clock、source/target relevance与完成时机。Native control不再只传一个统一`TargetWeight`，而是传入transition elapsed、base duration、curve index与profile index。

Native evaluator对每个Pose Bone计算：

```text
boneDuration = baseDuration * denseProfile[bone]
normalized = saturate(elapsed / boneDuration)
targetWeight = canonicalCurve(normalized)
```

Physical和Virtual Bone都使用同一个dense profile。Pose Parameter使用全局base duration的canonical envelope；左右Foot Feature分别使用对应foot Bone envelope。Standard transition只在全部需要的bone envelope完成后释放source；Duration为0时当帧Hard Cut。

这保持PoseStateMachine拥有transition，而不是让Animancer backend决定fade。

## Decision 5: Inertialization执行节点不覆盖触发owner的时间数学

Inertialization仍是显式branch-local单Pose节点：StateMachine只发出typed transition请求，节点拥有history、velocity residual、accumulator、rebase与完成。

时间数学按事件来源唯一归属：

```text
PoseStateMachine Transition -> edge提供duration/curve/profile
AnimationSlot exact route   -> Slot Policy提供duration/curve/profile
Direct Player discontinuity -> Inertialization exact Policy提供duration/curve/profile
```

Inertialization节点的response设置只保存Pose Parameter filter与残差处理设置，不得覆盖前两类上游owner。直接Player没有上游transition owner时，节点exact policy继续提供完整时间数学。Compiler必须根据直接上游类型要求恰好一个owner；同时存在两个owner或没有owner都失败，不做优先级和fallback。

### Tradeoff

- 按事件来源唯一owner：每种切换在触发位置配置，执行节点只做连续化；需要拆分当前Policy中混在一起的时间数学与response字段。
- 始终由Inertialization节点Policy覆盖：多个edge无法独立调参，且和UE Transition体验相反，不采用。
- 把惯性化算法直接塞进StateMachine：会破坏显式局部节点和其它owner复用，不采用。

## Decision 6: Document只保存业务配置，不保存Unity对象或compiled index

Pose Transition JSON形状改为：

```json
{
  "blendLogic": "Inertialization",
  "durationSeconds": 0.12,
  "blendMode": "EaseOut",
  "blendProfileAssetId": "corin.locomotion.lower-body-fast"
}
```

Custom模式额外包含：

```json
{
  "blendMode": "Custom",
  "customBlendCurveAssetId": "animation.blend.fast-response"
}
```

非Custom模式禁止`customBlendCurveAssetId`。旧`blendCurveId`与`blendProfileId`字段严格拒绝。Curve/Profile资源的稳定identity和类型进入只读Asset Catalog；Document可修改引用，但不通过Transition文件复制资产正文。

人工UI与Document Reconciler都提交`SetPoseTransitionFieldMutation`的强类型值。Mutation完成后更新StateMachine content revision、Undo、dirty与stale；不Build。

## Decision 7: State唯一拥有进入重置

`CharacterPoseStateDefinition`保存必填`AlwaysResetOnEntry`。State第一次成为Transition target时，StateMachine在提交target relevance前执行一次State级进入命令：开启时重置该State可达的全部source provider，关闭时保留它们的既有播放状态。StateMachine整体Reset重新进入Entry State时使用同一规则。

Sequence Player作者payload与compiled descriptor删除`ResetOnEntry`。Player的`SetRelevant`只负责source generation、continuity与relevance，不再暗中改写clock；初始clock仍由Player初始化，后续重置只由StateMachine显式调用provider `ResetForStateEntry`。

### Tradeoff

- State级唯一owner：一个State内部所有Player保持一致，Transition和Player都不再覆盖；需要扩展State authoring、Document、descriptor和runtime。
- 自动按“当前是否相关”判断：界面更少，但相同行为在中断时依赖瞬时relevance，作者无法从资产直接确认，不采用。

## Decision 8: Source Sync从source binding自动编译

Transition不再保存`SourceSyncMode`。Sequence、Blend Space与对应Pose Source Binding继续唯一保存Marker Group、canonical group identity、Finite/Cyclic topology、Sync Role与ordered Marker。Projection检查source/target State中唯一可同步provider：

- 两侧都没有共同canonical Marker Group时，编译None计划。
- 两侧各有唯一可同步provider且group identity相同时，编译Marker Group Source Sync Plan。
- 同一State存在多个候选可同步provider时，Build失败并要求消除歧义。
- 角色冲突或共同group内marker topology不完整时，Build失败，不关闭同步继续运行。

这对应UE由Animation Node/Source加入Sync Group、共同Marker自动生效的作者模型。Transition只决定如何混合，不重复选择source已经声明的同步关系。

### Tradeoff

- source binding自动同步：组成员身份与Marker只配置一次，所有Transition得到一致结果；作者不能在某一条edge上临时关闭合法共同group。
- edge显式Source Sync：单edge控制更强，但重复source关系并允许同一组在不同edge产生矛盾，不采用。

## Migration

1. 安装新Curve Asset、Blend Mode、typed transition与compiled payload合同。
2. 更新Capability、Details、Mutation、Document codec/exporter/reconciler/validator，删除字符串字段。
3. 更新Projection catalog与PoseStateMachine/Native runtime，使新数据完整可执行。
4. 将BlendStack、AnimationSlot和直接Player Inertialization Policy的inline曲线迁移为同一Mode/Curve Asset合同，删除旧序列化字段和reader。
5. 将State进入生命周期迁移为唯一`Always Reset on Entry`，删除Transition `Target Reset`与Player `Reset On Entry`。
6. 删除Transition `Source Sync`作者字段，让Projection从Pose Source Binding的共同Sync Group与Marker编译Source Sync Plan。
7. 通过唯一Document v3事务迁移Corin State与Transition：根据旧版本真实runtime结果明确选择Mode、Profile和State reset，不按失效edge字段猜测。
8. 显式Build Corin Presentation Projection与Native Pose Program；重新checkout并确认Document Clean。
9. 删除旧identity picker kind、旧schema字段、旧Policy时间覆盖和失实文档。

迁移不保留旧字段reader、兼容alias、默认曲线、默认Profile或运行时fallback。

## Validation

实现完成必须能从代码链证明：

- Pose Transition资产中不存在`m_BlendCurveId`和`m_BlendProfileId`字符串。
- Transition Details不会为Curve/Profile显示TextField，也不会显示GUID。
- 非Custom模式不显示或保存Custom Curve引用。
- Curve Asset编辑器能够修改key与tangent，且编辑动作本身不Build。
- Standard Blend Native evaluator读取compiled curve/profile并执行per-bone权重。
- StateMachine触发的Inertialization读取edge compiled curve/profile，不读取另一个temporal default。
- Direct Player Inertialization仍有明确exact policy owner。
- BlendStack不是StateMachine Blend Logic。
- Document旧字段被严格拒绝，UI与Document使用同一种typed Mutation。
- Corin正式资产、Projection、Native Program与Document使用同一新schema和revision。
- State Details是`Always Reset on Entry`唯一作者入口，Transition与Sequence Player Details均不再显示Reset。
- Transition Document不再包含`targetResetPolicy`或`sourceSyncMode`，State Document必填`alwaysResetOnEntry`。
- Projection只根据source-local Sync Group与Marker生成Source Sync Plan，Transition资产不再保存重复开关。
