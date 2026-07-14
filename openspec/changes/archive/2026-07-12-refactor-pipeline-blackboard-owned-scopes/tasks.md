## 1. Declaration 模型

- [x] 1.1 盘点 `BaseExposedProperty` 当前序列化字段、GUID 生成和克隆路径
- [x] 1.2 将 declaration GUID 明确暴露为 Pipeline Blackboard declaration identity
- [x] 1.3 将 `BlackboardKey` 约束为 declaration owner 内唯一作者键
- [x] 1.4 新增 `GraphInstance` lifetime
- [x] 1.5 将 `DebugCategory` 模型迁移为层级 `CategoryPath`
- [x] 1.6 定义 Character scope 合法 lifetime 组合
- [x] 1.7 定义 Graph scope 合法 lifetime 组合
- [x] 1.8 定义 State scope 合法 lifetime 组合
- [x] 1.9 定义 ActionInstance scope 合法 lifetime 组合
- [x] 1.10 定义 Frame scope 合法 lifetime 组合
- [x] 1.11 让 `Config` lifetime 在 pipeline runtime 中保持只读
- [x] 1.12 删除非法 scope/lifetime 的宽松接受路径

## 2. Variable Reference

- [x] 2.1 定义包含 declaration identity 与声明 owner 的正式 variable reference
- [x] 2.2 让 variable reference 支持 RootTree Character declaration
- [x] 2.3 让 variable reference 支持当前 Graph local declaration
- [x] 2.4 让 variable reference 支持上层可见 declaration
- [x] 2.5 让 variable reference 在 declaration 重命名后保持绑定
- [x] 2.6 让断裂 declaration reference 产生结构校验错误
- [x] 2.7 让不可见 declaration reference 产生结构校验错误
- [x] 2.8 让类型不匹配 declaration reference 产生结构校验错误
- [x] 2.9 删除 pipeline variable node 的裸 key 绑定入口
- [x] 2.10 删除按名称或最近声明自动 shadow 的解析路径

## 3. Runtime Address 与存储

- [x] 3.1 定义统一 Pipeline Blackboard runtime address 值类型
- [x] 3.2 实现 Character runtime owner identity
- [x] 3.3 实现 Graph runtime instance owner identity
- [x] 3.4 实现 StateMachine execution owner identity
- [x] 3.5 实现 ActionInstance owner identity
- [x] 3.6 实现 local logic tick owner identity
- [x] 3.7 将 declaration registry 从裸 key 迁移到 declaration identity
- [x] 3.8 将 value store 从裸 key 迁移到结构化 runtime address
- [x] 3.9 让同一 key 的不同局部 declaration 可同时注册
- [x] 3.10 让同一 shared graph 的不同 runtime instance 值互相隔离
- [x] 3.11 让 runtime read 通过 declaration reference 和 access context 解析 address
- [x] 3.12 让 runtime write 通过 declaration reference 和 access context 解析 address
- [x] 3.13 让缺失 owner context 的读取直接失败并报告错误
- [x] 3.14 让缺失 owner context 的写入直接失败并报告错误
- [x] 3.15 更新 runtime debug entry 以显示 declaration、scope owner 和 resolved address
- [x] 3.16 删除旧 `Dictionary<string, declaration/value>` 主路径

## 4. Graph 与生命周期上下文

- [x] 4.1 让每个 BaseGraph 只注册自己拥有的 declaration
- [x] 4.2 让 Graph 工作副本初始化正式 Graph runtime identity
- [x] 4.3 让 Graph 工作副本销毁时只清理自己的 GraphInstance bucket
- [x] 4.4 扩展 blackboard access context 以携带当前 Graph runtime identity
- [x] 4.5 扩展 State enter 通知以传递完整 `StateMachineExecutionScope`
- [x] 4.6 扩展 State exit 通知以传递完整 `StateMachineExecutionScope`
- [x] 4.7 进入 State activation 时初始化目标 State bucket
- [x] 4.8 退出 State activation 时只清理目标 State bucket
- [x] 4.9 保证并行 Locomotion 与 Action StateMachine 的 State bucket 隔离
- [x] 4.10 保证同一 State 重入时按 activation generation 隔离
- [x] 4.11 将 ActionInstance variable 绑定到当前 `ActionInstanceId`
- [x] 4.12 将 action terminal/clear 改为只清理目标 ActionInstance bucket
- [x] 4.13 将 Frame variable 绑定到 local logic tick
- [x] 4.14 在每个 local logic tick 边界只清理过期 Frame bucket
- [x] 4.15 让 ConditionRuleGraph evaluation 继承当前 Graph 与 active State access context
- [x] 4.16 删除只传 `stateId` 的旧 blackboard lifecycle 接口
- [x] 4.17 删除扫描全部 Action scope value 的旧清理实现

## 5. 节点与规则图访问

