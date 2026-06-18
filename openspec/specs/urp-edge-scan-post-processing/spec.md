# urp-edge-scan-post-processing Specification

## Purpose
定义 URP 边缘扫描波的接入方式、参数抽象、世界空间扫描、深度法线边缘检测、渲染顺序、非球体主路径和验证边界。
## Requirements
### Requirement: URP 边缘扫描波接入
系统 MUST 通过当前 URP Renderer Feature 链路接入边缘扫描波后处理，并且 MUST NOT 使用 `OnRenderImage`、额外相机叠加、独立 CommandBuffer 管线或球体网格材质作为主渲染出口。

#### Scenario: Renderer Feature 接入当前 URP
- **WHEN** 项目启用边缘扫描波后处理
- **THEN** 边缘扫描波 MUST 作为 `ScriptableRendererFeature` 挂接到当前使用的 URP Renderer Data
- **AND** 渲染执行 MUST 通过 `ScriptableRenderPass` 完成
- **AND** 系统 MUST NOT 新增相机脚本或独立后处理路径

#### Scenario: 默认不改变画面
- **WHEN** 边缘扫描波 Volume 使用默认参数
- **THEN** 边缘扫描波 pass MUST NOT 入队执行
- **AND** 当前相机颜色结果 MUST NOT 被改写

### Requirement: 边缘扫描波参数抽象
系统 MUST 使用 Volume 组件表达边缘扫描波配置，使扫描强度、扫描源、扫描方向、扇形角度、扫描半径、扫描宽度、扫描颜色、深度边缘阈值、法线边缘阈值、边缘强度、重复扫描线、前沿辉光、暗化和距离衰减由配置层控制，渲染实现只消费已经规范化的 settings。

#### Scenario: Volume 控制扫描波
- **WHEN** Volume Profile 中启用边缘扫描波且强度大于激活阈值
- **THEN** RenderPass MUST 使用 Volume 输出的规范化 settings
- **AND** 参数 MUST 在进入 shader 前被限制在安全范围内

#### Scenario: 参数钳制
- **WHEN** 输入超出范围的强度、半径、宽度、深度边缘阈值、法线边缘阈值、边缘强度、距离衰减、扇形角度、扫描线间距、扫描线宽度、扫描线强度、前沿辉光或暗化强度
- **THEN** settings MUST 将这些参数钳制到定义的安全范围内

#### Scenario: 方向规范化
- **WHEN** 输入扫描方向为非水平向量或零向量
- **THEN** settings MUST 将扫描方向规范化为水平单位向量
- **AND** 零向量 MUST 使用项目定义的默认前向方向

### Requirement: 世界空间扇形扫描
系统 MUST 使用世界空间扫描源、方向、扇形角度和扫描半径表达扩散扫描波，使效果跟随场景空间距离，而不是固定屏幕坐标或可见球体表面。

#### Scenario: 世界空间扩散
- **WHEN** 用户调整扫描半径
- **THEN** shader MUST 根据像素重建世界位置与扫描源之间的水平距离判断扫描前沿和已扫过区域
- **AND** 距离扫描半径越接近的像素 MUST 获得越强前沿响应

#### Scenario: 扫描宽度控制
- **WHEN** 用户调整扫描宽度
- **THEN** 扫描前沿厚度和暗化尾部范围 MUST 随宽度参数变化
- **AND** 扫描扇形之外的区域 MUST 保持原始画面

#### Scenario: 扇形方向控制
- **WHEN** 用户调整扫描方向或扇形角度
- **THEN** shader MUST 只在扫描源前方指定扇形内生成扫描响应
- **AND** 扇形之外的轮廓和线条 MUST 保持原始画面

### Requirement: 死亡搁浅式扫描层次
系统 MUST 在扫描扇形内合成重复世界空间扫描线、前沿白蓝辉光、受控暗化和深度/法线轮廓，使效果具有地形扫描读数感。

#### Scenario: 重复扫描线
- **WHEN** 扫描扇形覆盖不透明场景表面
- **THEN** shader MUST 依据世界空间距离生成重复扫描线
- **AND** 扫描线间距、宽度和强度 MUST 可由 Volume 参数控制

