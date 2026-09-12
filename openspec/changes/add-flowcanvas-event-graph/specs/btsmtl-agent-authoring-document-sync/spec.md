## ADDED Requirements

### Requirement: Character事件图必须进入同一Presentation文档闭包

Character动画事件图、可达Macro、原生变量、布局和宿主引用 MUST属于同一Character Presentation Document目标。节点、字段、逻辑端口和允许引用 MUST由正式共享能力投影；变量声明 MUST保持原生稳定身份，不保存第二份节点定义、私有序列化镜像或运行状态。

公开图种和目录扩展 MUST进入项目当时唯一正式文档版本及其严格文件清单，不提供旧包兼容读取或独立事件图协议。事件图可见语义、版本登记和依赖 MUST由相关领域及唯一事务服务共同闭合，不由另一个工具链补写。

#### Scenario: 导出动画事件图

- **WHEN** Agent checkout引用动画事件图的正式Character目标
- **THEN** 文档 MUST包含该图、变量、Macro闭包及宿主context，并保持跨Pose引用身份

#### Scenario: 文件闭包缺失

- **WHEN** 目标事件图缺少所需布局、Macro或合法变量引用
- **THEN** 文档校验 MUST报告精确文件或实体身份，不从旧包或Unity私有字段猜补

### Requirement: 事件图与消费引用必须共同通过原资产事务

事件图节点、变量及其跨Pose消费引用变更 MUST通过已有dry-run、typed Mutation、apply与反向导出流程完成。任何一步失败 MUST恢复同一批资产owner与文档状态。新增事件图 MUST不引入局部MCP写入、JSON直编、自动Build或额外保存服务。

#### Scenario: 删除仍被消费的变量

- **WHEN** 删除事件图变量但目标状态中的Pose仍引用它
- **THEN** dry-run MUST拒绝并定位引用，不能只提交事件图删除

#### Scenario: 同批修改失败

- **WHEN** 事件图及消费引用的同批Mutation在保存前失败
- **THEN** 事务 MUST恢复这批正式owner，不能保留半份作者修改
