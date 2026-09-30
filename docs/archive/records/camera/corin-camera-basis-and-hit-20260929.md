# 可琳基础镜头消费者与命中链补缺（2026-09-29）

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

## 本轮结果

已补齐并发布 Branch_02、Rush 的 A 类命中震动资源。相机 Profile 的 Shake 数量从 13 增至 15，正式投影构建通过。真实命中触发尚未实现，不把资源存在视为反馈链已完成。

用户已明确授权本轮一并补正式攻击碰撞与命中结果生产链。继续沿现有 Simulation Step、GameplayFact、PresentationCommand 和相机请求执行，不能将命中震动改成动作时间点无条件触发。

## 1. 新增资源的原始参数

来源：`D:/ZZZ_Dump/output/corin_replication/replication-guide/data/variants/shake-0.json`，以及 Branch_02 / Rush 动作页中的 `ConfigEntityAttackCameraShake`。

| 项目 | Branch_02 A_01 | Rush A_01 |
| --- | --- | --- |
| AngleVertical | 180 | 90 |
| RadiusLength | 0.02500000037252903 | 同左 |
| ShakeTotalTime | 0.6000000238418579 | 同左 |
| Frequency | 12 | 同左 |
| NoiseAngle / NoiseRatio | 10 / 0 | 同左 |
| DissipationMode | 5 | 同左 |
| CustomCurveKey | Camera_ShakeSpatial_Curve_01 | 同左 |
| CurveKey | Camera_ShakeDecay_Curve_02 | 同左 |
| IngoreTimeScale | true | 同左 |
| PlayStackingType | 原始 0，映射项目 Replace=1 | 同左 |
| ShakeOnNotHit | false，属于攻击反馈事件 | 同左 |

`CorinCameraResourcesAuthoring.Publish` 经正式 Configure API 保存资产并登记 Profile。新资源分别比较 24 项标量／字符串字段，数值按 float32 比较，另核对时间曲线引用及两个空淡入／淡出曲线引用，全部一致。ShakeCenterAttachPoint 原始为 null；本次没有将项目 CameraSpace 枚举视为同名原始字段来计入一致项。证据：[hit-shake-assets.json](../../../diagnostics/camera/camera-basis-runtime-20260929/hit-shake-assets.json)。

## 2. 当前命中链的实际缺口

以下从当前实现确认，不能用 Effect 应用成功替代命中确认：

1. `GameplayAttackCollisionComponentDefinition` 已保存 Box / BoxContinuous / FanWithHeight、形状尺寸、跟随、命中间隔及命中次数限制。
2. `CharacterGameplayEffectRuntimeBinding` 将它们序列化进正式运行绑定；Fixed / Float32 的 `GameplayEffectRuntimeCatalog` 解析为 `PortableAttackCollisionComponent` 与 `PortableAttackPropertyComponent`。
3. 当前这两个运行组件的引用集中在类型声明、解析与数据校验，没有执行攻击几何查询及命中计数的消费者。
4. `FixedGameplayEffectOperationRuntime.Apply` / Float32 对应入口将 SourceActorId、TargetActorId 都设为当前 actor，GameplayResultId 为 0。这是给自身应用攻击配置的入口，不能据此声称打中了目标。
5. 现有 Cue 分支消费 `PortableCueRuntimeChange`，把一般 Effect 生命周期投影为 Cue；当前没有 AttackHit 结果可供它判断。
6. 正式 Step 已有 `CharacterEvaluationResults → WorldSolveBatchResult → AbilityFinalize`，Finalize 生成带 Actor、Tick、BodySample、GameplayFacts、PresentationCommands 的已提交结果。命中扩展应接入该正式事务与输出链，不能从相机直接查询 Physics 或修改其他角色状态。

### 已授权实施的责任分配

- 攻击窗口与形状仍由正式技能／GameplayEffect 作者数据提供；增加真实命中消费者，消费明确的攻击实例身份、源 Actor、目标与时间。
- 命中计数和间隔属于攻击实例运行状态；多个目标分别记 UnitMaxHitCount，整个攻击使用 AliveMaxHitCount。结束／中断回收该实例，快照与提交保持同一状态源。
- 目标位置来自正式世界／角色状态；不能用渲染帧插值姿态代替逻辑世界状态，也不能把地面场景碰撞体都当成受击者。
- 命中结果携带源／目标、攻击实例、Tick 和结果身份，作为 GameplayFact 进入提交；相机只消费对应表现请求。
- `ShakeOnNotHit=false` 时仅真实命中触发；命中强度按已解出的 `ShakeStrength` 实体变量语义接入。这里还需确认变量生产位置与当前属性入口，不能固定成 1 掩盖缺口。

