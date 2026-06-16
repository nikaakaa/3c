## Context
用户指出当前效果“只是贴图”“像板子”“没有立体感”，这个判断是正确的。参考图的格挡亮光不是一张横条贴图，而是屏幕空间强光、局部世界空间火花、弧线、冲击环、Bloom 和短暂后处理共同叠加的结果。

现有 `add-block-impact-vfx` 的优点是入口已经齐全：Prefab、Profile、Controller、PostProcess、Sandbox 和测试都在同一条表现链路上。这个变更不另起炉灶，而是在同一入口内重构视觉层职责。

## Target Visual Model
目标效果收缩为三层：

1. 中心 HDR 核心：命中点附近短暂过曝，负责把 Bloom 点亮。
2. 屏幕横向强光：屏幕空间水平 streak，负责参考图里最明显的横条亮光。
3. 方向火花：沿攻击反方向或切线喷射，使用速度拉伸、短 trail、重力和速度衰减制造轻量物理感。

## Architecture
### Data Layer
`BlockImpactVfxProfile` 继续作为正式配置资产，负责保存：
- 中心核、火花、trail、横光 mask 等贴图引用。
- 每层颜色、HDR 强度、生命周期、尺寸、数量、随机种子策略。
- 每层启用开关。
- 后处理强度、横向 streak 宽度、长度、软边和衰减参数。

配置缺失时应报告错误或让预览明确不可用，不生成临时贴图，也不切到隐式 fallback。

### Runtime Layer
`BlockImpactVfxController` 继续消费 `BlockImpactVfxRequest`。Controller 只做调度和生命周期，不把每层细节写死在一个大方法里。

建议内部拆成表现层模块：
- CoreFlash layer：控制中心 HDR 核心和局部软光面。
- Spark layer：控制 ParticleSystem burst、方向、速度、重力、速度衰减和 trails。
- ScreenPulse bridge：向现有 `BlockImpactPostProcess` 提交屏幕中心、强度和时间。

这些模块都只依赖 request、profile 和已绑定的 prefab 子对象，不读取动画事件、状态机、输入或战斗对象。

### Rendering Layer
#### 横向强光
主横条亮光应由 URP 屏幕空间 pass 生成，而不是世界空间矩形 quad。

技术做法：
- Render Pass 读取当前相机颜色目标。
- Hidden shader 使用屏幕 UV 和冲击中心计算水平 streak。
- 垂直方向用窄高斯衰减控制厚度。
- 水平方向用长衰减控制延展，并支持中心附近更强。
- 输出 HDR additive 颜色，让后续 Bloom 放大。
- 支持温暖核心色和冷/紫边缘色的可选混合。
- 强度为 0 或无脉冲时不改变画面。

这样横条始终贴合屏幕镜头语言，不会在场景里暴露一张方形板子。

#### 中心 HDR 核心
中心核可以保留一个小的 camera-facing soft impostor，但它只负责局部过曝，不负责整条横光。shader 必须使用贴图 alpha 或程序化径向 mask，并通过软边、防方边 alpha 和 HDR 强度避免看见贴图边界。

#### 火花
火花用 ParticleSystem burst：
- Renderer 使用 Stretched Billboard。
- 粒子朝速度方向拉伸。
- Shape/发射方向按攻击反方向或切线 cone 生成。
- Trail 模块开启，trail material 使用独立 trail alpha。
- 粒子生命周期短，速度高，尺寸随机。
- Main 模块使用 gravity modifier，让火花短暂下坠。
- Limit Velocity Over Lifetime 使用 dampen，让火花速度快速衰减。
- 可以叠少量无 trail 的星点火花，但主感受来自速度线和方向性。

#### 贴图职责
贴图只做视觉层的输入，不是完整效果本体：

| 贴图类型 | 用途 |
| --- | --- |
| soft flash / star mask | 中心核 alpha 或局部星芒 |
| horizontal glow mask | 可选辅助横光轮廓，主横光仍由屏幕 shader 生成 |
| spark sprite | 火花粒子 alpha |
| trail texture | trail 宽度方向 alpha |
| noise texture | 火花亮度闪烁或后续扩展 |

## Tests
自动测试需要覆盖以下内容：
- Profile 必需贴图和参数范围。
- Prefab 仍只有一个正式 `BlockImpactVfx` 入口。
- 中心核、火花、screen pulse 子层都存在。
- 火花 ParticleSystem Renderer 使用 Stretched Billboard 或等效速度拉伸配置。
- 火花 Trail 模块启用且绑定正式材质。
- 火花使用重力或速度衰减参数，而不是 Rigidbody。
- 横向强光的主路径来自 `BlockImpactPostProcess` 的屏幕 shader 参数，而不是只依赖世界空间横向 quad。
- shader 暴露 HDR 强度、fade、screen center/streak 参数。
- Controller 不依赖 AnimationClip Event、Timeline、输入系统或状态机对象。
- Sandbox 预览默认不自动刷屏，手动触发可播放。

## Manual Validation
用户验证时应按两轮看效果：

第一轮只看核心和横光：
1. 打开 `Assets/Scenes/Sandbox.unity`。
2. 确认 Main Camera 开启 Post Processing。
3. 启用格挡冲击预览对象。
4. 在 Profile 中只开启中心核和 screen streak。
5. 触发一次预览。
6. 确认看到中心强 Bloom 和屏幕水平亮条。
7. 确认没有可见方形贴图边界。

第二轮看火花：
1. 开启火花。
2. 再触发一次预览。
3. 确认火花沿一个方向喷射，而不是均匀点状散开。
4. 确认火花有速度拉伸、短 trail、下坠或速度衰减。
5. 调整攻击方向，确认火花方向跟随变化。
6. 关闭 screen streak，确认中心核和火花仍存在。

## References
- Unity Particle System Renderer module: https://docs.unity3d.com/6000.4/Documentation/Manual/PartSysRendererModule.html
- Unity Particle System Trails module: https://docs.unity3d.com/6000.4/Documentation/Manual/PartSysTrailsModule.html
- Unity particle rendering and shading: https://docs.unity3d.com/6000.0/Documentation/Manual/particle-rendering-shading.html