- [x] 5.1 将 `ExposedPropertyNode` 迁移到正式 variable reference
- [x] 5.2 让 `ExposedPropertyNode Get` 使用统一 runtime resolver
- [x] 5.3 让 `ExposedPropertyNode Set` 使用统一 runtime resolver
- [x] 5.4 将 ConditionRuleGraph blackboard ValueNode 迁移到正式 variable reference
- [x] 5.5 保持 ConditionRuleGraph blackboard 节点为纯 ValueNode
- [x] 5.6 阻止 ConditionRuleGraph 创建 blackboard setter
- [x] 5.7 让规则图读取缺失 declaration 时本次求值失败
- [x] 5.8 让规则图读取类型错误时本次求值失败
- [x] 5.9 删除规则图读取失败后写入零值或空值的 fallback
- [x] 5.10 删除 pipeline runtime 中直接读取 authoring exposed value 的 fallback

## 6. Authoring UI

- [x] 6.1 重构 Tree Inspector 的 exposed property source 合同以支持当前 owner 与可见 declarations
- [x] 6.2 让 Character declaration 创建入口写入 RootTree
- [x] 6.3 让 Graph declaration 创建入口写入当前 Graph
- [x] 6.4 让 State declaration 只在具有 State owner context 的 Graph 中可创建
- [x] 6.5 让 ActionInstance declaration 只在具有 Action context 的 Graph 中可创建
- [x] 6.6 让 Frame declaration 使用当前 Graph owner 保存
- [x] 6.7 增加 `All / Character / Graph / State / Action / Frame` scope 筛选
- [x] 6.8 增加 `Current Context / All Visible` 上下文筛选
- [x] 6.9 按 `CategoryPath` 构建层级 foldout
- [x] 6.10 增加 key、display name、类型和 owner 搜索
- [x] 6.11 为每个条目显示 `Local` 或 `Inherited`
- [x] 6.12 为每个条目显示 declaration owner
- [x] 6.13 在创建菜单中只显示当前上下文合法的 scope/lifetime 组合
- [x] 6.14 在 Graph tab 中保持 Pipeline Blackboard 面板可访问
- [x] 6.15 在 Transition selection 中保持 Pipeline Blackboard 面板可访问
- [x] 6.16 为 blackboard ValueNode 提供按可见 declaration 的类型化 picker
- [x] 6.17 拖拽 inherited declaration 时只创建 reference
- [x] 6.18 删除 inherited declaration 拖拽时复制 declaration 的路径
- [x] 6.19 将变量面板文案从 `Exposed Property` 收敛为 `Pipeline Blackboard`

## 7. 校验与资产迁移

- [x] 7.1 增加同一 declaration owner 内重复 key 校验
- [x] 7.2 增加重复 declaration identity 校验
- [x] 7.3 增加 scope/lifetime 非法组合校验
- [x] 7.4 增加 declaration owner 与 scope 不匹配校验
- [x] 7.5 增加 variable reference 可见性校验
- [x] 7.6 增加 variable reference 类型校验
- [x] 7.7 增加缺失 State/Action/Frame runtime owner context 校验
- [x] 7.8 等待 `fix-corin-action-lifecycle-and-dodge-interruption` 完成后读取最终 Corin RootTree
- [x] 7.9 保留 Corin RootTree 的 Character 配置阈值 declarations
- [x] 7.10 保留 Corin RootTree 的 Character `IsDodging` declaration
- [x] 7.11 将 DodgeForward body 的 `IsDodging` 节点重绑到 RootTree declaration
- [x] 7.12 删除 DodgeForward body 的重复 `IsDodging` declaration
- [x] 7.13 将 DodgeBack body 的 `IsDodging` 节点重绑到 RootTree declaration
- [x] 7.14 删除 DodgeBack body 的重复 `IsDodging` declaration
- [x] 7.15 扫描角色 pipeline 资产中的裸 key blackboard node
- [x] 7.16 迁移所有可解析的裸 key node 为显式 reference
- [x] 7.17 对不可唯一解析的裸 key node 产生迁移错误
- [x] 7.18 扫描并清理跨 Graph 重复 declaration
- [x] 7.19 扫描并修复非法 scope/lifetime 组合
- [x] 7.20 删除旧 `DebugCategory` 序列化字段与读取路径
- [x] 7.21 删除旧变量 source override 与兼容 resolver

## 8. 文档与工具校验

- [x] 8.1 更新 `openspec/project.md` 的 Pipeline Blackboard 架构口径
- [x] 8.2 使用禁用 build server 的参数编译受影响 C# 工程
- [x] 8.3 编译结束后立即执行 `dotnet build-server shutdown`
- [x] 8.4 运行 `openspec validate refactor-pipeline-blackboard-owned-scopes --strict --no-interactive`
- [x] 8.5 运行 `openspec validate --all --strict --no-interactive`
