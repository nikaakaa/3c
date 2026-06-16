## 1. 范围收缩
- [x] 1.1 确认本变更只保留中心 Bloom、屏幕横向高光和方向火花。
- [x] 1.2 确认不新增 `BlockImpactVfx2`。
- [x] 1.3 确认不新增第二套播放入口。
- [x] 1.4 确认不接动画事件。
- [x] 1.5 确认不接 Timeline Signal。
- [x] 1.6 确认不接真实格挡判定。
- [x] 1.7 确认不读取输入系统。
- [x] 1.8 确认不修改 FullBody、Locomotion、Action、回滚或网络。
- [x] 1.9 确认不删除现有 log。

## 2. 删除复杂层
- [x] 2.1 从默认 Prefab 移除旧世界空间横条 quad。
- [x] 2.2 从默认 Prefab 移除 `ImpactRing`。
- [x] 2.3 从默认 Prefab 移除 `EnergyArc`。
- [x] 2.4 删除 runtime ring mesh 生成代码。
- [x] 2.5 删除 runtime ribbon/arc mesh 生成代码。
- [x] 2.6 删除不再使用的 arc/ring 材质。
- [x] 2.7 删除不再使用的 arc shader。

## 3. 保留核心反馈
- [x] 3.1 保留中心 HDR 核心。
- [x] 3.2 保留屏幕空间横向高光。
- [x] 3.3 保留方向性火花。
- [x] 3.4 保留 Inspector 预览入口。
- [x] 3.5 保留 Edit Mode 预览能力。

## 4. 火花轻量物理感
- [x] 4.1 火花使用 `ParticleSystemRenderMode.Stretch`。
- [x] 4.2 火花使用 Trail 模块。
- [x] 4.3 火花绑定正式 trail 材质。
- [x] 4.4 火花根据攻击方向喷射。
- [x] 4.5 火花使用 gravity modifier。
- [x] 4.6 火花使用 velocity damping。
- [x] 4.7 火花不创建 Rigidbody。
- [x] 4.8 火花物理感参数写入正式 Profile。

## 5. 自动测试
- [x] 5.1 更新 Profile 必需贴图测试。
- [x] 5.2 更新 Profile 参数范围测试。
- [x] 5.3 更新 Prefab 结构测试。
- [x] 5.4 更新火花 Stretch/Trail 测试。
- [x] 5.5 新增火花 gravity/dampen 测试。
- [x] 5.6 更新屏幕横向高光 shader 参数测试。
- [x] 5.7 运行 `ThirdPersonRendering.Tests.BlockImpactVfxTests`。
- [x] 5.8 运行 `ThirdPersonRendering.Tests.BlockImpactPostProcessingTests`。
- [x] 5.9 不运行 Unity batchmode。

## 6. 用户手动验证
- [x] 6.1 打开 `Assets/Scenes/Sandbox.unity`。
- [x] 6.2 确认 Main Camera 开启 Post Processing。
- [x] 6.3 启用格挡冲击预览对象。
- [x] 6.4 触发一次预览。
- [x] 6.5 确认中心 Bloom 明显。
- [x] 6.6 确认横向亮光是屏幕水平延展。
- [x] 6.7 确认没有方形横条板子。
- [x] 6.8 确认火花有速度拉伸和短 trail。
- [x] 6.9 确认火花有轻微下坠或速度衰减。

## 7. OpenSpec 校验
- [x] 7.1 运行 `openspec validate refactor-block-impact-vfx-depth --strict --no-interactive`。
- [x] 7.2 修正所有 validation 问题。
- [x] 7.3 明确告诉用户实施阶段怎么验证。

## 8. 颜色和调参修正
- [x] 8.1 定位中心亮光偏绿来自贴图 RGB 参与发光色相。
- [x] 8.2 将中心 additive shader 改为贴图只提供 shape mask。
- [x] 8.3 将火花 additive shader 改为贴图只提供 shape mask。
- [x] 8.4 默认 Flash 材质引用正式 Profile 中心 mask 贴图。
- [x] 8.5 Profile 暴露中心软边参数。
- [x] 8.6 Profile 暴露屏幕闪白权重。
- [x] 8.7 Profile 暴露屏幕径向权重。
- [x] 8.8 Profile 暴露屏幕横光权重。
- [x] 8.9 Profile 暴露屏幕色散权重。
- [x] 8.10 Controller 播放时读取新增 Profile 参数。
- [x] 8.11 后处理脉冲允许单项权重放大到 3。
- [x] 8.12 自动测试覆盖贴图不染色规则。
- [x] 8.13 自动测试覆盖默认 Flash 材质贴图引用。
- [x] 8.14 自动测试覆盖新增 Profile 参数钳制。
