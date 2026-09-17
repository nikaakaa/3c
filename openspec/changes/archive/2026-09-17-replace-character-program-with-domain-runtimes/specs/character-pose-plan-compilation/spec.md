## ADDED Requirements

### Requirement: 原生Pose节点定义必须只有一个业务真相

Pose 节点的字段、默认值、端口、局部规则、资源引用和子图依赖 MUST由同一领域定义提供给原生运行、作者 UI 和 C# API。系统 MUST不复制编译节点定义、payload DTO 或按不同入口维护规则。运行缓存和缓冲位置 MUST不进入作者字段。

#### Scenario: 编辑并执行同一种节点
- **WHEN** UI 或 C# API 配置一个合法 Pose 节点
- **THEN** 原生运行实例 MUST消费相同字段和端口定义，不能使用另一套默认值或编译镜像


### Requirement: 原生Pose校验必须保留全局约束

显式图校验与实例绑定 MUST检查可达图引用、递归、悬空引用、typed 空间、唯一输出、目标槽重复、唯一 Goal Set／FBBIK、写冲突和实际绑定容量。局部节点规则 MUST不扫描整图；全局校验 MUST不生成 IR、operation plan 或持久化工作区计划。运行热路径 MUST不重复完整静态扫描。

#### Scenario: 子图形成递归
- **WHEN** 根图、状态子图或 Linked Pose 引用形成递归
- **THEN** 校验 MUST定位完整调用链并拒绝绑定，不得在运行时按最大深度截断

#### Scenario: 图类型合法但目标写入冲突
- **WHEN** 两个局部合法节点声明冲突目标或多个最终输出
- **THEN** 全局校验 MUST报告冲突来源，不能交给运行顺序决定结果


### Requirement: 原生Pose错误必须定位作者来源

静态与运行错误 MUST提供稳定图、节点、端口或调用实例身份，以及发生阶段和原因。运行观察 MUST直接关联原生图已完成结果，不为恢复旧 SourceMap 编译隐藏程序。

#### Scenario: 已绑定节点求值失败
- **WHEN** 原生节点因资源或 Constraint 输入失败
- **THEN** 诊断 MUST定位实际作者节点与本次调用，不显示另一个不可见编译操作作为唯一来源


### Requirement: 旧Pose编译产物必须退出正式入口

正式角色、预览、资源绑定和作者工具 MUST不再要求、创建或读取旧 Pose IR／ProgramImage。迁移 MUST先删除IR／Image及专属生成／执行链与旧生成产物，再接回保留消费者；不能等原生节点全部实现才退出旧链。原生图与资源版本 MUST被明确检查，未接完整的中间状态 MUST明确失败，不能读取旧产物作为回退。

#### Scenario: 只有旧PoseImage存在
- **WHEN** 角色缺少合法原生图但磁盘上仍有旧 Image
- **THEN** 实例准备 MUST报告缺失图，不能读取旧 Image 继续运行