以上是本轮依据现有调用链确定的实施边界，不是已完成实现。尚未新增战斗数值结算、受击者装配或命中 Pass。

## 3. 基础轨道消费者新证据

二进制 SHA256 与前轮一致：`4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`。元数据与字节状态来自 829。证据文件及哈希见 [manifest.json](../../../diagnostics/camera/camera-basis-runtime-20260929/manifest.json)。

### 轨道建模与求值

- `CameraModelSphereData.GetOverrideTrack`，RVA `0x1279EE80`：启用 OverrideElevationTrack 时，把 Orbits、TopOrbit、TopCurvature 与 ScreenYTrack 转为 CameraTrackSetting。ScreenY 数组保存 Top、Middle、Bottom。
- `KGBMAKEKHAP.FLBGLBGHEOM`，RVA `0x109EBE40`：按 Bottom / Middle / Top 顺序，把三个轨道点放到参数 0 / 0.5 / 1；ScreenY 同样按这个参数顺序另建一条标量曲线。
- 轨道曲线构造 `0x10A1AC60` 调用控制点生成 `0x10A1B620`，后者将插值模式置为 raw 3，并调用平滑控制点求解；因此原消费者不是当前项目 SampleTrack 的相邻点线性插值。控制点具体公式还须继续核对，不能仅凭其形态等同于本地 Cinemachine SplineHelpers。
- `KGBMAKEKHAP.GMLFFJIBDBK`，RVA `0x109EBA20`：第一参数先 Clamp01，然后采 Vector2 轨道曲线；结果转换为 `(0, height, -radius)`。第二参数大于 0 时进入 TopOrbit 的额外混合分支；不能把 TopOrbit 当成普通三点数组第四项。
- `KGBMAKEKHAP.GACMGHADADI`，RVA `0x109EC430`：以 Clamp01 后的同类比例采 ScreenY 标量曲线，结果为屏幕偏移向量的 Y 分量。
- 上述轨道采样／屏幕采样／轨道构造的 IFix 分支开关在 829 中为 0。记录同时包含类型初始化字节，不能把所有非零字节都称为 IFix 开关；详见 [branch-flags.json](../../../diagnostics/camera/camera-basis-runtime-20260929/branch-flags.json)。

### 与当前项目的差异

当前 `CharacterCameraFramePlanner.BuildTargetPlan` 用固定 `byTrack.ElevationRatio` 采轨道及 ScreenOffset，再单独加 `m_PitchOffset`；这会保留轨道中点距离，鼠标俯仰不能沿原曲线改变半径及 ScreenY。当前作者资产的比例为 0.5，整理的 Default_Normal 参数 ELEVATION_ANGLE 为 0.60000002。仍需核实当前原作配置选择和输入比例生产者，不能直接改一个默认数值就宣称还原完整轨道。

### 本轮尚未完成

- 平滑控制点与 TopOrbit 分支的完整公式及输入比例生产链。
- `EJKKEBPOLME` 的 Delay 镜头模式选择、各方向跟随／构图阻尼。`DELAYDATAS` 按 CameraDelayMoveMode 查表，不是 FOV 分档；各条目虽有 mFOV，不能据此直接覆盖基础 FOV。已保存更新与应用函数证据，尚未得出可实施的完整公式。
- 日常跟拍、输入响应、额外震动时间倍率、Base 保持计数与静默的完整业务接入。

## 检查与交付范围

Unity 项目路径为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`，目标实例 `e852139597e42532`。新增资源作者源码编译完成，Console 错误数为 0，`CharacterCameraProjectionBuilder.Build` 与 `RequireValid` 成功，Profile 中 15 项 Shake。

本轮未运行 Play 或 replay，未新增测试，未修改 IK。基础轨道只新增取证与文档，尚未修改其求值代码；命中反馈仅完成资源补齐，真实触发链仍在实现范围内。
