# 可琳相机复刻实施记录

## 范围与状态

2026-09-29，用户已创建实现 goal，并明确不运行 replay。本文记录第一项可独立交付的震动核心修正；整个相机 goal 尚未完成。

依据：[震动消费者调研](corin-camera-shake-replication-20260928.md)。本次保留既有技能触发和 OnExit 选择性取消入口，没有重建角色动画、IK 或技能资产。

## 本批调用链

1. `CameraShakeAsset.CustomCurveKey` 仍由作者资源提供。`CameraShakeProjectionCompiler` 在正式 Profile 曲线集合中解析它，发布 `CameraShakePayload.CustomCurve`，运行时不再只持有未使用的字符串。缺少对应曲线在编译边界报错。
2. `CameraShakeEffectEvaluator` 对每个存活实例独立求值；位移采用余弦半幅信号，NoiseRatio 控制附加 Perlin 振幅，NoiseAngle 单独控制方向变化，首个采样不加方向噪声。已移除身份散列相位及 1.07/1.13 频率差。
3. 震源初始方向与距离保存在已分配的效果实例中；RealtimeVibration 只决定距离是否逐帧更新。mode 5 按真实距离读取空间曲线，不经过 DissipationDistance=0 的普通距离分支。
4. 基础震动按资源 PlayPriority、当前位移模长选择；附加震动累加。每个实例继续推进时间，未选中的基础实例不会重新起播。相等位移不替换已有候选。
5. 旋转按 Pitch/Yaw/Roll 的 0.25/0.5/0 相位求值，欧拉合成后右乘已有朝向。位置输出与旋转分开应用，避免修改轨道朝向时额外绕 Pivot 移动相机。
6. raw ShakeType 0 使用触发时震源方向，1 使用当前相机方向，2 在当前透视相机的屏幕像素空间转换。`ICameraRigAdapter.PixelHeight` 提供实际输出尺寸，经 `CameraFrameInput` 传给求值器；没有把窗口分辨率写成常量。当前可琳 13 项资源均为 raw 0，raw 2 尚未画面对照。
7. 正时长震动收到明确取消后不再输出，也不人为添加固定 0.016 秒尾巴。负时长效果按资源淡入／淡出曲线处理；取消时保留上一帧包络权重。负时长不强制要求仅供正时长使用的时间曲线。
8. 效果生命周期改为当前采样输出后判断是否已经结束，再推进时间；因此终点采样不会因先加下一帧 delta 而被提前删除。此顺序统一作用于现有相机效果 owner；与旧实现相比，有限时长效果最多保留到下一次终点采样。

所有求值新增状态复用既有池对象，循环内没有新增托管对象、LINQ 或动态容器。未进行运行时 GC 采集，因此这里只声明源码检查结果。

## 时钟与未完成业务

当前 `GameplayTickSystem.PresentFrame` 提供共享 Tick 时钟，OwnerTimeScale、LocalAvatarTimeScale 均为 1，没有原游戏 `DHDAMGEBBHN` 的独立倍率聚合器。当前震动消费 `ScaledDeltaSeconds`，不再把 RealtimeVibration／IngoreTimeScale 解释成无缩放时钟。

这只完成默认时钟入口修正。原游戏额外倍率、该倍率的 IngoreTimeScale 绕过、基础选择保持计数和整体静默尚未接到项目业务输入；不能把这些功能描述成已复刻。后续必须先确认实际输入来源，不能将 OwnerTimeScale 或 HitStop 猜作原始聚合倍率。

## 检查

