## ADDED Requirements

### Requirement: 公共Unity Composition必须由程序集依赖强制模型无关

公共Unity Session Composition、Float32 request lowering与标准Local/Preview authoring MUST位于不引用具体Network Model程序集的独立Unity程序集。Character Host和模型Unity adapter只能单向引用该公共程序集；它们 MUST不通过预定义程序集、friend assembly、反射、字符串类型查找或fallback registry绕过依赖方向。

#### Scenario: ServerAuthoritative Unity adapter被移除

- **WHEN** 构建中不包含ServerAuthoritative Unity程序集
- **THEN** Local与Preview Composition程序集 MUST仍可编译并创建正式Session
- **AND** 公共Composer源码 MUST不包含ServerAuthoritative类型或分支
