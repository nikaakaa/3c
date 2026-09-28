# 可琳镜头消费链核查（2026-09-28）

## 范围与证据边界

本轮核对当前源码、可琳作者资产、已整理的 dump 文档和 battle-0 事件。没有修改运行代码或资产，没有启动 Play、回放或新增测试。以下区分能够由代码确定的行为与尚未证明的原作语义；不把静态核查当作画面验收。

原始资料入口：`D:/ZZZ_Dump/output/corin_replication/replication-guide/`，主要读取 `镜头参数.md`、`镜头补缺.md`、`analysis/基础镜头.md`、`analysis/镜头原生参数.md`、`data/variants/battle-0.json` 和 Branch/Rush 动作页。

项目路径前缀为 `3cDemo/Client/3C_Client/Assets/`。

## 纠正前次评估

- 可琳 `CorinCharacterCameraProfile.asset` 的 `m_Shots` 和 `m_OverrideTracks` 均为空。Shot 在求值器中排在 Shake 之后属实，但不能把它列为当前可琳普攻、E、Rush 的已发生覆盖原因。
- 当前 13 个 Shake 资产的 Pitch/Yaw/RollAmplitude 全为 0。自定义的 1.07/1.13 系数没有影响这批资源的旋转输出；实际生效的是位移正弦、初相位和衰减。
- 同一批 Shake 的 DissipationDistance 全为 0，线性距离分支没有执行。空间曲线未消费仍是缺口，但缺少原消费者证据，不能直接说当前应按该曲线减半。
- 179 个 Shot key 的资源值核对不能证明当前可琳攻击镜头已还原；当前链主要使用 Shake、Zoom、Stretch。本轮没有重做该 179 项审计。
- `CameraBasisResolver` 只计算水平移动基向量。基础镜头求值在 `CharacterCameraFramePlanner`、`CharacterCameraSequenceEvaluator`。

## 1. E 持续阶段丢失镜头保持

### 数据与实际执行

- `Corin_Attack_Branch_02_CamZoom_01.asset`：StartTime=0.25、LastTime=-1、EndTime=1.5、绝对 FOV=52。
- `Corin_Attack_Branch_02_CamStretch_01.asset`：StretchTime=0.25、HoldTime=-1、RecoilTime=1.5、RadiusRatio=0.1、CamOffset.y=-0.2。
- 当前 `Timelines/BranchAttack/CorinBranchStartTimeline.asset` 把两者放在 0～1.1 秒的 CameraEffectClip 上；Loop/Walk 时间轴没有对应 CameraEffectClip。
- `CorinCameraTimelineAuthoring.Apply` 把持续效果的终点直接设为所在阶段终点。
- `TimelineToActionCommandBridge.CameraEventState.ResourceTimed` 只对 Shake 为真；`RetireInactive` 在 Zoom/Stretch 离开宿主时间轴时提交撤销。
- `CameraEffectEvaluationMath.ResolveRetiredWeight` 从撤销时权重开始按退出曲线回退。

因此在持续按 E、Start 转 Loop/Walk 时，镜头不再保持，开始按 1.5 秒资源退出时长回到基础镜头。基础 FOV 是 50。实际墙钟时长受表现时钟影响。

dump 的 Branch_02 在 0 帧触发 Zoom/Stretch，资源正文是无限保持；battle-0 的该事件组没有与 Start 结束一一对应的 End 事件。它的跨状态结束/替换规则仍需沿技能退出、Explode 与原消费者核实，不能靠把效果复制到每个 Loop 周期解决，否则会重复启动和叠加。

## 2. 基础轨道的数据没有随输入完整消费

