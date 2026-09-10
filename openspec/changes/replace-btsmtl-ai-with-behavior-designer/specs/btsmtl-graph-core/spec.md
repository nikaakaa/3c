## MODIFIED Requirements

### Requirement: Graph节点兼容性必须由稳定Authoring Capability裁决

每个可进入受限项目 Graph 的节点类型 MUST声明稳定 authoring capability。Graph Role MUST通过唯一 policy 定义允许能力；搜索、拖拽、粘贴、脚本创建与 Compiler Validator MUST复用该 policy，并为 Agent 作者入口提供同一只读查询。系统 MUST不按路径字符串、显示名、继承层次或窗口类型猜测兼容性。已退出的 AIControllerTree Role、AI 专用节点和注册 MUST删除；插件 AI 图不进入项目 Graph Role 或被转换为项目节点。

#### Scenario: 受限Graph创建不兼容节点

- **WHEN** 任一作者或编译入口尝试在受限 Graph 中使用未允许的执行能力
- **THEN** 统一 policy MUST拒绝该节点
- **AND** 正式图数据 MUST不发生部分修改

#### Scenario: 已退役AI图尝试进入BTSMTL

- **WHEN** 搜索、粘贴、脚本或Compiler尝试打开旧AIControllerTree或创建旧AI节点
- **THEN** 统一Graph policy MUST拒绝该图和节点
- **AND** Graph数据 MUST不发生修改

#### Scenario: Behavior Designer图不注册为BTSMTL Graph

- **WHEN** 作者从BTSMTL Graph入口选择Behavior Designer行为资源
- **THEN** 入口 MUST明确说明该资源由插件编辑器拥有
- **AND** BTSMTL MUST不为其创建Graph role、节点或编译镜像

#### Scenario: 退役AI节点缺少能力声明

- **WHEN** 未声明authoring capability的旧AI节点尝试进入任一BTSMTL Graph
- **THEN** 创建与发布 MUST失败并报告节点类型和Graph Role
- **AND** 系统 MUST不按默认Base节点处理

#### Scenario: 正式Graph创建共享纯值节点

- **WHEN** 作者在允许 SharedPureValue 的正式 Graph 中创建已登记纯值节点
- **THEN** Editor 与 Compiler MUST使用同一 capability identity 并允许该操作

#### Scenario: 旧AI图请求进入作者入口

- **WHEN** 旧 AIControllerTree 或 AI 专用节点请求创建、打开为可编辑内容或发布
- **THEN** 正式入口 MUST拒绝已删除的领域能力
- **AND** MUST不把它当作普通 Base 节点处理

#### Scenario: AI Graph创建Character动作节点

- **WHEN** 旧 AIControllerTree 标识尝试通过项目作者入口创建 Character 执行节点
- **THEN** 入口 MUST首先拒绝已退役的 AI Graph Role
- **AND** MUST不创建部分节点或转到技能 Graph

#### Scenario: AI Graph创建共享纯值节点

- **WHEN** 旧 AI Graph 请求创建原来允许的 SharedPureValue 节点
- **THEN** 入口 MUST拒绝已退役的 Graph Role
- **AND** 纯值节点在其它合法 Graph 中的能力 MUST保持

#### Scenario: AI节点缺少能力声明

- **WHEN** 旧 AI 节点或未登记节点请求进入项目 Graph
- **THEN** 创建和发布 MUST因领域已删除或能力未登记而失败
- **AND** MUST不按默认 Base 节点解释
