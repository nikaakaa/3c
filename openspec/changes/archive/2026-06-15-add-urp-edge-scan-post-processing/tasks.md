## 1. 规格和边界
- [x] 1.1 确认 Edge Scan 是独立 URP 后处理能力，不并入 Glitch。
- [x] 1.2 确认第一版只做 Sandbox 手动预览，不接入格挡或动作事件。
- [x] 1.3 确认主渲染路径不使用球体网格材质。
- [x] 1.4 确认不新增相机脚本、相机叠加或 `OnRenderImage`。
- [x] 1.5 确认默认参数不改变画面。

## 2. 参数抽象
- [x] 2.1 新增 `EdgeScanSettings`。
- [x] 2.2 定义扫描强度安全范围。
- [x] 2.3 定义扫描源世界坐标参数。
- [x] 2.4 定义扫描半径安全范围。
- [x] 2.5 定义扫描宽度安全范围。
- [x] 2.6 定义扫描颜色参数。
- [x] 2.7 定义深度边缘阈值安全范围。
- [x] 2.8 定义法线边缘阈值安全范围。
- [x] 2.9 定义边缘强度安全范围。
- [x] 2.10 定义距离衰减安全范围。
- [x] 2.11 定义 `Disabled` 默认值。
- [x] 2.12 提供 `IsActive` 激活判定。
- [x] 2.13 提供传入 shader 的参数向量。

## 3. Volume 配置
- [x] 3.1 新增 `EdgeScan` VolumeComponent。
- [x] 3.2 暴露强度参数。
- [x] 3.3 暴露扫描源世界坐标参数。
- [x] 3.4 暴露扫描半径参数。
- [x] 3.5 暴露扫描宽度参数。
- [x] 3.6 暴露扫描颜色参数。
- [x] 3.7 暴露深度边缘阈值参数。
- [x] 3.8 暴露法线边缘阈值参数。
- [x] 3.9 暴露边缘强度参数。
- [x] 3.10 暴露距离衰减参数。
- [x] 3.11 输出 `NormalizedSettings`。
- [x] 3.12 保持默认不激活。

## 4. Renderer Feature 和 Render Pass
- [x] 4.1 新增 `EdgeScanRendererFeature`。
- [x] 4.2 新增 `EdgeScanRenderPass`。
- [x] 4.3 RenderPass 声明需要 `Color`、`Depth`、`Normal` 输入。
- [x] 4.4 RenderPass 执行时机放在内置后处理之前。
- [x] 4.5 RendererFeature 从 Volume Stack 读取规范化设置。
- [x] 4.6 settings 未激活时不入队。
- [x] 4.7 shader 缺失时不入队。
- [x] 4.8 使用临时 RT 复制当前相机颜色。
- [x] 4.9 写回当前相机颜色目标。
- [x] 4.10 释放临时 RTHandle。

## 5. Shader
- [x] 5.1 新增 `Hidden/3C/PostProcessing/EdgeScan` shader。
- [x] 5.2 采样当前相机颜色。
- [x] 5.3 采样相机深度纹理。
- [x] 5.4 采样相机法线纹理。
- [x] 5.5 由深度重建像素世界位置。
- [x] 5.6 根据扫描源和扫描半径计算薄壳 mask。
- [x] 5.7 通过深度邻域差异计算深度边缘。
- [x] 5.8 通过法线邻域差异计算法线边缘。
- [x] 5.9 合并深度边缘和法线边缘响应。
- [x] 5.10 用扫描薄壳 mask 限制边缘高亮范围。
- [x] 5.11 支持扫描颜色和强度叠加。
- [x] 5.12 支持距离衰减。
- [x] 5.13 保持非扫描区域原始颜色。

## 6. 配置资产
- [x] 6.1 将 Edge Scan Renderer Feature 接入 High Fidelity Renderer Data。
- [x] 6.2 将 Edge Scan Renderer Feature 接入 Balanced Renderer Data。
- [x] 6.3 将 Edge Scan Renderer Feature 接入 Performant Renderer Data。
- [x] 6.4 在 Sandbox Volume Profile 中加入默认关闭 Edge Scan 参数。
- [x] 6.5 确认 Main Camera 后处理开启时可通过 Volume 控制效果。

