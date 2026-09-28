# 可琳动作转向与 Rush 分支对齐

## 来源与时间口径

来源为 `D:/ZZZ_Dump/output/corin_replication/replication-guide/data/zones.json`、`transitions.json`，以及该目录的技能与输入、动画同步整理。原始 AnimatorZone 文件 SHA256：`3acf6cc38cb004edb1d11b8bb906645e40ef3b8189d25b87819e6cce4c5b7f15`。

本次采用 dump 的帧字段，按 60 帧/秒映射 Timeline。原游戏 `ConfigMisc.IsAnimatorZoneUseFrame` 的活动实例值仍未取得，因此不能把这项选择说成已验证原游戏运行配置。归一化时间字段仍可在原始来源核对。

原生锁敌更新 `0x1484F370` 的 `steerType=1` 分支：`0x14850090` 将 deltaTime 乘曲线与倍率，再乘实体时间因子；`0x14850A6C` 调用 `0x13B87040`，经 `0x13B83110` 调用 IAT `0x05408E60`。该地址与 `UnityEngine.Vector3.Slerp_Injected` 的 `0x1E4923F0` 叶函数一致。局部反汇编证据保留在 `tmp/corin-steering-20260928/`。

## 执行链

输入适配器提供世界平面的 `MoveAxis` 和动作准入目标快照。角色控制请求为 Attack、RushAttack、BranchAttack 传递 `ActionTarget`，后两者准入改为 OptionalSnapshot。

Timeline MotionWarp 的 TargetResponse 模式分别采样目标响应、方向输入响应；有目标且目标响应大于零时使用目标方向，否则按该阶段允许的输入响应读取方向。Fixed、Float32 计算 `Clamp01(response × 动作时间增量)`，按平面最短角度转向，同时旋转当前帧根位移方向，取消源根旋转。没有旋转响应时保留当前朝向。

原来的位置 Warping、累计进度和恒定角速度模式保持独立语义；新模式不做位置拉伸。曲线进入正式字段、作者合同、采样请求、双数值执行及内容哈希。运行高频链没有新增显式 new 引用对象；未测量 GC，不能宣称实测零分配。

## 本次配置

- 普攻 1–5：锁敌响应 60，帧窗口分别 0–10、0–9、0–32、0–18、0–25；方向输入使用各自 RotSpeedZone 原曲线与倍率。
- 普通 Rush：锁敌曲线 60→6；方向输入 999→1.5。
- 强化 Rush 起手：独立锁敌曲线和输入曲线；循环输入响应恒为 1。
- 当前 E 为 Branch02：起手锁敌曲线乘 12；Walk 输入响应 0.5；Loop、Explode、End 禁止旋转。
- Rush 的爆发和收尾阶段禁止旋转。
- 保留 dump 曲线关键帧、切线和权重；帧时间按源片段时长转换。位于片段结束之后的控制点仍保留，以保持片段内的曲线形状。
- 普通 Rush 三个阶段此前没有 MotionCurveTrack；本次接入正式根位移。Rush、Rush_End 的缺失曲线通过 `RootMotionCurveBakingService.BakePlanarBoneTranslation` 从原始动画 Bip001 平面位移生成，Explode 使用已有曲线。没有重新编译 ACL 或足部缓存。

## Rush 条件

- 从 Run/TurnBack 或闪避允许窗口收到普通攻击请求后，入口由 Badge_S01 选择。无标签为普通，有标签为强化；没有普通 Rush 中途升级为强化的边。
- 项目现有角色初始标签包含 Badge_S01，此项未改，因此默认体验强化版本。
- 普通版释放条件由错误的“没有 MoveAxis”改为没有 AttackHeld，并受第 13 帧起的 RushRelease 窗口控制。
- 第 23 帧后 SawExplode 或状态主体完成进入爆发。它们具有相同目的状态，作者图用 OR 表达；松手条件优先。
- 强化版第 38 帧进入循环，循环周期 80 帧；SawExplode 进强化爆发，松手进收尾。
- 修复三个出口错误共用同一个 owned condition graph；各自保持唯一 placement owner。

## 尚未对齐的边界

- 原始松手条件还包括 `!Bool_IsClicking`。它由动画事件触发 `Corin_Clicking`，本轮尚未取得该能力的完整生产逻辑，不能用猜测的连点超时补齐。当前已实现按住/松手。
- 当前目标来自动作准入时的不可变快照；原游戏锁定后的移动目标追踪和各状态重新选敌不等同于此。尚未改写目标查询职责。
- Badge_S03 的自动衔接不是本次已完成项。
- 仅覆盖当前已有技能阶段，不代表新增反击、连携、支援或所有 Branch01/03 技能。

## 验证

运行代码和作者代码已完成 Unity 编译。共享 Timeline 与准入资产已正式生成；`PublishSelected(Attack, RushAttack, BranchAttack)` 成功发布 Fixed、Float32。普攻生成的 MCP 响应因连接超时丢失，后续以磁盘导入回读、闭包校验和发布成功确认完成，没有将丢失响应当作成功证据。

重新导入三份技能资产后，正式 GraphClosure 校验均通过，dirty=false。普攻五段、普通 Rush、强化 Rush 循环和 E Walk 的 MotionWarp 回读均为 TargetResponse；输入来源为 MoveAxis，E Walk 输入响应为 0.5。该次回读后 Console 为 0 个错误。

运行准备检查另发现 Fixed Timeline 先前拒绝加权和无限切线，不能直接消费 Rush 原曲线。已在正式 Fixed/Float32 GameplayAbilityCurve 中支持加权 Bézier，在 Fixed Timeline 适配层保留阶跃段语义，没有重采样或改写原始曲线。通过编辑器只读数值核对，36 条已发布响应曲线共 5645 个采样点，Fixed 与 Unity 曲线最大绝对误差为 0.001220703（该点原值约 687.28，位于 Rush 起手输入响应急降段），未出现负值；没有启动角色或 replay。

| 技能 | Fixed/Float32 共同 SourceRevision | 共同 SemanticHash |
| --- | --- | --- |
| Attack | f01b8ea950c669001c87c40bd6f1f8eb636ecb2786e01c7c9ac43d7bb2dee850 | 1838b138420ab7b9d29e9d4d8be60ca251ef20d8cdb151276f8ddc8963b78ba7 |
| RushAttack | c53c4f799477bbd8e10fde3cf14297af0fd8dfbe9bd5d7880f54ba1475cb9391 | 92724be898ed97289b1c835789b83f960a23830ec73600ee3dbf00c3a54484b5 |
| BranchAttack | e5a17cb1a4404179b60814af6091e792578fac02858dd4ddc8e91d6ab67a6660 | 3c63e26af4a7e26266ecb1911cc1723da68c180d99d13277b2a3c9e2fdc0b1a6 |

没有运行 replay、没有新增测试、没有修改 IK，画面效果由用户手测。

最后补齐 Float32 通用加权曲线求值后，全项目编译曾遇到同期 Pose 改动的阻塞：`CharacterPoseNativeFrameCoordinator.cs:29` 无法解析 `IActionPresentationClockCoordinator`。相关 Pose 文件属于其它在途改动，本轮未修改。提交 `40fdedc21` 时保留了该阻塞记录；随后工作区同步修复完成，最新 Bee 编译记录无失败节点，目标 Editor 回读为 Play=false、Compiling=false，Console 为 0 错误。最终编译门槛已恢复通过，可以交由用户手测。
