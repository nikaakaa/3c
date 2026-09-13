## Why

Timeline 的 MotionCurveClip 仍内嵌 PositionX/Y/Z/Yaw，导致创建源码重复携带大批曲线关键帧。前一份精简变更已归档，本次将已讨论的数据段引用具体落实为正式 RootMotionCurveAsset 所有权和统一消费链，而非只缩短导出文本。

## What Changes

- **BREAKING** MotionCurveClip 使用现有 RootMotionCurveAsset 类型的正式外部源引用，删除内嵌位移与 yaw 曲线；不定义 Timeline 专属曲线源。
- 源资产拥有曲线与源时间，Clip 拥有源区间、Timeline 位置、播放映射、权重和混合；Timeline 不拥有外部源资产的删除权。
- Attack/Dodge 现有嵌入曲线一次无损迁为正式源资产，保留关键帧、插值与时间语义，不抽点、不从动画重烘焙代替旧内容、不保留兼容读取。
- Timeline 正式采样、MotionWarp 源读取、Timeline semantic emitter 和 ControlMotion catalog emitter 统一消费源资产与同一时间映射；Gameplay Runtime 继续只消费 compiled Program。
- Timeline 的位移/yaw 改为源引用展示和真实 owner 导航；Weight/Ease 仍是 Clip 自有曲线。源资产由正式资源编辑入口修改。
- 两个作者 MCP 和偏函数式链式 builder 不变；export_code 每个源只声明一次类型化引用，generate_assets 只恢复绑定与片段配置，不隐式提取、复制或烘焙素材。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `character-root-motion-curves`：统一源所有权、源时间映射、存量曲线无损迁移和编译边界。
- `character-csharp-authoring`：MotionCurve 导出类型化源引用与生成范围边界。
- `btsmtl-timeline-editor-preview`：位移/yaw 的真实 owner 展示与导航，保留 Timeline 自有曲线编辑。
- `character-motion-warp-authoring`：Warp 继续绑定具体 Clip，并按其正式源区间读取运动。

## Impact

影响 RootMotionCurveAsset 正式创建/导入、MotionCurveClip/TimelineData typed binding、Timeline 曲线目录与编辑入口、MotionWarp、Timeline/ControlMotion semantic 编译、源依赖失效标记、C# 薄适配和正式生成源码。无需新建程序集反向依赖，当前 Timeline 已引用 RootMotion 程序集。用户已在规划完成后明确授权绑定实现任务实施并设置 goal，具体绑定见 design；不向其它任务扩散工作。

现行 spec 对照：

| 现行内容 | 本次处理 |
| --- | --- |
| root-motion spec 已要求 Timeline 引用曲线并编译到 Program，但还保留“内联位移”名称 | 收敛为外部源唯一拥有曲线，编译产物不是另一份作者源 |
| root-motion 资产描述仅覆盖动画烘焙来源 | 补入存量嵌入曲线的正式无损迁移，来源未知不伪造动画或采样率 |
| Timeline Curve Catalog 要求直接编辑 MotionCurve 的 Position/Yaw | 删除这四类 Timeline-local 可写通道，提供源引用与导航 |
| Curve Editor 禁止 RootMotionCurveAsset 进入 Timeline 可写目录 | 保留，与源 owner 导航一致 |
| C# spec 已要求源数据段，不复制逐 tick 数据 | 将 MotionCurve 具体绑定到现有 RootMotionCurveAsset，不再把其嵌入运动样本当“独立作者曲线”输出 |
| MotionWarp 以 Clip 身份绑定、限定窗口与 Action/Override/ActorLocal 等规则 | 保留；增加源区间映射，不能改绑共享资产身份 |

这些变更以本目录的 spec delta 提出；当前 spec 在实施同步时替换，archive 历史不改写。已有其它任务正确的配置和业务规则不回退，实际字段冲突交用户决定。
