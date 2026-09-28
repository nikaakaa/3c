# 两次起步后快速换脚的相位证据

用户指出最新采样中发生两次，并补充发生于起步一段时间之后。

表现采样：`20260928-064516-52209026795540f1b7e1ea2bcd2c4bdf`。
足部采样：`20260928-064516-e0425b1ce8e24d62aba4109a8e9b15be`。

共 914 个表现帧，姿态完成标识 2965–3878，累计帧 delta 约 6.671 秒。下述时间按采样帧 delta 累加，首帧 delta 也计入。

## 两次相同的交接

| 项目 | 第一次 | 第二次 |
| --- | --- | --- |
| Idle → WalkStart 开始 | 2989，约 0.276 秒 | 3442，约 3.691 秒 |
| WalkStart → Walk 开始 | 3146，约 1.382 秒 | 3601，约 4.792 秒 |
| 右踝相对髋部速度峰值 | 3160，约 1.485 秒，9.273 m/s | 3616，约 4.894 秒，9.328 m/s |
| 交接结束 | 3165，约 1.524 秒 | 3622，约 4.935 秒 |

两次峰值均出现在起步约 1.2 秒后、同一条 0.15 秒 Crossfade 约 74% 处。右踝相对髋部的位置已经消除了骨盆整体平移，避免再次把骨盆位移误判为换脚。

所有相同来源、相同选择代次的相邻记录，连续播放时间均没有超过 1 毫秒的增量误差。时间连续不代表两条动画处于同一个迈步姿势。

## 原动画姿态对照

对实际绑定的 `Walk_Start_FootMotionTarget` 与 `Walk_FootMotionTarget` 曲线，按关键帧时间、切线计算局部姿态，再沿骨骼层级求出左右膝踝相对各自髋部的位置。候选匹配扫描只用于定位相位，不能把查到的常量直接写成运行配置。

| WalkStart 时间 | 运行时 Walk 时间 | 与 WalkStart 姿态接近的 Walk 时间 | 左大腿旋转差 | 右大腿旋转差 |
| --- | --- | --- | --- | --- |
| 1.12155 s | 0.01587 s | 0.4883 s | 8.6° | 43.2° |
| 1.224 s | 0.119 s | 0.5900 s | 75.9° | 64.4° |
| 1.216 s | 0.116 s | 0.5833 s | 71.2° | 65.7° |
| 1.250 s | 0.150 s | 循环首尾 0 s | 87.6° | 57.1° |

旋转差是该行 WalkStart 与实际混入的 Walk 的骨骼局部旋转夹角。WalkStart 结束姿态接近 Walk 循环首尾；实际 Crossfade 从 Walk 片头开始推进，到交接结束时 Walk 已推进约 0.15 秒。过渡中混合的是不对应的迈步姿势，会形成快速拉腿。这与用户描述及两次重复峰值一致；没有逐帧视频，仍保留画面与具体时间最终对应的边界。

## 配置和代码原因

`CorinAnimationPresentationProfile.asset` 的 `Locomotion.Gait` 只包含 Run、Walk、TurnBack，不包含 WalkStart。WalkStart 绑定的 GUID 为 `7bd5e9c109acf4744b50901ed2675c3c`。

`CharacterPoseNativeDomainResourceSetCompiler.CompileLocomotionPhasePlan` 在 group 为 null 时不发布相位计划；`CharacterPoseNativeClipPlayerHandler.PhasePlayerCount` 在计划为 null 时返回 0。`CharacterPoseNativeStateMachineSource.SynchronizeTransition` 只从存在相位计划且同组的播放器映射目标时间。因此 WalkStart → Walk 的交接没有获得起步动画的脚步相位，目标从片头启动。

应通过正式脚步同步数据修正这条交接，让 Crossfade 中源和目标对应同一步的位置。仅延长混合时间、改 IK 或检查 Transform 是否被覆盖均不处理这个相位缺口。本轮完成诊断，没有修改动画、同步组或运行逻辑，没有执行回放。
