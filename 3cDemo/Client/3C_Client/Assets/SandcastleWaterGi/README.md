# Sandcastle Water & GI

从 **Sandcastle Demo**(Bubblebird Studio,Unity 6000.4.10f1)打包字节码反汇编还原的:
水体渲染 Shader + 高度场动态 GI。学习用途。

## 安装

1. Unity 6000 → Window → Package Manager → **+** → **Add package from disk**
2. 选择本文件夹的 package.json

## 内容

| 文件 | 说明 |
|---|---|
| Runtime/Water/Lit_Water.shader | 水面(内置管线,GrabPass 折射)。由 SM5.0 汇编逐条翻译:双层法线滚动、折射、Beer-Lambert 吸收、Fresnel、Blinn-Phong(指数 1500)、浅水渐隐 |
| Runtime/Gi/GiLighting.compute | 3 个 kernel(Raycast / Gi / BlurGi),对应原作的高度场 GI |
| Runtime/Gi/GiLighting.cs | 驱动组件:每帧 dispatch,输出全局纹理 _GILightMap / _GiHeightmap |
| Runtime/Gi/GiTerrain.shader | 示例地形:顶点位移 + GI 光照采样 |
| Runtime/Water/WaterSimulation.compute | GPU 浅水模拟(原作 Water 9 个 kernel 中的核心 4 个:Initialize / VelocityIntegration / HeightIntegration / UpdateTangent) |
| Runtime/Water/WaterSimulation.cs | 水模拟驱动:每帧 dispatch,发布 _WaterCellsA/B 给水面 shader |
| Samples~/WaterGiSample/ | SampleBeach.cs + Beach.unity:自动构建的沙滩场景 |

## 快速开始(示例)

**导入 .unitypackage 后直接打开 Samples/WaterGiSample/Beach.unity**——场景自带相机和自动构建组件,shader 编译完会自己长出沙滩:256² 沙洲高度场 + 128² GPU 浅水模拟水面 + 实时 GI + 太阳。什么都不用点。

如果场景没自动建(或想重建):选中 Beach 物体,右键 SampleBeach 组件 → Build Beach。

- 水是**真模拟**:重力梯度 + 四方向通量 + 蒸发 + 边界阻尼,水往低处流、洼地积水、高处退潮(还原自原作 Water 的 9 个 compute kernel 中的核心 4 个)
- GI 每帧对高度场做 DDA raymarch,沙丘影子实时

## 原理(与原作对照)

原作 Gi 类(IL2CPP dump)→ 3 个 kernel 反汇编:

- **Raycast**:沿太阳方向的 Amanatides-Woo DDA,记录每格沿线的最高遮挡点
- **Gi**:每格 N 个随机方向(围绕法线,Wang hash),DDA 步进,
  地平线仰角遮挡 AO + 沿地平线按角度加权积分表面色(bounce)+ 太阳 5 次幂高光,
  与上一帧 lerp 平滑(原 cb0[139].z)
- **BlurGi**:4 邻均值模糊 → 软阴影

## Unity 2022.3 / 已有工程导入

- 支持 Unity 2022.3 及以上(内置管线或 URP 都行)
- 导入到已有工程后打开 Samples/WaterGiSample/Beach.unity;如果之前导入失败留下残留,先手动删除 Assets/SandcastleWaterGi 再重新导入
- SampleBeach 会自动检测渲染管线并选用对应 shader(URP 用 LitSandURP / WaterURP / GiTerrainURP)
- **URP 项目**需要:Universal Renderer Data 里开启 **Opaque Texture** 和 **Depth Texture**(水面折射和水深要用),URP 12+ 均可
- 首次打开场景时等 shader 编译完成,沙滩会自动构建(Beach 物体上的 SampleBeach 组件,可随时右键 Build Beach 重建)

## 水面的已知差异

- 原作顶点直接从 GPU 浅水模拟的 ComputeBuffer 读高度场(24 位定点,4 邻平均);
  本包暂用程序化波占位,后续可接 Water.compute(9 个 kernel 的反汇编在
  Sandcastle Demo/shader_blobs/)
- 原作是 URP;本包是内置管线(GrabPass),URP 项目请开启兼容或改用
  _CameraOpaqueTexture 版本
- 勾选材质 Use scene depth + 相机开启 Depth Texture 可获得真实水下距离吸收

## 参数速查(游戏原值)

- 折射 refractionFactor = 0.0121
- 水色 = (0.96, 0.986, 0.9995),散射色 = (0.568, 0.357, 0)
- 沙色 = (0.25, 0.5, 0.5)
- 高光指数 = 1500,Fresnel = 0.75·(1-cosθ)⁵
