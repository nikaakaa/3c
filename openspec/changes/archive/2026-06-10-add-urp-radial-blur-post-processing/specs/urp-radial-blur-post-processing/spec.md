## ADDED Requirements
### Requirement: URP 径向模糊后处理接入
系统 MUST 通过当前 URP Renderer Feature 链路接入径向模糊后处理，并且 MUST NOT 使用 `OnRenderImage`、额外相机叠加或独立渲染路径绕过当前 URP 系统。

#### Scenario: Renderer Feature 接入当前 URP
- **WHEN** 项目启用径向模糊后处理
- **THEN** 径向模糊 MUST 作为 `ScriptableRendererFeature` 挂接到当前使用的 URP Renderer Data
- **AND** 渲染执行 MUST 通过 `ScriptableRenderPass` 完成
- **AND** 系统 MUST NOT 新增相机脚本作为径向模糊的主要渲染出口

### Requirement: 径向模糊参数抽象
系统 MUST 使用 Volume 组件表达径向模糊配置，使强度、中心点、半径和采样次数由配置层控制，渲染实现只消费已经规范化的参数。

#### Scenario: Volume 控制径向模糊
- **WHEN** Volume Profile 中启用径向模糊且强度大于激活阈值
- **THEN** 径向模糊 pass MUST 使用 Volume 中的强度、中心点、半径和采样次数
- **AND** 参数 MUST 在进入 shader 前被限制在安全范围内

#### Scenario: 默认参数不改变画面
- **WHEN** Volume Profile 未启用径向模糊或强度为 0
- **THEN** 径向模糊 pass MUST NOT 改写当前相机颜色结果

### Requirement: 径向模糊 shader 行为
系统 MUST 使用全屏 shader 沿屏幕径向方向采样当前相机颜色，并根据强度混合原始颜色和模糊结果。

#### Scenario: 从中心产生拖影
- **WHEN** 径向模糊中心为屏幕中心且强度大于 0
- **THEN** shader MUST 以屏幕中心为焦点沿 UV 径向方向累积采样
- **AND** 越远离中心的像素 MUST 呈现更明显的拖影趋势

#### Scenario: 采样次数受控
- **WHEN** 设计者配置采样次数
- **THEN** shader MUST 使用 C# 侧传入的受限采样次数
- **AND** 实现 MUST 避免无限循环或不可控采样数量

### Requirement: 径向模糊测试覆盖
系统 MUST 为径向模糊后处理提供 EditMode 测试，覆盖参数激活判定、安全范围和 Renderer Feature 配置行为。

#### Scenario: 自动测试验证配置逻辑
- **WHEN** 运行径向模糊 EditMode 测试
- **THEN** 测试 MUST 验证默认参数不激活
- **AND** 测试 MUST 验证有效强度激活
- **AND** 测试 MUST 验证半径、强度和采样次数的安全范围

#### Scenario: 手动验证画面效果
- **WHEN** 用户在 Unity Editor 中启用径向模糊 Volume 参数
- **THEN** 用户 MUST 能通过调节强度、中心点和采样次数观察到画面变化
- **AND** 用户 MUST 能将强度调回 0 以恢复无径向模糊画面
