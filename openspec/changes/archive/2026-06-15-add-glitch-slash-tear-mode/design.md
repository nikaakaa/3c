## Context
现有 `urp-glitch-post-processing` 已经提供 `Glitch` Volume、`GlitchSettings`、`GlitchRendererFeature`、`GlitchRenderPass`、`GlitchMaskRenderPass` 和 `Hidden/3C/PostProcessing/Glitch` shader。当前 Glitch 支持全屏或目标 Rendering Layer 遮罩下的块状故障、水平抖动、RGB 分离和扫描线。参考图中的效果更接近局部横向信号撕裂和亮部拖影，而不是热力扭曲的空气折射。

## Goals / Non-Goals
- Goals: 在现有 Glitch 后处理系统中新增斩击撕裂模式。
- Goals: 第一版能通过局部 mask 预览刀光/冲击区域中的横向撕裂、拖影和扫描线。
- Goals: 保持 Volume 参数层和 shader 实现层分离，RenderPass 只消费归一化 settings。
- Goals: 不新增独立后处理路径，不复用 LocalHeatDistortion。
- Non-Goals: 本变更不生成真实武器轨迹 ribbon mesh。
- Non-Goals: 本变更不把效果接入攻击动画事件、Animancer notify 或动作状态机。
- Non-Goals: 本变更不实现多段刀光生命周期、伤害判定或网络同步。
- Non-Goals: 本变更不删除现有 Glitch 参数、log 或截图验证资产。

## Decisions
- Decision: 新增 Glitch 模式枚举，第一版包含 `DigitalGlitch` 和 `SlashTear`。
- Alternatives considered: 新增独立 `SlashTearRendererFeature`。该方案会复制颜色拷贝、mask、Volume 和 RenderPass 链路，和现有 Glitch 能力分裂。

- Decision: `SlashTear` 沿用现有 `Glitch Target` Rendering Layer mask。
- Alternatives considered: 立即新增刀光 ribbon mesh 写入专用 mask。该方案更接近最终攻击 VFX，但会跨越动画轨迹、VFX 载体和后处理多个系统，第一版预览成本过高。

- Decision: 第一版新增一个预览占位载体，使用普通 Renderer/Rendering Layer 限定局部范围。
- Alternatives considered: 直接接入武器骨骼。该方案需要先确认动作事件、武器挂点和生命周期，不适合作为本次视觉模式预览。

- Decision: shader 中新增横向条带采样和亮部 smear，但仍保持无外部噪声贴图依赖。
- Alternatives considered: 使用美术噪声/撕裂贴图。该方案可提升质感，但会阻塞第一版验证；后续可在模式稳定后追加。

## Risks / Trade-offs
- 斩击撕裂如果强度过大容易遮挡角色动作：默认配置必须低强度，预览时手动提高。
- 亮部拖影会增加额外采样：第一版限制采样次数和拖影宽度，并仅在 Glitch 激活时执行。
- 目标 mask 载体不是最终刀光轨迹：第一版只验证视觉语言，真实攻击绑定另起变更。
- 模式参数增多会让 Volume 面板变复杂：参数命名必须清楚，并保持普通 Glitch 默认行为不变。

## Migration Plan
1. 保留现有 Glitch 默认参数和默认行为。
2. 新增 `SlashTear` 模式后，默认 Volume 仍使用普通故障或关闭状态，不改变默认场景画面。
3. 在 Sandbox 中提供默认关闭的斩击撕裂预览载体。
4. 用户确认视觉方向后，再另起变更把 `SlashTear` 接入动作事件和刀光轨迹载体。

## Open Questions
- 斩击撕裂最终是跟随武器轨迹 ribbon，还是只作为攻击瞬间屏幕冲击 mask，需在预览后决定。
- 是否需要亮部颜色偏移、Bloom 配合或音画同步，后续根据手动验证结果决定。