#### Scenario: 前沿辉光
- **WHEN** 像素接近当前扫描半径
- **THEN** shader MUST 叠加更亮的白蓝前沿辉光
- **AND** 前沿辉光强度 MUST 可由 Volume 参数控制

#### Scenario: 扫描暗化
- **WHEN** 像素位于扫描前沿内侧的已扫过区域
- **THEN** shader MUST 能按暗化参数压低原始画面亮度
- **AND** 暗化强度为 0 时 MUST 不改变原始画面亮度

### Requirement: 深度和法线边缘检测
系统 MUST 使用相机深度和法线纹理做屏幕空间边缘检测，使扫描波主要照亮物体轮廓、遮挡边界、地形高差和表面折角。

#### Scenario: 深度边缘响应
- **WHEN** 扫描波命中物体轮廓、遮挡断面或地形高差
- **THEN** shader MUST 通过邻域深度差异生成边缘响应
- **AND** 深度边缘阈值 MUST 能控制深度断面的敏感度

#### Scenario: 法线边缘响应
- **WHEN** 扫描波命中墙角、硬表面折角或法线变化明显的区域
- **THEN** shader MUST 通过邻域法线差异生成边缘响应
- **AND** 法线边缘阈值 MUST 能控制表面折角的敏感度

#### Scenario: 非边缘区域不常亮
- **WHEN** 扫描波命中大面积平坦且深度/法线连续的表面
- **THEN** shader MUST 避免整片表面被均匀涂亮
- **AND** 只允许保留受控的重复扫描线、前沿辉光或弱扫描底色，具体由扫描线、前沿辉光、强度和边缘强度参数决定

### Requirement: URP 输入和渲染顺序
系统 MUST 为边缘扫描波 RenderPass 声明所需的颜色、深度和法线输入，并在内置后处理之前写回当前相机颜色目标。

#### Scenario: 必要输入声明
- **WHEN** 边缘扫描波 RenderPass 创建
- **THEN** RenderPass MUST 请求 `Color`、`Depth` 和 `Normal` 输入
- **AND** shader MUST 能采样当前相机颜色、相机深度和相机法线纹理

#### Scenario: 三档 Renderer 可用
- **WHEN** 项目使用 High Fidelity、Balanced 或 Performant 任一质量档
- **THEN** 对应 URP Renderer Data MUST 包含边缘扫描波 Renderer Feature
- **AND** Renderer Feature MUST 默认不产生画面变化，直到 Volume 参数激活

### Requirement: 不依赖球体主渲染路径
系统 MUST NOT 将球体网格材质作为边缘扫描波的主视觉实现。球体或 gizmo MAY 仅作为调试可视化，用于表达扫描源和半径。

#### Scenario: 球体调试可选
- **WHEN** 用户需要观察扫描源或半径
- **THEN** 系统 MAY 提供关闭状态的调试可视化对象
- **AND** 关闭该调试对象后扫描波后处理 MUST 仍能独立工作

#### Scenario: 无球体仍可扫描
- **WHEN** 场景中不存在任何扫描球体网格
- **THEN** 边缘扫描波 MUST 仍能根据 Volume 参数和相机深度/法线生成边缘扫描效果

### Requirement: 边缘扫描波可验证性
系统 MUST 提供自动测试和手动验证步骤，确认边缘扫描波参数、Renderer 接入、深度/法线输入和手动画面效果可验证。

#### Scenario: 自动测试
- **WHEN** 运行 `ThirdPersonRendering.Tests.EdgeScanTests`
- **THEN** 测试 MUST 覆盖默认激活、强度激活、参数钳制、Volume 配置、Renderer 接入、必要输入声明和 shader 关键路径

#### Scenario: 手动验证
- **WHEN** 在 `Assets/Scenes/Sandbox.unity` 的 Volume Profile 中启用边缘扫描波并调整扫描半径
- **THEN** Game View MUST 出现从扫描源向外扩散的边缘高亮
- **AND** 高亮 MUST 主要落在轮廓、高差、墙角和遮挡边界上
- **AND** 将强度调回 0 后画面 MUST 恢复无扫描效果
