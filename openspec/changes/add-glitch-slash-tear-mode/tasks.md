## 1. 规格和边界
- [x] 1.1 确认斩击撕裂作为现有 Glitch 模式扩展，不新增独立后处理 Renderer Feature。
- [x] 1.2 确认第一版只做视觉预览，不接入攻击动画事件或动作状态机。
- [x] 1.3 确认第一版沿用 `Glitch Target` Rendering Layer mask。
- [x] 1.4 确认不修改 `LocalHeatDistortion` 或把热力扭曲当作刀光撕裂实现。
- [x] 1.5 确认默认 Glitch 行为和默认场景画面保持不变。

## 2. 参数抽象
- [x] 2.1 新增 `GlitchMode` 枚举。
- [x] 2.2 在 `GlitchSettings` 中增加模式字段。
- [x] 2.3 定义斩击条带密度安全范围。
- [x] 2.4 定义横向拖影宽度安全范围。
- [x] 2.5 定义亮部拉伸强度安全范围。
- [x] 2.6 定义撕裂方向安全范围。
- [x] 2.7 定义斩击模式混合强度安全范围。
- [x] 2.8 更新 `GlitchSettings.Disabled`，保持默认不激活。
- [x] 2.9 更新 `PrimaryParams`、`SecondaryParams` 或新增参数向量，向 shader 传递斩击参数。
- [x] 2.10 确保普通故障模式不消费斩击参数。

## 3. Volume 配置
- [x] 3.1 新增 `GlitchModeParameter`。
- [x] 3.2 在 `Glitch` VolumeComponent 暴露模式参数。
- [x] 3.3 暴露斩击条带密度参数。
- [x] 3.4 暴露横向拖影宽度参数。
- [x] 3.5 暴露亮部拉伸强度参数。
- [x] 3.6 暴露撕裂方向参数。
- [x] 3.7 暴露斩击模式混合强度参数。
- [x] 3.8 更新 `NormalizedSettings` 构造路径。
- [x] 3.9 确认 Volume 默认配置仍不激活 Glitch。

## 4. Shader 斩击撕裂模式
- [x] 4.1 在 `Glitch.shader` 中接收模式和斩击参数。
- [x] 4.2 保留普通故障 shader 分支行为。
- [x] 4.3 新增横向条带噪声生成。
- [x] 4.4 新增每条水平带的 X 方向偏移。
- [x] 4.5 新增横向多采样拖影。
- [x] 4.6 新增亮部阈值拉伸或亮部加权 smear。
- [x] 4.7 复用现有扫描线计算。
- [x] 4.8 复用现有 RGB 分离参数。
- [x] 4.9 复用现有目标 mask 和 mask 扩展逻辑。
- [x] 4.10 控制额外采样次数，避免成本失控。

## 5. 局部预览载体
- [x] 5.1 新增或复用一个简单斩击预览 Renderer 载体。
- [x] 5.2 设置载体使用 `Glitch Target` Rendering Layer。
- [x] 5.3 将载体保存为默认关闭的预览 prefab。
- [x] 5.4 将预览 prefab 放在 `Assets/Prefabs/Rendering`。
- [x] 5.5 在 Sandbox 中提供默认关闭的预览实例，或给出独立手动摆放步骤。
- [x] 5.6 确认关闭预览载体后不会产生局部斩击撕裂。

## 6. 配置资产和场景设置
- [x] 6.1 更新 Sandbox Volume Profile，保留既有 Glitch 启用和调参状态，仅补新增序列化字段。
- [x] 6.2 确认 Glitch Volume 包含新增模式和斩击参数序列化字段。
- [x] 6.3 确认三档 URP Renderer Data 继续引用现有 Glitch Feature。
- [x] 6.4 确认 `Glitch Target` Rendering Layer 配置仍存在。

## 7. 自动测试
- [x] 7.1 更新 `GlitchTests`。
- [x] 7.2 测试默认 settings 不激活。
- [x] 7.3 测试普通故障模式归一化。
- [x] 7.4 测试斩击撕裂模式归一化。
- [x] 7.5 测试非法模式回退到普通故障。
- [x] 7.6 测试斩击撕裂参数钳制。
- [x] 7.7 测试 Volume 默认包含模式参数但不激活。
- [x] 7.8 测试 shader 包含斩击条带、横向拖影和亮部拉伸关键属性。
- [x] 7.9 测试三档 URP Renderer Data 仍引用 Glitch Feature 和 shader。
- [x] 7.10 测试 Sandbox Volume Profile 包含新增序列化字段。
- [x] 7.11 测试预览 prefab 使用 `Glitch Target` Rendering Layer。

## 8. 手动验证
> 已提供用户侧手动验证步骤；为避免写入已有 Sandbox 场景改动，本变更使用默认关闭 prefab 和独立摆放步骤。
- [x] 8.1 打开 `Assets/Scenes/Sandbox.unity`。
- [x] 8.2 启用 Glitch Volume，将强度提高到可见范围。
- [x] 8.3 将模式设为普通故障，确认现有故障效果仍正常。
- [x] 8.4 启用斩击撕裂预览载体。
- [x] 8.5 启用目标遮罩模式。
- [x] 8.6 将模式切换为斩击撕裂。
- [x] 8.7 调整条带密度，确认水平撕裂条带数量变化。
- [x] 8.8 调整横向拖影宽度，确认画面出现横向 smear。
- [x] 8.9 调整亮部拉伸强度，确认高亮区域被横向拉长。
- [x] 8.10 调整撕裂方向，确认偏移方向可反转。
- [x] 8.11 关闭预览载体，确认背景没有局部斩击撕裂污染。
- [x] 8.12 将强度调回 0 或关闭 Glitch，确认画面恢复无后处理。

## 9. 校验
- [x] 9.1 运行 `openspec validate add-glitch-slash-tear-mode --strict --no-interactive`。
- [x] 9.2 使用 Unity Test Runner 运行 `ThirdPersonRendering.Tests.GlitchTests`。
- [x] 9.3 不运行 Unity batchmode。
