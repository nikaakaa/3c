## MODIFIED Requirements

### Requirement: 公共Unity Composition必须由程序集依赖强制模型无关

公共 Unity Session Composition、Float32 request lowering 与标准 Local authoring MUST位于不引用具体 Network Model 的独立 Unity 程序集。Character Host 与模型 adapter MUST单向引用公共程序集，不得通过预定义程序集、friend assembly、反射、字符串查找或补充 registry 绕过依赖方向。

独立预览场景 MUST经同一公共 Composition 入口和正式 Host 建立真实 Session，明确选择已经存在的 Source、Pipeline、Backend、Solver 与 Actor registration；预览编辑器 MUST只管理启动请求和公开输入/控制端口。系统 MUST不新增 Preview 专用 Composer、Kernel、Pipeline 或模型推断，公共 Composition MUST不反向依赖作者窗口或表现实现。

#### Scenario: ServerAuthoritative Unity adapter被移除

- **WHEN** 构建不包含 ServerAuthoritative Unity 程序集
- **THEN** Local Composition MUST仍可编译并创建正式 Session
- **AND** 公共 Composer MUST不包含该模型类型或分支
- **AND** 明确使用 Local 配置的预览场景 MUST通过同一公共入口运行
