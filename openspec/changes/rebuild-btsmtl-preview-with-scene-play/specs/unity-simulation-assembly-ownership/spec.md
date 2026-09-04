## MODIFIED Requirements

### Requirement: Unity Simulation公共基座必须由独立程序集拥有

系统 MUST以独立公共 Unity Simulation 程序集唯一拥有 model-neutral Session Composition Definition、Float32 Unity request lowering、标准 Local Pipeline authoring、Actor registration 合同和通用 roster output/diagnostics aggregate。该程序集 MUST不引用具体 Network Model、Fantasy、Character Presentation、Animancer、Camera、可选 DotRecast 实现或 Editor 预览工具，也不得通过预定义程序集、friend assembly、反射或字符串 registry 获取这些实现。

场景预览的 Editor 编排 MUST单向依赖正式公共 Composition 和客户端公开运行端口；预览场景的上下文声明 MUST留在客户端 Unity 边界。真实 Session 与角色表现 MUST继续由各自正式程序集拥有，不新增 Preview Simulation Composition 或把角色执行放入 Editor。

#### Scenario: 编译公共Unity Simulation程序集

- **WHEN** 项目编译公共 Unity Simulation 程序集
- **THEN** 其业务依赖 MUST只包含 portable Simulation、model-neutral adapter 合同与允许的 Unity API
- **AND** 删除 ServerAuthoritative、Fantasy、DotRecast 或 Editor 预览程序集后，公共程序集 MUST仍可独立编译