- `CorinCameraDefaultSequence.asset` 有三段轨道，但 `m_ElevationRatio` 固定为 0.5。
- `CharacterCameraFramePlanner.ResolveLook` 累积鼠标 yaw/pitch；`BuildTargetPlan` 用固定 `byTrack.ElevationRatio` 取轨道及画面偏移，再单独加 pitch。
- 对当前三段轨道，始终采中段 Height=0.22499999、Radius=3.75，球面距离约 3.7567。改变俯仰不会沿上/下轨道调整距离和 ScreenY。
- 基础 dump 还包含 TopOrbit/TopCurvature、按 FOV 分类的 Delay 数据、各方向阻尼和移动方向比例等。当前 `CameraWorldBasicHistory` 仅对 pivot、radius、offset 用统一 DefaultSmoothTime=0.15 做 SmoothDamp；旋转与 FOV直接采用目标值。
- 最终 `CinemachineCameraRigAdapter.Apply` 调用 ForceCameraPosition，不能据“使用 Cinemachine”假定缺少的原作轨道/阻尼自动补齐。

以上能证明当前基础镜头仅消费了部分数据。原作如何选择 Delay 表和如何解释各系数，整理文档尚未提供完整运行时公式，不能把这些值直接当成 SmoothDamp 秒数。

## 3. 命中震动缺口不止普攻第一段

- 已交付记录明确 A01 命中确认触发尚未接入。
- dump 的 Branch_02 动作页列出多次 `ConfigEntityAttackCameraShake`，引用 `Corin_Attack_Branch_02_CamShake_A_01`，`ShakeOnNotHit=false`。
- Rush/强化 Rush 的攻击处理同样引用 `Corin_Attack_Rush_CamShake_A_01`，`ShakeOnNotHit=false`。
- 当前可琳 Shake 资源集合为 13 项，其中唯一 A 类为 `Corin_Attack_Normal_01_CamShake_A_01`；Branch/Rush 的上述 A 类资源不在该集合中。

当前接入主要覆盖动作时间点上的 E 类震动。不能把这些事件当成命中反馈已完成，更不能把 dump 的 A 类命中事件改为按帧无条件触发。

## 4. 有效震动公式及重叠仲裁仍需对齐

当前可琳有效位移近似为：

`sin(elapsed * Frequency * 2π + identityPhase) * RadiusLength * decayWeight`

NoiseRatio=0，因此方向噪声在这批配置上也为零。identityPhase 来自 ResourceId、SourceId、EventId 的散列，不是来自 dump 的波形。原作是否使用该波形、频率单位和起始相位，尚无消费者证据。

当前 Shake 全被作者映射成 Replace。`CameraEffectRuntimeStateStore.Select` 对同优先级、权重、代次、动作实例、循环的请求，按 SourceId/EventId 排序，没有按触发先后排序；未选中效果仍随时间推进直到到期。不能从 Replace 这个项目枚举名称推断它实现了“最新震动替换旧震动”。

Rush 的触发帧为 8、22、38、54，单次时长 0.3 秒，按 60 帧/秒换算存在重叠区间。是否选到了不合预期的请求，需要核对这些区间的实际身份与选择结果；本轮没有把这一风险表述为已复现根因。dump 的 PlayStackingType=0 的原消费者语义也尚未确定。

## 已核对但不列为当前根因

- Camera_Default_Curve_01 与 04 均从 0 到 1；当前退出使用 1-curve，没有发现首尾反向的直接证据。
- 正常 FrameLateUpdate 把 OwnerTimeScale 与 LocalAvatarTimeScale 均传为 1；本轮没有发现正常路径在相机端重复乘角色时间倍率的证据。
- Shot、旋转频率系数、未进入的线性距离分支均不能解释当前这批可琳资源的有效输出。

## 后续实现边界

- E 的镜头保持应随技能持续区间归属和明确退出结束；阶段内部切换保持同一效果身份，不重复起动。
- 命中镜头从正式命中结果进入表现请求，保留 ShakeOnNotHit 条件。
- 基础轨道需要输入驱动的仰角轨道位置，以及具有原始语义依据的跟随响应。
- 波形、空间衰减及叠加枚举需消费者证据再改，不能以猜测公式冒充还原。

上述是调查结论及修复边界，本轮未发布任何相机修复。
