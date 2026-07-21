# Change: 将 Timeline Authoring Preview 收敛为纯表现预览

## Why

当前独立 Timeline Authoring Preview 在 Timeline 包含 TreeClip、MotionCurve 或 MotionWarp 时会创建完整 Preview Simulation Session。每次拖动游标都可能重新创建 Actor registration、隐藏 CharacterController、Diagnostics、Presentation runtime 与 Session composition，并从 Tick 0 重放到目标时间。这让作者为调整动画片段、衔接和时间窗口而拖动游标时出现明显卡顿，也把完整 Gameplay 验证混入了本应快速采样表现的作者工具。

Timeline Editor 已经拥有互斥的 Authoring Preview 与 Live Debug。Authoring Preview 应只负责动画表现采样与已创作 MotionCurve 的只读轨迹投影；TreeClip、Action、MotionWarp、Window、Blackboard、GameplayEffect 与 WorldSolver 的真实执行应只由正式运行 Session 产生，并通过 Live Debug 观察。

## What Changes

- Authoring Preview 始终通过 `CharacterPresentationProjection`、`CharacterAnimationPlaybackRuntime`、`AnimationPlaybackLifecycle` 与 Animancer 采样动画表现。
- MotionCurve 在 Authoring Preview 中按 Timeline 帧率从起点绝对求值，并把累计位移与朝向只读投影到 visual root；退出预览、切换 Target 或 Timeline 时恢复原姿态。
- TreeClip、Action Cue 与 MotionWarp 在 Authoring Preview 中继续显示和编辑，但不执行 Gameplay、目标修正、碰撞或 WorldSolver。
- 删除 Timeline Preview 专用 Simulation Session、Preview Source、Preview Pipeline、Preview passes、Preview input port、Actor registration、Action target snapshot UI、`Preview` tick source、直接 Program entry override 与对应配置资产。
- `CharacterPipelineHost` 不再持有 `PreviewComposition`；Preview target 只要求正式 Definition、Program、Projection、Animation Presentation Profile、Animancer 与 visual root。
- Live Debug 继续只读消费正式 runtime trace，负责观察 TreeClip、Window、MotionWarp、Solver 与动画生命周期事实。
- 拖动同一量化帧时不重复采样动画表现。
- Timeline 游标拖动以固定的面板按下坐标计算累计位移，避免游标布局更新反向抵消后续拖动。
- Timeline 手动 seek 复用当前 animation playback，只更新采样时间并立即应用精确姿势；Timeline、Target 或 authoring 内容切换时才重置 lifecycle。
- 正式运行按 Presentation Profile 使用 Timed transition；Authoring Preview 使用同一 playback lifecycle 和 Animancer adapter 的 Immediate transition mode，确保首次采样与 seek 不等待表现时间推进。
- 共享 Timeline 被多个 TimelineNode 引用时，Authoring Preview 复用其同一动画 producer，并使用确定性的 canonical operation 生成预览事件身份。

## Impact

- Affected specs: `btsmtl-timeline-editor-preview`, `character-animation-pipeline`, `gameplay-simulation-session-composition`
- Affected code: BTSMTL Timeline Editor preview、Character Pipeline animation/motion preview、Float32 Preview Pipeline、Unity preview composition 配置
- Breaking change: 删除 `PreviewSimulationSessionSourceDefinition`、Preview Pipeline/pass contracts、`PreviewComposition` 序列化字段与 editor-only Action target snapshot 输入
- Agent impact: 不改变 Timeline/Track/Clip authoring 数据、节点、端口、身份或 Patch 能力，因此 Agent Snapshot、Patch schema、lowerer、handler 与 validator 不变

## Conflicts

- `openspec/project.md` 当前“隔离 Preview Simulation Session”描述将由本 change 替换为纯表现 Authoring Preview。
- current `btsmtl-timeline-editor-preview` 中允许执行 TreeClip 与 MotionWarp Gameplay Preview 的要求将被移除；真实执行统一由 Live Debug 观察。
- active `add-timeline-animation-marker-sync` 已按本 change 重基线为纯表现 Authoring Preview与Live Debug，不得恢复 Preview Simulation。
