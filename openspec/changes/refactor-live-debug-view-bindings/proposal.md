# 提案：重构 Live Debug 目标与视图绑定

## Why

当前运行时 Trace、Debug Source Map、Graph overlay 和 Timeline overlay 都已存在，但 Live Debug 还没有形成作者可直接使用的产品闭环。

RuntimeDebugSession 同时保存 target、history、选中 runtime instance 和 follow/pin。Graph 窗口执行 FollowGraph 时会清除 Timeline 的 follow；Timeline 窗口执行 FollowTimeline 时又会清除 Graph 的 follow。两个窗口因此不能同时观察同一角色的状态机和 Timeline playback。

Target 注册后当前 Session 只刷新 UI，不会按当前作者页自动附着。退出 Play Mode 或角色销毁时 Session 直接清空 view model，最后一段 Trace 无法留在编辑器中复盘。Target、实例和 source mismatch 的状态也没有被区分为作者可理解的产品状态。

## What Changes

- 作者在 Play Mode 中以场景选中的 CharacterPipelineHost、唯一精确匹配 target 或显式 Target 菜单选择要观察的角色。
- Graph 与 Timeline 共享同一角色、channel 和历史时间位置，但各自保存自己的 Follow / Pin runtime instance。
- Graph 进入 Live Debug 时跟随当前 Graph；Timeline 进入 Live Debug 时跟随当前 Timeline 的 playback，不互相覆盖。
- 同一 Timeline 有多次播放时，作者能看到每次播放的来源与生命周期，并显式固定其中一次。
- runtime target 结束后保留只读的最后 Trace 快照，明确标记为已结束，不继续伪装成实时数据。
- 所有自动选择都必须基于显式场景角色或唯一的 source identity + content hash 精确匹配；不按名称、顺序或场景搜索结果猜测。

### 具体调整

- 将 RuntimeDebugSession 收敛为共享的 target、channel、live/pause、history 和只读 Trace snapshot 服务，删除其全局 runtime instance、Follow 和 Pin 状态。
- 增加窗口本地的 RuntimeDebugViewBinding 概念。每个 Graph 或 Timeline 窗口各自持有 source、Follow / Pin 模式和当前 runtime instance，不写入 authoring asset，不扫描 runtime clone。
- 引入统一 Target 解析规则：场景中显式选中的 Host 优先；无显式 Host 时，只有唯一 source/hash 精确匹配的 registered target 才能自动附着；零个或多个匹配对象必须显示明确状态并等待作者选择。
- 让 Graph、Timeline Target 菜单和 CharacterPipelineHostEditor 都调用同一条正式 target attach 路径，删除平行的 attach 语义。
- 为 Timeline playback 菜单提供由正式 Trace 构成的来源摘要，使同一 Timeline 的多次播放能按 playback、来源节点、状态 activation 和 terminal/lifecycle 区分。
- target 注销时由 Editor 复制不可变最终 snapshot 并进入 Ended 状态；runtime target 和 Trace Buffer 仍按原生命周期释放。
- 将 Live Debug 的空状态明确区分为未运行、多个候选、source 不存在、revision mismatch、当前 source 未执行、已结束。
- 修复 Tree Inspector 创建时使用 Unity USS 不支持伪类产生的 Console 错误。

### 不在范围内

- 不改变 CharacterPipeline、StateMachine、TimelinePlaybackScheduler、AnimationPlaybackLifecycle 或 Animancer 的 gameplay / presentation 运行结果。
- 不增加第二套 Trace、Timeline 采样、动画生命周期、runtime clone 绑定或 Debug Server。
- 不新增测试、不运行 Unity batchmode、不向作者资产保存 target、binding、history 或 Trace。
- 不恢复旧 Node direct-state、TimelinePlayer、旧 Workbench 或其它兼容调试路径。

## Impact

### 现行规格对比

- btsmtl-runtime-diagnostics 当前要求唯一 Session 共享 target、instance、follow/pin 和 history；这正是双窗口互相覆盖的根因。本 change 改为共享 target/时间/快照，instance binding 下放到各个观察窗口。
- btsmtl-timeline-editor-preview 当前把同一 Timeline 的多实例表达为全局 Follow Graph Selection 或 Pin Playback。本 change 改为 Timeline 窗口自己的 Follow / Pin，不再改写 Graph 窗口。
- btsmtl-tree-inspector-information-architecture 当前把 TreeWindow instance 归入共享 Session。本 change 保留共享 target/history，但将 TreeWindow 的 instance binding 明确为窗口本地。
- openspec/project.md 当前“Graph、Timeline 与 Host Inspector 共享唯一 target、instance、follow/pin”的架构口径需要同步改为“共享 target/history，视图独立 binding”。

现行规格存在上述冲突，归档本 change 时必须替换旧口径，不保留“全局 instance/follow/pin”兼容 API。

### 依赖与影响

- 本 change 依赖已稳定的 Source Map、Trace、runtime target registry 和 Timeline playback Trace；不改变 refactor-animation-presentation-authoring-boundary 已定义的动画 Trace producer 语义。
- 实现时需要与仍 active 的 refactor-animation-presentation-authoring-boundary 协调 btsmtl-runtime-diagnostics delta 与 openspec/project.md 的最终写入顺序，避免覆盖其动画 Trace 口径。
- 影响范围限于 BTSMTL/Diagnostics/Editor、Graph / Timeline 编辑器、CharacterPipeline Host Inspector 和 Tree Inspector USS。
