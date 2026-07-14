# Design: Root Motion 曲线烘焙和管线提交

## 目标链路

```text
编辑器选择 AnimationClip 和采样 Prefab
-> RootMotionCurveBaker 临时实例化 Prefab
-> Animator 以固定采样率播放目标 clip
-> 累计本地 position.xyz 和 yaw
-> 保存 RootMotionCurveAsset
-> Timeline/Action 显式引用 RootMotionCurveAsset
-> 运行时按 previousTime/currentTime 采样 delta
-> 提交 motion 数据
-> CharacterMotionStage 应用位移
```

## 数据模型

`RootMotionCurveAsset` 是从动画派生出来的正式资产，不是手填 gameplay 配置。

建议字段：

```text
AnimationClip SourceClip
float Duration
float SampleRate
AnimationCurve LocalPositionX
AnimationCurve LocalPositionY
AnimationCurve LocalPositionZ
AnimationCurve LocalYaw
Vector3 TotalLocalPosition
float TotalYaw
```

曲线保存累计值，单位使用 Unity 米和角度。运行时用两次采样的差值获得本帧 delta：

```text
deltaLocalPosition = sample(currentTime).position - sample(previousTime).position
deltaLocalYaw = sample(currentTime).yaw - sample(previousTime).yaw
```

业务取舍：

- 累计曲线比速度曲线更适合 Timeline seek、动作变速、低帧率和预览。
- 速度可以从累计曲线派生，反过来从速度积分回位移会产生漂移。
- yaw 曲线先满足动作游戏根朝向需求；完整 quaternion 后续可以作为扩展模式，不作为第一版默认。

## 编辑器烘焙器

BBB 有两条参考思路：

- `RootMotionExtractor` 使用 `clip.SampleAnimation(...)` 读取 transform 位置和旋转，并写回旧 `MotionClipData`。
- `WarpedMotionExtractor` 临时实例化 prefab，通过 `AnimatorOverrideController` 播放 clip，读取 `Animator.deltaPosition/deltaRotation`。

本变更采用第二种思路作为主采样方式，因为它更接近 Unity root motion 的实际播放结果。正式工具只负责生成 `RootMotionCurveAsset`：

- 需要采样 Prefab 包含 `Animator`。
- 需要 `Animator` 有可用 `RuntimeAnimatorController`，用于 override 到目标 clip。
- 工具实例化隐藏临时对象，不保存场景对象。
- 工具按 `FromClip`、`60fps` 或 `120fps` 采样。
- 工具把世界 delta 转换到采样对象当前局部空间，再累计到本地曲线。
- 工具用 `AssetDatabase.CreateAsset` 或明确覆盖目标资产。

不采用 BBB 旧递归扫描，因为扫描任意配置再写回会绕过 BTSMTL 节点、Timeline 和角色管线，重新制造不可解释的数据源。

## 运行时求值器

`RootMotionCurveEvaluator` 应是纯 runtime 逻辑，不依赖 Unity Editor API。

它负责：

- 校验曲线资产和时间区间。
- 根据 `previousTime` 和 `currentTime` 求本地 delta。
- 支持单次播放的 clamp。
- 后续如需要 loop，必须以显式参数表达，不通过自动猜测。
- 将 local delta 按角色当前朝向转换为 world displacement。
- 输出 motion contribution 或 proposal 所需数据。

它不负责：

- 不读 AnimationClip。
- 不访问 Animator/Animancer。
- 不调用 Transform。
- 不调用 CharacterController。

## 管线集成

当前 `CharacterMotionStage` 是最终移动边界。Root motion 只能进入它上游的数据层。

推荐收口：

```text
TimelinePlaybackScheduler
-> RootMotionCurveEvaluator
-> MotionContribution
-> MotionResolver
-> MotionIntent
-> CharacterMotionStage
```

Root motion 必须经过同一个 strict gameplay output，不能让 Timeline 轨道或表现层直接移动角色。当前方向是由 `TimelinePlaybackScheduler` 采样出 `MotionContribution`，再由 resolver 生成最终 `MotionIntent`，后续输入驱动移动、root motion、击退、外力都走同一个 resolver。

业务取舍：

- 直接写 `MotionIntent` 最小，但容易让多个 motion 来源互相覆盖。
- 增加 `MotionContribution` 稍多代码，但能解释 root motion、输入移动、击退和网络修正的优先级关系。
- 本项目是动作客户端 demo，动作位移必须可调试、可仲裁、可被服务端修正；所以更推荐贡献收集再 resolve。

## Timeline / Action 引用方式

Timeline 或 Action 不应通过命名约定查找 root motion 曲线。它们必须显式引用：

```text
AnimationClip/ActionClip
-> AnimationClip
-> RootMotionCurveAsset
```

这样同一个动画可以有不同烘焙版本，也能清楚知道一个动作有没有 root motion。引用缺失时就是没有 root motion，不自动搜索、不自动生成、不 fallback。

## 清理边界

- `Assets/Ref/BBB` 保留为参考，不作为运行时依赖。
- 不复制 `BBBNexus.MotionClipData`、`WarpedMotionData`、`FootPhase` 到正式目录。
- 不新增 `AnimationPresentationPolicySO`、`BodyClaimPolicySO` 或 locomotion 特化 SO。
- 不让 `TimelinePlayer.ApplyRootMotion` 成为角色管线模式下的移动来源。
- 不通过 Animator `applyRootMotion` 直接移动正式角色 Transform。

## 后续扩展

后续如果业务需要，可以在同一资产上扩展：

- quaternion 或 pitch/roll 曲线。
- 曲线压缩和 key reduction。
- warped motion 点位。
- 基于 Timeline 的 motion scale 曲线。
- 调试预览窗口。

这些都是同一链路的扩展，不应新增第二套配置源。
