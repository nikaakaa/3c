## Context
当前 Unity 客户端使用 URP 14，径向模糊已经通过 VolumeComponent、ScriptableRendererFeature 和 ScriptableRenderPass 接入三档 URP Renderer Data。Glitch 应复用同一条后处理路径，避免新增相机脚本、`OnRenderImage` 或并行渲染出口。

## Goals
- 提供一个能在 Volume Profile 中启用和调参的故障后处理。
- 提供一个可选的目标遮罩模式，只影响指定 Rendering Layer 的 Renderer 及其故障扩展区域。
- 第一版不依赖外部噪声贴图，降低资产准备成本。
- 保持参数抽象和 URP 执行实现分离。
- 提供可自动测试的参数与 Renderer 接入逻辑。

## Non-Goals
- 不实现玩法逻辑到 Glitch 参数的触发绑定。
- 不新增噪声贴图资产。
- 不使用 stencil 作为第一版局部 Glitch 的遮罩来源。
- 不实现 VHS、CRT 或视频压缩完整风格包。
- 不新增独立相机渲染路径。

## Decisions
- Decision: 使用 `VolumeComponent` 表达 Glitch 参数。
  - Alternatives considered: MonoBehaviour 挂相机参数。该方式会绕过 URP Volume 体系，不采用。
- Decision: shader 内部通过 hash 生成块状伪随机噪声和扫描线。
  - Alternatives considered: 强制依赖噪声贴图。第一版会增加资产依赖和调试变量，不采用。
- Decision: 使用运行时 Mask RT 表达局部 Glitch 的目标范围。
  - Alternatives considered: stencil。Stencil 会引入额外 render state 约束，容易影响合批和 URP 后处理阶段读写边界，第一版不采用。
- Decision: 使用 Rendering Layer 筛选局部 Glitch 目标。
  - Alternatives considered: GameObject Layer。GameObject Layer 通常服务碰撞、射线、AI 和交互逻辑，容易与玩法逻辑冲突，不采用。
- Decision: Glitch Render Pass 在 `BeforeRenderingPostProcessing` 执行，并通过 `Blitter.BlitCameraTexture` 写回相机颜色目标。
  - Alternatives considered: 在 `AfterRenderingPostProcessing` 显式绑定相机颜色目标后手写全屏三角形。该阶段在当前 URP Renderer Data 中不能稳定写回画面，不采用。

## Risks / Trade-offs
- 风险: 程序噪声不如手绘噪声贴图有质感。
  - Mitigation: 第一版保证功能闭环，后续可扩展可选噪声贴图参数。
- 风险: RGB 分离和多次采样会增加成本。
  - Mitigation: 暴露有限范围，并在测试中验证钳制。
- 风险: 多个后处理在同一 injection point 下顺序影响表现。
  - Mitigation: 先与径向模糊使用相同 URP 自定义 pass 路径，后续如需固定顺序再单独审批排序策略。
- 风险: Mask RT 多一次目标 Rendering Layer 绘制和额外采样。
  - Mitigation: 遮罩模式默认关闭，只在需要局部 Glitch 时启用；遮罩绘制只使用指定 Rendering Layer。

## Migration Plan
1. 新增运行时代码和 shader。
2. 新增 EditMode 测试。
3. 将 Renderer Feature 加入现有三档 URP Renderer Data。
4. 在场景 Volume Profile 中手动添加 Glitch 组件验证。
5. 需要局部效果时启用 `Use Target Mask`，并将目标 Renderer 的 `Rendering Layer Mask` 勾选 `Glitch Target`。
