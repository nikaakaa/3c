## 归档口径（2026-09-04）

已完成 15/15 项任务：Corin 四个正式材质统一使用 ZZZMiyabi Toon，配置 Corin 自己的色表和纹理语义，以 Head Bone 经 MaterialPropertyBlock 提供面部朝向，并消费场景主光。范围是当前本地 Corin 材质接入，不代表已经完整复刻 ZZZ 原始渲染，也不扩大原文中的素材使用范围。本次只归档文档，不提交 Shader、材质、贴图或角色代码。

下文保留设计与实验过程；最终状态及被替代关系以同目录 proposal.md 的归档结果为准。

# ZZZMiyabi Toon 角色渲染接入设计

## 唯一渲染链

正式 Corin 链固定为：

Corin Renderer -> CelShaders/ZZZShader Material -> ZZZ Forward / Outline / ShadowCaster / Depth Pass -> 既有项目后处理

Genshin Toon、ASP 和旧 Simple Toon 不作为 Corin 的并行或回退路径。项目中其他角色原有材质和 Renderer Feature 保持不变。

## 原始 Shader 与项目适配边界

`Assets/Render/Shaders` 直接采用 ZZZMiyabi 的 ZZZShader、ZZZForwardPass、ZZZOutlinePasss 和所需 ShaderLib。渲染算法不重写。唯一核心适配位于 Face Attenuation 输出：将七个光照区间的合计权重归一，修正 Corin face lightmap 与参考模型数据分布不同造成的整体变黑。

ZZZShader 继续读取 URP Main Light 的方向和颜色。录像场景必须存在明确的 Directional Light；材质不再伪造独立光方向。

## Corin 材质输入

四个材质保留原 meta GUID：

- Body：Corin Body D、Body N、Body M。
- Hair：Corin Hair D、Hair N、Hair M。
- Weapon：Corin Weapon D、Weapon N、Weapon M。
- Face：Corin Face D 与 Famale Face lightmap，不读取 M 图。

N 图以 Linear、Default Texture 导入，因为 ZZZShader 自己从 RGB 解码切线法线；不得使用 Unity Normal Map 导入后的重新编码。M 图以 Linear 数据导入，R 通道选择五区材质 ID，G/B/A 延续仓库定义的数据语义。

Body、Hair、Weapon 和 Face 各自拥有独立 Shadow Color、Shallow Color、Post Tint 与 Outline 参数，不引用 Miyabi 材质或其角色色表。Corin Body M 图的 materialID 1 是皮肤，materialID 3 是熊和棕色布料，不得按 Miyabi 的皮肤区域解释。Body 常规明暗与 Face SDF 明暗使用不同衰减区间和不同底图色值，因此皮肤色表以最终画面颜色一致为目标，不要求两个材质保存相同数值。

## 面部方向

`CorinZZZFaceOrientation` 显式绑定 Head Bone 与 Face Renderer。Corin 骨骼的本地 Y 轴作为 Head Forward，本地 Z 轴作为 Head Left。组件在编辑态和运行态通过 MaterialPropertyBlock 写入 `_HeadForward` 和 `_HeadLeft`，不创建材质实例、不写共享材质，也不进入 Gameplay 状态。

## 场景光

ZZZShader 的明暗由 URP Main Light 驱动。正式录像场景需要一个 Directional Light，以场景旋转决定 Toon 明暗方向，以颜色和强度决定亮部能量。没有 Main Light 属于场景配置错误，不增加 Shader fallback。