- 第一轮 Unity 脚本编译完成；目标项目为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`，Edit 模式，Console 错误数为 0。
- 正式 `CharacterCameraProjectionBuilder.Build` 和产物检查成功，schema 为 `character-camera-projection/v3`；13 项 Shake 全部绑定 `Camera_ShakeSpatial_Curve_01`。Normal_01 命中资源的 IgnoreTimeScale=true，其余 12 项为 false。
- 取消与负时长边界整理后的第二轮 Unity 编译及正式投影构建也已完成，Console 错误数仍为 0。
- 未运行 Play、replay，未新增或修改测试代码。没有手感已经一致的结论。

## Goal 后续工作

- 补齐独立倍率／保持与静默的业务证据及必要输入。
- 修复 E 的 Start → Loop／Walk 镜头保持及明确退出，沿正式连续 Clip／节点生命周期实现。
- 接入正式命中结果对应的震动资源与 ShakeStrength，保留 ShakeOnNotHit 规则。
- 核对基础轨道、FOV 和持续推镜的原消费者，完成当前可琳技能的相机交付。

## 持续区间播放合同

当前 E 的 Start／Loop／Walk 各自拥有时间轴，Start 中的 CameraEffectClip 在状态切换时自然退场。核对后，原时间轴只支持 Once／Loop，并行节点也只支持等待全部子流程结束，无法直接表达“镜头持续、随主流程完成而停止”。本批在正式合同中扩展：

- `TimelinePlaybackMode.HoldLastFrame`：时间推进到内容末尾前最后一个 FixedScalar 采样点后保持，Cycle 不递增、播放状态保持 Running，活跃 Clip 直到 owner 停止才退出。表现采样使用提交的定点区间插值，保持时区间两端相同，不经过浮点时长重新定位。
- Fixed／Float32 的技能 Timeline 请求传递完整播放模式，不再压缩成 bool Loop。快照捕获、恢复、编码和解码同步支持该模式。
- `BtsmtlSkillParallelMode.FirstChild`：第一个子流程决定完成结果；它结束后，通过已有 ParentStop／ContinueStop 机制停止其他子流程，等待退出完成后返回主流程结果。退出期间仍保存第一个子流程的成功／失败结果。

目的：让 E 持续区间成为父状态，Start／Loop／Walk 作为内部状态机执行；CameraEffectClip 在伴随时间轴中保持。进入 Explode 或整个技能被取消时，由正式父子生命周期停止伴随时间轴。资产重组、Start 时间轴清理和 BranchAttack 双数值产物已正式生成并发布；画面效果尚未经手测。

运行态快照删除了与 PlaybackMode 重复的 Loop 字段，Fixed／Float32 编码版本由 11 升至 12。旧版本运行态快照不再直接恢复；不保留另一套兼容读取路径。尚未据此判断固定输入录制文件的兼容性。

编译状态：HoldLastFrame、FirstChild、作者源和局部发布入口均完成 Unity 编译及重载，Console 错误数为 0。未运行 Play、replay 或新增测试。

## E 持续镜头发布结果

- 外层 FSM 为 Hold → Explode → End；Hold 内层保留 Start → Loop / Walk 的输入与窗口条件。
- Hold 主体使用 FirstChild 并行：第一支执行内层 FSM；第二支播放私有 `Branch Hold Camera`，模式 HoldLastFrame。
- 私有时间轴引用 dump 的 Branch_02 Zoom_01 / Stretch_01；区间长 0.25 秒，末帧保持，资源自身的 Hold=-1 与退出时间仍由原有相机效果求值器消费。Start 原来的两条重复镜头 Clip 已删除；其动画、位移、Release 窗口和震动触发保留。
- Loop / Walk 切换不结束父状态；释放或中止通过 ParentStop 停止镜头时间轴。Walk 的释放条件具有独立 owned graph，不复用 Loop 条件的 placement。
- `GameplayAbilityExecutionDataAssetPublisher.PublishSelected` 原来只更新执行二进制，遗漏新私有 Timeline 的运行目录登记；现在与全量发布共用 MergeTimelines，保存正式 Definition 的目录。
- `btsmtl.generate_assets` 分别保存 BranchAttack Ability 与 BranchStart Timeline；`PublishSelected(..., "BranchAttack")` 发布 Fixed / Float32 成功。
- 回读 Definition 的 Timeline 目录为 24 项；其中 Branch Hold Camera 为 0.25 秒、2 个 Clip、dirty=false。

首次生成因作者创建顺序错误而失败，工具正式回滚：新父主体图尚未挂接时创建了嵌套 FSM，原生 FSM 事务对整个资产文件执行未引用清理，删除了尚未进入闭包的对象。现将状态主体、条件图在创建后立即绑定正式 owner，再继续创建其他对象，重新生成成功；未放宽清理或闭包约束。

## 命中震动资源与后续消费者

已通过正式资源作者入口补齐 Branch_02、Rush 的 A_01 命中震动，Profile 现有 15 项 Shake，投影构建通过。两项资源各 24 个标量／字符串字段及曲线引用已与 dump 对齐。真实命中结果与触发链尚未完成；用户已授权本轮一并补正式命中链。

新增基础轨道消费者证据、当前命中链缺口及授权范围见 [基础镜头与命中链补缺](corin-camera-basis-and-hit-20260929.md)。该文档明确区分已发布资源、已取得证据和未实施的链路，不把资料齐全等同于运行效果已经一致。
