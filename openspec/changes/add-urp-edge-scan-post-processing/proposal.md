# Change: 新增 URP 边缘扫描波后处理

## Why
需要复刻类似《死亡搁浅》扫描器扫过场景时只在几何轮廓、高差和遮挡边界上显色的效果。此前用球面材质表达扫描范围容易变成可见球壳，不能稳定提取场景边缘，也难以和当前 URP 后处理体系统一。

## What Changes
- 新增 `Edge Scan` URP 后处理能力，通过世界空间扫描波半径驱动屏幕空间边缘检测。
- 使用 Volume 组件配置扫描源、方向、扇形角度、半径、宽度、颜色、强度、深度边缘阈值、法线边缘阈值、重复扫描线、前沿辉光、暗化和衰减参数。
- 通过 `ScriptableRendererFeature` 和 `ScriptableRenderPass` 接入当前 URP Renderer Data，不新增相机脚本、`OnRenderImage` 或球面网格作为主渲染路径。
- shader 读取相机颜色、深度和法线纹理，重建世界位置后在扫描扇形内叠加重复蓝线、前沿白蓝辉光、受控暗化和深度/法线轮廓高亮。
- 第一版只提供 Sandbox 手动预览和参数验证，不接入格挡、动作状态机或动画事件。
- 提供 EditMode 测试覆盖参数钳制、默认不激活、Renderer 接入、必要输入声明和 shader 关键路径。

## Impact
- Affected specs: `urp-edge-scan-post-processing`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime`
- Affected shaders: `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing/EdgeScan`
- Affected tests: `3cDemo/Client/3C_Client/Assets/Tests/Editor/Rendering`
- Affected settings: High Fidelity、Balanced、Performant 三档 URP Renderer Data，Sandbox Volume Profile
