# Action 动画重播意图设计

## Context

动作层和动画层现在已经分离：

- Action resolver 输出稳定 action id、motion spec 和 action animation key。
- Action lifecycle 接收 accepted action，维护 active action 与 state time。
- Character frame output 阶段把 animation request 交给统一 Animancer Presenter。
- Presenter 只负责播放和只读进度，不决定 Action 是否 accepted。

连续 Dodge 暴露的问题在于协议粒度不够。`Action.Dodge.Directional` 是动画资源语义 key，不是一次动作播放的实例身份。Presenter 当前只看到同 key，就把第二次 Dodge 当作同一次播放继续提交。

同时，restore 场景确实需要“同 key 不重启”。因此修复不能简单删除 same-key early return，而要明确播放意图身份。

## Goals

- 连续 accepted Action 即使使用相同动画 key，也能触发新的 Action 动画播放段。
- 同一个 active Action 在多帧重复提交时保持幂等，不每帧重启。
- restore 后同一次 Action 播放继续提交时不重启，保护回滚恢复语义。
- 播放意图身份是纯数据，可测试、可恢复或可确定性重建。
- Presenter 只消费播放意图，不生成播放意图，不参与业务仲裁。

## Non-Goals

- 不重写 Action Catalog、Dodge resolver 或打断策略。
- 不把动画 clip、TransitionAsset、AnimancerState 或 Animator 状态放进请求模型。
- 不改变基础移动动画播放时钟的回滚权威；该方向由已有 active change 处理。
- 不新增第二 Presenter 或 Dodge 专用 animation applier。

## Decisions

### Decision: 稳定 key 和播放意图身份分离

`ActionAnimationKey` 继续只表达“播放哪个语义动画”。新增或等价扩展的 Action playback intent 负责表达“这是哪一次动作播放”。

Presenter 的 Action 重播判断必须至少包含：

- action animation key
- playback intent identity
- 当前播放状态是否仍可继续

相同 key 且相同 playback intent 可以幂等保持；相同 key 但不同 playback intent 必须重新播放。

### Decision: 播放意图由 Action 生命周期或其上游纯数据生成

Presenter 不能根据当前 Animancer state 自行生成动作实例身份。实现应在 accepted action 进入生命周期时生成或确定一个纯数据播放意图，并在 active action 后续帧复用。

第一版可以从 accepted request 的稳定输入身份、action state、variant、provider/source order 或等价 action instance sequence 组合出播放意图；不得只使用当前 frame `SourceStep`，因为 active action 后续帧的 source step 会随 tick 变化。

### Decision: Restore resume 是同一次播放语义

restore 或等价恢复入口必须能让 Presenter 知道当前视觉播放段属于哪个 Action playback intent，或至少以明确 restore-resume 模式建立一次同 key 幂等提交。恢复后的首个同 intent Action 请求不得归零。

如果未来 strict gameplay 需要 Action animation clock 参与回滚比较，播放意图身份必须进入对应纯数据 restore state，或能从 restored active action 确定性重建。

### Decision: 最小实现先覆盖 Action base layer

本变更只要求当前 Action base layer 的播放请求支持意图身份。Locomotion alias、TurnBack playback clock、UpperBody layer、AvatarMask 和视觉 blend 权重不在本提案内。

### Decision: 测试先锁定协议语义

实现时先添加窄范围模型和 Presenter 测试，证明：

- 同 key 不同 playback intent 会重播。
- 同 key 同 playback intent 不会重播。
- restore 后同 playback intent 不会重播。
- Action lifecycle 新 accepted action 会产生新 playback intent。

然后再补 frame pipeline/FullBody output 级测试，覆盖连续 Dodge 的真实提交路径。

## Risks / Trade-offs

- Risk: 只用 `SourceStep` 作为 intent 会把每帧 active action 误判为新播放。
  - Mitigation: 规格要求 intent 在同一个 active action 内稳定，不能只绑定当前 frame step。
- Risk: 修复连续 Dodge 时破坏 restore 不重启。
  - Mitigation: Presenter same-playback 判断必须同时覆盖 key 与 intent，并保留 restore resume 测试。
- Risk: 与 `formalize-animation-playback-rollback-authority` 的 Locomotion restore 语义重复。
  - Mitigation: 本变更只碰 Action playback intent；Locomotion profile playback clock 仍由该 active change 管。
- Risk: 后续 Attack combo 可能需要多段同 key 或同输入派生多次播放。
  - Mitigation: intent 模型保持 Action 通用，不写 Dodge-only 分支；如 combo 需要额外 sequence，可在 resolver/lifecycle 内扩展纯数据 identity。

## Migration Plan

1. 审计现有 Action animation request、playback request、snapshot 和 presenter progress 字段。
2. 添加纯数据 playback intent identity 或等价字段，并保持默认值表示无有效 intent。
3. 让 Action lifecycle 在新 accepted action 时确定新 intent，在 active action 后续 tick 复用 intent。
4. 更新 Presenter same-playback 规则：同 key 同 intent 保持，同 key 不同 intent 重播。
5. 更新 restore progress 行为，使 restore 后同 intent 请求保持当前播放段。
6. 补齐自动测试和静态边界验证。

## Verification Notes

- Play Mode 中连续按两次 Shift，并让两次都进入 `Action.Dodge.Directional`，第二次动画应从开头重播。
- 连续后闪进入同一个 `Action.Dodge.Backstep` 时也应重播。
- 从 rollback restore 或测试恢复的 Action 播放进度，再提交同一次播放意图，不应被重置到 0。
- 若出现失败，优先查看 action animation key、playback intent、source step、current normalized time 和是否走了 `action-animation-played` 日志。

## Open Questions

- 后续 Attack combo 如果允许同一个输入请求派生多段播放，是否需要在 resolver 中引入 action-local sequence。当前 Dodge 修复不需要先决定该扩展。
