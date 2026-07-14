# Tasks

## 1. 建立 Root Motion 曲线数据模型

- [x] 1.1 确认正式目录位于 `Assets/GameScripts/Main/Runtime/Character/Pipeline/Motion/RootMotion`。
- [x] 1.2 新增 `RootMotionCurveAsset`。
- [x] 1.3 记录源 `AnimationClip` 引用。
- [x] 1.4 记录动画时长。
- [x] 1.5 记录烘焙采样率。
- [x] 1.6 增加累计本地位置 X 曲线。
- [x] 1.7 增加累计本地位置 Y 曲线。
- [x] 1.8 增加累计本地位置 Z 曲线。
- [x] 1.9 增加累计本地 yaw 曲线。
- [x] 1.10 增加总本地位移缓存。
- [x] 1.11 增加总 yaw 缓存。
- [x] 1.12 明确资产是派生数据，不承载 footphase、window、action policy 或旧 locomotion 配置。

## 2. 建立运行时求值器

- [x] 2.1 新增 `RootMotionCurveSample` 或等价采样结果结构。
- [x] 2.2 新增 `RootMotionCurveDelta` 或等价 delta 结果结构。
- [x] 2.3 新增 `RootMotionCurveEvaluator`。
- [x] 2.4 实现按时间采样累计本地位置。
- [x] 2.5 实现按时间采样累计 yaw。
- [x] 2.6 实现 `previousTime -> currentTime` 的本地 delta。
- [x] 2.7 实现本地 delta 转世界 displacement 的入口。
- [x] 2.8 明确单次播放使用 clamp。
- [x] 2.9 禁止 evaluator 访问 Animator、Animancer、Transform、CharacterController 或 Editor API。

## 3. 建立编辑器烘焙工具

- [x] 3.1 确认 editor 目录位于正式 `GameScripts/Main` 下，而不是 `Ref`。
- [x] 3.2 新增 Root Motion 曲线烘焙窗口或菜单命令。
- [x] 3.3 支持选择目标 `AnimationClip`。
- [x] 3.4 支持选择采样 Prefab。
- [x] 3.5 支持选择输出目录或目标资产。
- [x] 3.6 支持 `FromClip`、`60fps`、`120fps` 采样率。
- [x] 3.7 校验 Prefab 必须包含 `Animator`。
- [x] 3.8 校验 `Animator` 必须有 `RuntimeAnimatorController`。
- [x] 3.9 临时实例化采样对象并设置隐藏标记。
- [x] 3.10 使用 `AnimatorOverrideController` 将控制器中的动画替换成目标 clip。
- [x] 3.11 启用采样对象 root motion。
- [x] 3.12 按固定 deltaTime 驱动 Animator。
- [x] 3.13 读取每帧 `deltaPosition`。
- [x] 3.14 读取每帧 `deltaRotation`。
- [x] 3.15 把世界 delta 转换为本地 delta。
- [x] 3.16 累计本地 position.xyz。
- [x] 3.17 从 deltaRotation 累计 yaw。
- [x] 3.18 写入 `RootMotionCurveAsset` 曲线 key。
- [x] 3.19 写入总位移和总 yaw。
- [x] 3.20 销毁临时采样对象。
- [x] 3.21 保存资产并标记 dirty。
- [x] 3.22 覆盖已有资产必须走明确覆盖路径，不做隐式 fallback。

## 4. 接入 Timeline/Action 创作数据

- [x] 4.1 梳理当前 BTSMTL `AnimationTrack` / `AnimationClip` 数据字段。
- [x] 4.2 为动画 clip 数据增加显式 `RootMotionCurveAsset` 引用。
- [x] 4.3 确保引用字段走现有 BTSMTL inspector/序列化链路。
- [x] 4.4 不通过动画名、同目录、同名 asset 自动查找曲线。
- [x] 4.5 不恢复 BBB `MotionClipData` 或 `WarpedMotionData`。
- [x] 4.6 为后续 Action clip 复用同一引用模型保留接口边界。

## 5. 接入角色 motion 管线

- [x] 5.1 梳理 `StrictGameplayOutput.MotionIntent` 当前写入位置。
- [x] 5.2 定义 root motion 的正式 motion 提交结构。
- [x] 5.3 如需要，增加 motion contribution 列表。
- [x] 5.4 通过 motion resolver 生成最终 `MotionIntent`。
- [x] 5.5 在 `TimelinePlaybackScheduler` 采样动画轨道时同步采样 root motion 曲线。
- [x] 5.6 使用 Timeline clip 的 previous/current clip time 求 root motion delta。
- [x] 5.7 将 root motion delta 转换为角色世界 displacement。
- [x] 5.8 把结果提交到 strict gameplay motion 输出。
- [x] 5.9 保持 `CharacterMotionStage` 作为唯一移动应用边界。
- [x] 5.10 禁止 Timeline 轨道、采样器或表现层直接修改角色 Transform。

## 6. 清理旧路径和依赖

- [x] 6.1 检查正式代码没有引用 `BBBNexus.MotionClipData`。
- [x] 6.2 检查正式代码没有引用 `BBBNexus.WarpedMotionData`。
- [x] 6.3 检查正式代码没有通过 `PlayerSO` 扫描写回 root motion。
- [x] 6.4 检查角色管线模式不依赖 `TimelinePlayer.ApplyRootMotion` 移动角色。
- [x] 6.5 检查没有新增 locomotion、footphase、bodyclaim 或 animation presentation 旁路配置。
- [x] 6.6 删除实现过程中产生的临时桥接类型、命名约定查找和 fallback 分支。

## 7. OpenSpec 校验

- [x] 7.1 运行 `openspec validate add-root-motion-curve-baking --strict --no-interactive`。
- [x] 7.2 若 current spec 与本 proposal 冲突，更新 proposal 的冲突说明或拆出依赖项。
