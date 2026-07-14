# Tasks

## 1. 确认动画层现状

- [x] 1.1 梳理 `CharacterPipelineHost` 到 `CharacterPresentationStage` 的当前调用链。
- [x] 1.2 梳理 `TimelinePlaybackScheduler` 到 `AnimationContribution` 的当前采样链。
- [x] 1.3 梳理 `AnimancerAnimationPresenter` 当前实际写入的 Animancer 字段。
- [x] 1.4 列出角色管线中仍可能绕过动画层的 Animator、TimelinePlayer 或 PlayableGraph 调用。
- [x] 1.5 确认旧 `AnimationCommand` 模型没有继续作为正式运行输入。

## 2. 定义动画层数据合同

- [x] 2.1 将动画贡献模型命名收口为正式动画层输入。
- [x] 2.2 为动画贡献补齐来源类型或来源身份字段。
- [x] 2.3 为动画贡献补齐 layer id 的正式表达。
- [x] 2.4 为动画贡献补齐 clip time、normalized time、weight、priority、blend mode 的边界约束。
- [x] 2.5 明确 contribution 只允许表达动画意图，不允许携带 gameplay 裁决结果。
- [x] 2.6 删除或迁移任何并行的动画播放命令结构。

## 3. 选择并实现唯一动画层定义来源

- [x] 3.1 决定采用管线定义层表作为唯一真数据。
- [x] 3.2 将最小 layer 表接入 `CharacterPipelineDefinition`。
- [x] 3.3 让 Timeline track 引用 layer id 而不是重复保存层级固定信息。
- [x] 3.4 确认未采用 Timeline/节点完整 layer 信息作为第二真数据。
- [x] 3.5 为缺失 layer、非法 layer 和重复 layer id 提供硬错误。
- [x] 3.6 确认没有旧 `AnimationPresentationPolicySO` 被重新读取。

## 4. 收口 Timeline 动画轨道

- [x] 4.1 将 `AnimationTrack` 输出字段对齐正式动画贡献合同。
- [x] 4.2 保留 Timeline clip overlap、ease in、ease out 和 weight curve 对权重的贡献。
- [x] 4.3 保留 RootMotionCurve 只进入 Motion 输出，不进入动画播放 adapter。
- [x] 4.4 确认 `AnimationTrack` 不直接调用 Animator、Animancer、TimelinePlayer 或 PlayableGraph。
- [x] 4.5 确认 Timeline runtime 实例只由 `TimelinePlaybackScheduler` 推进。

## 5. 建立动画层运行时

- [x] 5.1 将当前 `AnimationMixer` 收口并重命名为动画层运行时核心。
- [x] 5.2 让动画层运行时按 layer 收集本帧 contribution。
- [x] 5.3 让动画层运行时校验 layer 合法性。
- [x] 5.4 让动画层运行时按 priority 筛选 override contribution。
- [x] 5.5 让动画层运行时按 additive 规则保留 additive contribution。
- [x] 5.6 让动画层运行时生成每层最终播放计划。
- [x] 5.7 让动画层运行时生成只读 snapshot。
- [x] 5.8 确认 snapshot 不参与 transition、motion 或 gameplay 决策。

## 6. 收口 Animancer adapter

- [x] 6.1 让 `AnimancerAnimationPresenter` 只消费播放计划。
- [x] 6.2 让 presenter 不再直接理解 contribution 仲裁规则。
- [x] 6.3 让 presenter 负责创建或复用 Animancer state。
- [x] 6.4 让 presenter 正确设置 state time、speed、weight。
- [x] 6.5 让 presenter 正确设置 Animancer layer weight、mask 和 additive。
- [x] 6.6 让 presenter 停止本帧不再被播放计划引用的 state。
- [x] 6.7 确认 presenter 不自动补 Idle 或其它 fallback clip。

## 7. 接入基础动画来源

- [x] 7.1 确认 Idle 状态必须通过状态行为图或 Timeline 输出 base layer contribution。
- [x] 7.2 确认 Move 或 Locomotion 不恢复旧 locomotion SO。
- [x] 7.3 确认 Attack 等动作不恢复旧 action SO。
- [x] 7.4 为后续 Action runtime 预留同一 contribution 写入口。
- [x] 7.5 确认所有来源都写同一个 presentation animation input 集合。

## 8. 清理运行时旁路

- [x] 8.1 搜索角色管线运行路径中的 `Animator.Play`。
- [x] 8.2 搜索角色管线运行路径中的 `Animator.CrossFade`。
- [x] 8.3 搜索角色管线运行路径中的 `TimelinePlayer` 运行依赖。
- [x] 8.4 删除或隔离角色管线运行路径中的直接播放入口。
- [x] 8.5 保留 BTSMTL 编辑器预览所需代码时，确保角色管线不引用它。
- [x] 8.6 搜索并清理旧 animation presentation policy 读取路径。

## 9. 更新规格和文档

- [x] 9.1 更新 `openspec/project.md` 中动画层主链路描述。
- [x] 9.2 对比 `btsmtl-runnable-timeline-node` current spec 和已完成 Timeline 管线权威 change 的冲突。
- [x] 9.3 调整 proposal/design/spec，使其明确采用管线定义 layer 表。
- [x] 9.4 运行 `openspec validate complete-character-animation-layer-runtime --strict --no-interactive`。

