## 1. 现状和影响确认
- [x] 1.1 读取 `URPGenshinToon.shader` 的 Forward、ShadowCaster、DepthOnly、DepthNormals 和 Outline pass。
- [x] 1.2 读取 `ToonInput.hlsl`、`ToonForwardPass.hlsl` 和 `ToonOutlinePass.hlsl` 的 varyings、CBUFFER 和 fragment 输出。
- [x] 1.3 查找当前角色材质使用的 Toon shader 资产和默认材质参数。
- [x] 1.4 查找 `Assets/Scripts/Rendering/Runtime` 中现有配置、Controller 和测试命名风格。
- [x] 1.5 对准备修改的 shader/HLSL 符号运行 GitNexus impact 分析。
- [x] 1.6 对准备新增或修改的运行时 C# 符号运行 GitNexus impact 分析。
- [x] 1.7 若 impact 返回 HIGH 或 CRITICAL，暂停实现并先报告影响面。

## 2. 点阵参数和配置抽象
- [x] 2.1 定义点阵参数名、默认值和安全范围。
- [x] 2.2 新增正式 Profile 或等效配置对象表达点阵参数。
- [x] 2.3 实现配置参数归一化，不提供 fallback 配置。
- [x] 2.4 确认默认配置不改变现有角色材质画面。
- [x] 2.5 新增运行时写入对象，负责把归一化参数写入目标 renderer。
- [x] 2.6 运行时写入使用 `MaterialPropertyBlock`，不修改 shared material。
- [x] 2.7 运行时对象不依赖状态机、Animancer、输入系统或 gameplay 判定。

## 3. Shader 裁剪实现
- [x] 3.1 新增共享 HLSL 点阵裁剪函数。
- [x] 3.2 在 `ToonInput.hlsl` 增加点阵参数字段。
- [x] 3.3 在 Forward varyings 中保留屏幕坐标所需数据。
- [x] 3.4 在 Forward fragment 中根据点阵 mask 执行 clip。
- [x] 3.5 在 Outline pass 中传递屏幕坐标。
- [x] 3.6 在 Outline fragment 中复用同一套点阵 clip。
- [x] 3.7 为 DepthOnly pass 接入同一套屏幕点阵 clip。
- [x] 3.8 为 DepthNormals pass 接入同一套屏幕点阵 clip。
- [x] 3.9 确认 ShadowCaster pass 不接入屏幕点阵 clip。
- [x] 3.10 确认 shader 不切换到普通 Transparent queue。
- [x] 3.11 确认 `Fallback Off` 保持不变。

## 4. 资产和预览接入
- [x] 4.1 新增默认点阵透明 Profile 资产。
- [x] 4.2 新增测试或预览材质，使用正式 Toon shader。
- [x] 4.3 新增默认关闭的 Sandbox 预览入口。
- [x] 4.4 预览入口引用正式 Profile，不创建临时配置。
- [x] 4.5 预览入口不自动播放、不默认污染场景画面。

## 5. 自动测试
- [x] 5.1 新增 `ScreenSpaceDotTransparencyTests`。
- [x] 5.2 测试默认配置不激活点阵透明。
- [x] 5.3 测试非法 spacing、radius、coverage、hardness 和 offset 被钳制。
- [x] 5.4 测试配置缺失时不静默生成 fallback 配置。
- [x] 5.5 测试运行时写入使用 `MaterialPropertyBlock`。
- [x] 5.6 测试运行时对象不修改 shared material。
- [x] 5.7 测试 shader 包含点阵参数和共享裁剪函数引用。
- [x] 5.8 测试 Forward pass 包含点阵 clip。
- [x] 5.9 测试 Outline pass 包含点阵 clip。
- [x] 5.10 测试 DepthOnly pass 包含点阵 clip。
- [x] 5.11 测试 DepthNormals pass 包含点阵 clip。
- [x] 5.12 测试 ShadowCaster pass 不包含点阵 clip。
- [x] 5.13 测试 shader 未切换到普通 Transparent queue。
- [x] 5.14 测试默认 Profile、预览材质和 Sandbox 预览入口引用关系。
- [x] 5.15 运行定向 EditMode 测试 `ThirdPersonRendering.Tests.ScreenSpaceDotTransparencyTests`。

## 6. 工具验证和收尾
- [x] 6.1 运行 `openspec validate add-screen-space-dot-transparency --strict --no-interactive`。
- [x] 6.2 运行相关渲染 EditMode 测试，确认现有渲染能力未被破坏。
- [x] 6.3 运行 GitNexus `detect_changes()` 检查影响范围。
- [x] 6.4 更新任务状态，确保只勾选真实完成项。

## 7. Haste Diffuse 角色材质补齐
- [x] 7.1 确认 Sandbox 和 Haste 角色 prefab 使用的 Diffuse 风格角色 shader。
- [x] 7.2 对 Haste 角色 shader 运行 GitNexus impact 分析。
- [x] 7.3 将点阵裁剪函数抽到正式共享 HLSL include。
- [x] 7.4 让 Toon wrapper 复用共享 HLSL include。
- [x] 7.5 为 `W/savCharacterNEW` 接入屏幕点阵参数和 clip。
- [x] 7.6 为 `sav_CHAREYESHELLS` 接入屏幕点阵参数和 clip。
- [x] 7.7 为 `savglasstest` 接入屏幕点阵参数和 clip。
- [x] 7.8 将默认预览材质切到 Haste Diffuse 风格角色 shader。
- [x] 7.9 扩展自动测试覆盖 Haste Diffuse 风格角色 shader。
