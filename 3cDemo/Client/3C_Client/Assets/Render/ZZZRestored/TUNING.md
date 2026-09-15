# Nap 角色渲染手调指南

## 在哪调

选中任意 Original 材质(`Assets/AssetArt/Model/ZZZ/可琳/可琳tex/ZZZ导出/Materials/Original/*.mat`),
Inspector 即为手调面板(`ZZZRestored.NapAvatarStandardGUI`)。

面板结构:

- **MatCap 开关**(`_MATCAP_ON`):切换 toon pass 的 MatCap/非 MatCap 变体
- **区选择条**:全部 / 区1 / 区2 / 区3 / 区4 / 区5 / FB
  - 区1 = 无后缀属性,区2-5 = 同名+数字后缀,FB = Fallback(M 仅 MatCap 族)
  - 选"区3"时只显示区3的参数,同时全局面板里的全局参数照常显示
- **12 个标签页**:色带与明暗 / 高光 / MatCap / Rim / 描边 / 纹理输入 / 硬光与发光 / 光照输入 / 脸部 / 眼睛 / 特效状态 / 渲染状态
- 勾选"显示缺失属性"可以看到当前 shader 没有的条目(灰色)

## 参数从哪来

`MAT_Corin_*_ZZZ.json`(各 `ZZZ导出` 目录)是官方材质参数 dump;
`D:/ZZZ_Dump/output/corin_replication/*shader_parameter_snapshot*` 是运行时 payload 抓取。
两者都是只读参考——改参数直接在 Inspector 拧,不要反向编辑这些文件。

## 三个 shader 的分工

| shader | 用在 | 特有旋钮 |
|---|---|---|
| NapAvatarStandard | Body / Hair / Weapon | 色带、高光、MatCap、Rim、描边 |
| NapAvatarStandardFace | Face | `_UseFaceShadowPoint`、鼻线三件套(`_NoseLine*`)、`_FixedLightDirection`、`_LightMapUVFlip` |
| NapAvatarStandardEye | Eye | `_EyeColorMap`、`_UseVertexMaterialID` |

## 改坏了怎么办

- 单个材质:`Materials/Original/` 下的 .mat 在 git 里,`git checkout` 单文件回滚
- shader 本体:`Generated/Character/*.shader` 只加过一行 `CustomEditor`,逻辑零改动
- `git diff Assets/Render/ZZZRestored` 随时可核
