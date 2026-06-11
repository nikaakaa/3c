## Context
当前 Unity 客户端使用 URP 14，项目有 High Fidelity、Balanced、Performant 三档 URP Renderer Data。径向模糊应接入同一条 URP Renderer Feature 路径，避免新增相机脚本、`OnRenderImage` 或并行渲染出口。

## Goals
- 提供一个能在 Volume Profile 中启用和调参的径向模糊。
- 保持参数抽象和 URP 执行实现分离。
- 提供可自动测试的参数与 pass 配置逻辑。
- 让设计者能在场景中通过 Volume 调整效果。

## Non-Goals
- 不实现动作系统触发径向模糊。
- 不实现受击、冲刺、蓄力等玩法事件到后处理参数的绑定。
- 不新增独立相机控制路径。
- 不替换 Unity 内置 Bloom、Vignette 或现有 SSAO 配置。

## Decisions
- Decision: 使用 `VolumeComponent` 表达径向模糊参数。
  - Alternatives considered: MonoBehaviour 挂相机参数。该方式会绕过 URP Volume 体系，不采用。
- Decision: 使用 `ScriptableRendererFeature` 和 `ScriptableRenderPass` 执行全屏 pass。
  - Alternatives considered: `OnRenderImage`。URP 中不作为当前项目主路径，不采用。
- Decision: shader 只负责全屏采样算法，参数钳制和激活判定放在 C# 侧。
  - Alternatives considered: shader 内部吞掉所有非法参数。会让测试和配置错误更难定位，不采用。

## Risks / Trade-offs
- 风险: 采样次数过高会影响性能。
  - Mitigation: 暴露有限范围，并在测试中验证钳制。
- 风险: 不同 URP 版本 API 存在差异。
  - Mitigation: 按项目当前 `com.unity.render-pipelines.universal` 14.0.12 实现。
- 风险: 直接修改 Renderer Data 可能影响所有使用对应质量档的场景。
  - Mitigation: 只接入现有三档 URP Renderer Data，不新增并行渲染路径，并在任务中显式确认接入点。

## Migration Plan
1. 新增运行时代码和 shader。
2. 新增 EditMode 测试。
3. 将 Renderer Feature 加入现有三档 URP Renderer Data。
4. 在场景 Volume Profile 中手动添加径向模糊组件验证。

## Open Questions
- 径向模糊默认中心是否先固定屏幕中心 `(0.5, 0.5)`，后续再由动作或目标系统驱动？