## 7. 自动测试
- [x] 7.1 新增 `EdgeScanTests`。
- [x] 7.2 测试默认 settings 不激活。
- [x] 7.3 测试正强度和有效宽度激活。
- [x] 7.4 测试半径钳制。
- [x] 7.5 测试宽度钳制。
- [x] 7.6 测试深度边缘阈值钳制。
- [x] 7.7 测试法线边缘阈值钳制。
- [x] 7.8 测试边缘强度钳制。
- [x] 7.9 测试距离衰减钳制。
- [x] 7.10 测试 Volume 默认不激活。
- [x] 7.11 测试 Volume 正强度激活。
- [x] 7.12 测试 RendererFeature 缺 shader 时不渲染。
- [x] 7.13 测试 RendererFeature 有 shader 且 settings 激活时可渲染。
- [x] 7.14 测试 RenderPass 必要输入包含 Color、Depth、Normal。
- [x] 7.15 测试三档 URP Renderer Data 引用 Edge Scan Feature 和 shader。
- [x] 7.16 测试 shader 包含深度重建、深度边缘、法线边缘、扇形范围和扫描线关键参数。
- [x] 7.17 测试 Sandbox Volume Profile 包含 Edge Scan 序列化字段。

## 8. 手动验证
> 已提供用户侧手动验证步骤；实现过程未保存任何临时场景调参状态。
- [x] 8.1 提供步骤：打开 `Assets/Scenes/Sandbox.unity`。
- [x] 8.2 提供步骤：确认 Main Camera 开启 Post Processing。
- [x] 8.3 提供步骤：在 Volume Profile 中启用 Edge Scan。
- [x] 8.4 提供步骤：将扫描源设置在角色或场景中心附近。
- [x] 8.5 提供步骤：将强度调到明显值。
- [x] 8.6 提供步骤：缓慢调整扫描半径，观察扫描波从源点向外扩散。
- [x] 8.7 提供步骤：将扫描方向设置为角色前方，扇形角度保持约 120 度。
- [x] 8.8 提供步骤：确认扫描扇形内出现重复蓝色世界空间扫描线。
- [x] 8.9 提供步骤：确认扫描最外层前沿有更亮的白蓝辉光。
- [x] 8.10 提供步骤：确认扫描前沿内侧有受控暗化，不是整屏均匀变色。
- [x] 8.11 提供步骤：确认扫描命中的轮廓、高差、墙角和物体边界高亮。
- [x] 8.12 提供步骤：调整扫描线间距，确认地形读数线疏密变化。
- [x] 8.13 提供步骤：调整前沿辉光强度，确认最外层亮边变化。
- [x] 8.14 提供步骤：调整暗化强度，确认扫描区内侧压暗变化。
- [x] 8.15 提供步骤：调整深度边缘阈值，确认遮挡断面响应变化。
- [x] 8.16 提供步骤：调整法线边缘阈值，确认表面折角响应变化。
- [x] 8.17 提供步骤：调整扫描宽度，确认高亮环带厚度变化。
- [x] 8.18 提供步骤：将强度调回 0，确认画面恢复无扫描效果。

## 9. 校验
- [x] 9.1 运行 `openspec validate add-urp-edge-scan-post-processing --strict --no-interactive`。
- [x] 9.2 使用 Unity Test Runner 运行 `ThirdPersonRendering.Tests.EdgeScanTests`，12 passed。
- [x] 9.3 不运行 Unity batchmode。

## 10. 死亡搁浅式视觉校正
- [x] 10.1 观察并拆分死亡搁浅式扫描的主要视觉层次。
- [x] 10.2 将效果目标从完整扫描薄壳调整为世界空间扇形扫描区。
- [x] 10.3 为 settings 增加扫描方向参数。
- [x] 10.4 为 settings 增加扇形角度参数。
- [x] 10.5 为 settings 增加扫描线间距参数。
- [x] 10.6 为 settings 增加扫描线宽度参数。
- [x] 10.7 为 settings 增加扫描线强度参数。
- [x] 10.8 为 settings 增加前沿辉光强度参数。
- [x] 10.9 为 settings 增加暗化强度参数。
- [x] 10.10 将新增参数暴露到 `EdgeScan` VolumeComponent。
- [x] 10.11 将新增参数传入 `EdgeScanRenderPass`。
- [x] 10.12 将 shader 改为扇形范围 mask。
- [x] 10.13 将 shader 改为扫描前沿 mask。
- [x] 10.14 在 shader 中生成重复世界空间扫描线。
- [x] 10.15 在 shader 中合成白蓝前沿辉光。
- [x] 10.16 在 shader 中合成扫描前沿内侧暗化。
- [x] 10.17 保留深度和法线轮廓高亮。
- [x] 10.18 更新 Sandbox Volume Profile 的默认关闭参数。
- [x] 10.19 更新 EditMode 测试覆盖新增 settings 参数。
- [x] 10.20 更新 EditMode 测试覆盖新增 shader 关键路径。
- [x] 10.21 重新运行 OpenSpec 校验。
- [x] 10.22 重新运行 `ThirdPersonRendering.Tests.EdgeScanTests`。
- [x] 10.23 检查 Unity Console，确认没有 Edge Scan 相关错误；当前存在非本次变更的 Test Framework 清理错误。
- [x] 10.24 补充新的用户侧手动验证参数建议。
