## Context
当前项目已有 `RadialBlur`、`Glitch`、`LocalHeatDistortion` 三类 URP 自定义后处理，统一使用 `VolumeComponent -> Settings -> ScriptableRendererFeature -> ScriptableRenderPass -> Hidden shader`。边缘扫描波也应沿用这条路径。

死亡搁浅式扫描的关键不是屏幕横条纹，也不是一个可见球体，而是从世界源点向前扩散的扇形扫描区。它由几层视觉叠加组成：扇形范围限制、重复的世界空间扫描线、最外层白蓝前沿辉光、从前沿向内的短暂暗化，以及深度/法线轮廓高亮。

## Goals
- 使用世界空间扫描源、方向、扇形角度和扫描半径表达扩散波。
- 使用重复世界空间线条和前沿辉光建立死亡搁浅式扫描识别度。
- 使用深度与法线邻域差异提取边缘。
- 只在扫描扇形和已扫过区域内叠加高亮，避免全屏边缘常亮。
- 沿用当前 URP Renderer Feature 链路。
- 提供可测试的参数抽象和手动验证路径。

## Non-Goals
- 不接入格挡、动作事件、状态机或网络同步。
- 不使用球体网格材质作为主效果路径。
- 不新增独立相机、相机叠加或 `OnRenderImage`。
- 不实现完整 UI 扫描器或交互探测玩法。
- 不新增外部噪声贴图依赖。

## Decisions
- Decision: 使用独立 `EdgeScan` 后处理能力。
  - Reason: 该效果语义是世界空间扫描和边缘检测，不属于 Glitch 的信号故障，也不属于 LocalHeatDistortion 的折射区域。
- Decision: Volume 输出规范化 `EdgeScanSettings`。
  - Reason: 保持配置抽象和渲染实现分离，方便 EditMode 测试参数范围。
- Decision: RenderPass 声明需要 Color、Depth 和 Normal 输入。
  - Reason: shader 需要当前颜色叠加、深度重建世界位置、法线检测表面折角。
- Decision: shader 使用邻域采样做深度/法线边缘响应。
  - Reason: 比球面材质更能捕捉遮挡断面、物体轮廓和地形高差。
- Decision: shader 使用水平世界空间距离和方向点乘计算扇形扫描区。
  - Reason: 死亡搁浅式扫描更接近角色前方约 120 度的地形扫描，不是完整 360 度球壳。
- Decision: shader 在扫描扇形内生成重复世界空间线条、前沿辉光和暗化。
  - Reason: 单纯边缘检测视觉太薄，缺少 Odradek 扫描的地形读数感和推进感。
- Decision: 扫描源第一版由 Volume 参数直接配置。
  - Reason: 先验证美术效果和参数范围，后续再审批动作事件或运行时触发源绑定。

## Risks / Trade-offs
- 风险: DepthNormals 在不同 URP 设置下可能不可用。
  - Mitigation: RenderPass 明确声明 `ScriptableRenderPassInput.Normal`，测试检查必要输入。
- 风险: 邻域采样会增加成本。
  - Mitigation: 第一版使用固定小采样核，暴露阈值和强度，不提供不可控采样数。
- 风险: 扫描源从 Volume 配置不适合最终玩法触发。
  - Mitigation: 第一版只做预览；后续可审批一个轻量运行时控制器写入 Volume 或全局扫描源。
- 风险: 半透明物体和不写深度物体边缘不稳定。
  - Mitigation: 第一版明确以写入深度/法线的不透明几何为主验证对象。

## Migration Plan
1. 新增 EdgeScan Volume、Settings、RendererFeature、RenderPass 和 shader。
2. 将 Renderer Feature 接入三档 URP Renderer Data，默认关闭。
3. 在 Sandbox Volume Profile 添加默认关闭参数。
4. 新增 EditMode 测试。
5. 在 Sandbox 中手动调整扫描半径，验证边缘扫描波效果。
