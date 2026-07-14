# Design: Timeline Motion Authoring Modes

## 背景

旧链路是：

`Timeline AnimationClip.RootMotionCurve -> TimelinePlaybackScheduler -> MotionContribution.ActionRootMotion -> MotionResolver -> CharacterMotionStage`

这个链路把动画表现片段和玩法位移事实绑在同一个 AnimationClip authoring 对象上。短期好处是动画时间和位移时间天然一致，但代价是换动画可能隐式改变 gameplay motion，网络同步和 debug 也很难解释“同步的是动画还是位移事实”。因此旧链路不再作为正式运行时入口。

`RootMotionCurveAsset` 仍然有价值：它表达从动画派生出的累计 local XYZ / forward distance / yaw 曲线，适合烘焙和复用。但 Timeline 运行时位移必须作为独立 motion fact 写在 `MotionCurveTrack`，而不是挂在动画片段上。

MotionWarp 已经存在，并且在 `CharacterMotionStage` 中作为 Move 前 modifier 执行。它解决的是目标对齐，不应该和 root motion 混成一种东西。

## 方案

### RootMotionCurveEvaluationMode

新增曲线求值模式：

```text
FullLocalDelta
ForwardDistanceYaw
```

`FullLocalDelta` 使用现有累计 local XYZ + yaw。运行时 delta 为：

```text
localDelta = currentLocalPosition - previousLocalPosition
yawDelta = currentYaw - previousYaw
```

`ForwardDistanceYaw` 使用累计前向距离 + yaw。运行时 delta 为：

```text
localDelta = Vector3.forward * (currentDistance - previousDistance)
yawDelta = currentYaw - previousYaw
```

虽然作者会把它理解为“速度曲线模式”，资产持久化建议保存累计距离而不是每帧速度。原因是 runtime 仍然只做两次采样相减，和当前 root motion 求值模型一致，避免每 tick 做积分导致不同帧率或采样点产生差异。

### Baker 模式

Root Motion Baker 增加 Bake Mode：

```text
Full Local Delta
Forward Distance + Yaw
```

完整模式继续写 local X/Y/Z/yaw。

前向模式从采样 delta 计算平面距离并累计到 forward distance。默认只使用 planar magnitude，忽略 lateral sign；yaw 仍由 delta rotation 累计。业务效果是“动画可以告诉角色这一段推进多快，但不让动画横向漂移决定方向”。

如果后续需要倒退攻击或后撤，可以再加明确的 signed forward 规则或反向模式；本 change 不把它隐式塞进普通速度模式。

### Timeline MotionCurveTrack

新增 Timeline 直接位移轨：

```text
MotionCurveTrack
MotionCurveClip
```

Clip 字段表达：

```text
Space: Local / World
Channel: 默认 Action
BlendMode: Additive / WeightedBlend / Override
Priority
WeightCurve
LocalOrWorldPositionX/Y/Z
Yaw
ConsumeLowerChannels
```

该轨道采样后直接输出 `MotionContribution`，仍由 `MotionResolver` 仲裁和 `CharacterMotionStage` 应用。它不直接移动 Transform，不绑定 AnimationClip，不依赖 root motion baker。

如果位移来源来自动画烘焙，作者也必须把烘焙结果明确转成或配置进 `MotionCurveTrack`。运行时不再从 `AnimationTrack` 或 `AnimationClip` 上读取 `RootMotionCurveAsset`。

### MotionWarp 边界

MotionWarp 保持现状：Timeline 只提交 window，`CharacterMotionStage` 在 Move 前读取 target context 并修正 raw intent。MotionWarp 不变成 MotionContribution，也不直接输出固定 delta。

## 取舍

### 完整 root motion

业务收益：特殊动作表现忠于动画资源，闪避、翻滚、斜向攻击、绕圈斩能保留设计轨迹。

代价：依赖动画 root 质量、采样对象朝向和导入设置；普通动作容易吃到横向漂移。运行时不再把它挂到 AnimationClip 上直接生效，必须显式进入 MotionCurveTrack，换动画不会偷偷改 gameplay 位移。

### 前向距离 + yaw

业务收益：普通攻击、Walk/Run、前踏更稳定，角色方向由 gameplay 当前朝向决定，减少横飞和侧漂。

代价：动画中真实侧移会被丢弃；如果动作确实需要弧线位移，就应该选完整 root motion 或手画曲线。

### 手画 MotionCurveTrack

业务收益：不依赖动画 root，距离、时间和旋转可直接调，适合攻击手感快速迭代。

代价：脚底和动画不天然匹配，需要作者调动画、曲线和窗口时间。

### MotionWarp

业务收益：目标对齐和吸附由运行时上下文决定，适合锁定目标、处决、跳斩落点。

代价：必须有正式 target key 数据；目标缺失时不能使用场景搜索或 fallback。

## 不采用的方案

### 直接开启 Animator root motion

会绕过 `MotionContribution`、网络预测、MotionDebug 和纠偏阶段，不符合现有 motion spec。

### 复制 BBB 数据结构

BBB 可作为算法参考，但正式运行时不能依赖 `MotionClipData`、`WarpedMotionData` 或 PlayerSO。复制旧数据源会制造分裂路径。

### 按命名自动找曲线

这会让 Timeline clip 的位移输入变成隐式配置，调试和网络回放无法稳定追踪。曲线必须显式配置。

### 把 Loop Policy 一起做

Loop 是 Timeline request 生命周期问题，不是 motion authoring 问题。混在一起会扩大实现范围，也容易把状态机循环、Timeline 播放完成语义和 motion 曲线求值绑死。

## 风险和缺口

- 前向模式如何处理后退动作需要后续明确；本 change 先只支持正向推进。
- MotionCurveTrack 的曲线编辑 UI 依赖现有 Timeline clip inspector 能正常展示 AnimationCurve。
- 如果 MotionWarp target key 当前没有正式写入黑板或 context，这个 change 不会绕过系统补目标。
- 旧已烘焙曲线需要明确迁移为 `FullLocalDelta`，不能静默改成前向模式。
