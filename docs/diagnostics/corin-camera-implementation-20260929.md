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
