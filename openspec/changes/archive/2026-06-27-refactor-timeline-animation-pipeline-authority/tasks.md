# Tasks

## 1. 收口 TimelineNode 语义

- [x] 1.1 删除 `TimelineNode` 内部 runtime Timeline、TimelinePlayer 和 completed 播放状态的直接播放职责。
- [x] 1.2 保留 `TimelineReferenceModule` 作为 Timeline 资产引用来源。
- [x] 1.3 为 `TimelineNode` 增加播放请求 handle。
- [x] 1.4 `TimelineNode.OnStart()` 改为向正式管线上下文提交播放请求。
- [x] 1.5 `TimelineNode.OnUpdate()` 改为查询请求状态并返回节点状态。
- [x] 1.6 `TimelineNode.OnStop()` 取消未完成请求。
- [x] 1.7 `TimelineNode.OnReset()` 清理请求 handle。
- [x] 1.8 删除 `TimelineNode` 对 `ITimelinePlayerProvider` 的直接依赖。

## 2. 建立 Timeline 请求数据

- [x] 2.1 定义 `TimelinePlaybackRequest`。
- [x] 2.2 定义 `TimelinePlaybackHandle` 或等价稳定请求身份。
- [x] 2.3 定义 `TimelinePlaybackStatus`。
- [x] 2.4 在 `CharacterPipelineFrame` 或正式 output 层加入本帧 Timeline 请求集合。
- [x] 2.5 在 `CharacterGraphContext` 提供提交请求接口。
- [x] 2.6 在 `CharacterGraphContext` 提供查询请求状态接口。
- [x] 2.7 明确 frame transient 清理时机，避免请求跨帧丢失。

## 3. 做实 TimelinePlaybackScheduler

- [x] 3.1 让 `TimelinePlaybackScheduler` 读取本帧 Timeline 请求。
- [x] 3.2 建立 active Timeline runtime record。
- [x] 3.3 active record 保存 source id、Timeline asset、当前时间、状态和策略。
- [x] 3.4 `TimelinePlaybackScheduler.Tick()` 推进 active record 时间。
- [x] 3.5 播放完成时写回 request status。
- [x] 3.6 取消或打断时清理 active record。
- [x] 3.7 pipeline deactivate 或 dispose 时释放 active record。

## 4. 迁移动画轨道输出

- [x] 4.1 梳理当前 `AnimationTrack` 直接绑定 `TimelinePlayer` 的入口。
- [x] 4.2 定义动画轨道采样输出结构。
- [x] 4.3 让动画轨道按当前 timeline time 产出动画贡献数据。
- [x] 4.4 禁止动画轨道直接调用 Animator、TimelinePlayer 或 PlayableGraph。
- [x] 4.5 将动画贡献写入 presentation output 或动画 mixer 输入。
- [x] 4.6 保留非动画轨道后续迁移入口，但不新增 fallback 播放路径。

## 5. 建立动画混合运行时模型

- [x] 5.1 定义动画层身份。
- [x] 5.2 定义动画贡献来源身份。
- [x] 5.3 定义动画贡献权重、时间、fade 和优先级字段。
- [x] 5.4 定义每层最终混合结果。
- [x] 5.5 让 mixer 从本帧动画贡献生成结果。
- [x] 5.6 让 `CharacterPresentationStage` 消费混合结果或正式动画命令。
- [x] 5.7 删除直接绕过 mixer 的 Timeline 动画应用路径。

## 6. 输出动画混合预览数据

- [x] 6.1 定义 `AnimationBlendSnapshot`。
- [x] 6.2 snapshot 记录每层贡献列表和最终结果。
- [x] 6.3 snapshot 标记来源 Timeline、节点或状态。
- [x] 6.4 snapshot 只作为 debug output，不参与运行时决策。
- [x] 6.5 为后续编辑器动画层预览保留读取入口。

## 7. 清理和规格验证

- [x] 7.1 删除 `TimelineNode` 直接播放后不再使用的字段和接口引用。
- [x] 7.2 删除或迁移角色管线模式下的 `TimelinePlayer` autonomous tick 依赖。
- [x] 7.3 检查没有新增 Workbench、旧 SO/config 或第二套端口协议。
- [x] 7.4 运行 `openspec validate refactor-timeline-animation-pipeline-authority --strict --no-interactive`。
