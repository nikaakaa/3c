# Tasks

## 1. Shader 接入

- [x] 1.1 从 ZZZMiyabi clone 复制 ZZZShader、Forward、Outline 与所需 ShaderLib
- [x] 1.2 保持 URP 14 原始 include 和 Pass 结构
- [x] 1.3 为 Corin 2D Face SDF 归一七段衰减权重

## 2. Corin 材质

- [x] 2.1 将 Body、Face、Hair、Weapon 统一迁移到 CelShaders/ZZZShader
- [x] 2.2 按 Linear Default Texture 配置 N 图，按 Linear 数据配置 M 图
- [x] 2.3 绑定各自 D、N、M 与 Face lightmap
- [x] 2.4 为 Corin 配置独立五区阴影、浅阴和轮廓参数

## 3. 角色装配

- [x] 3.1 新增 CorinZZZFaceOrientation
- [x] 3.2 在六个正式 Corin Runtime Profile Prefab 绑定 Head Bone 与 Face Renderer
- [x] 3.3 在 Unity 中确认脚本完成编译且 Prefab 引用有效

## 4. 清理与收口

- [x] 4.1 恢复 Genshin Shader 源码和旧 Renderer Feature 配置
- [x] 4.2 删除先前生成的 Genshin 中性 Light Map 与 Shadow Ramp
- [x] 4.3 确认四个正式 Corin 材质只引用 ZZZMiyabi Shader
- [x] 4.4 确认录像场景存在可用 Directional Light
- [x] 4.5 在同一相机和场景光下完成最终画面对账
