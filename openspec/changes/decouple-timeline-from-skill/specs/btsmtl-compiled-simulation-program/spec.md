## MODIFIED Requirements

### Requirement: Character authoring 必须按显式 Numeric Target 生成 Simulation Program

系统 MUST以 CharacterPipelineDefinition 为角色产品唯一组合根，经 validated Gameplay Semantic IR artifact 和显式 Numeric Target 生成 CharacterSimulationProgram。Timeline 共用内容 MUST由相同发射和目标降低产生只读单元，再按角色组合链接技能和外部绑定；独立 Timeline 产物根不得改变角色组合入口。Target Compiler MUST只接收 validated artifact，不直接接收 Definition、Graph、Node、Timeline、Unity object 或 Frontend 私有对象。每个 target artifact MUST只有一个 NumericProfile，Runtime 不得从作者对象或 IR 创建 gameplay clone。

#### Scenario: 编译 Corin Float32 Program

- **WHEN** 构建 Corin CharacterPipelineDefinition
- **THEN** Frontend MUST先发布 validated artifact，Float32 Target 只从该产物降低角色与技能内容
- **AND** Runtime MUST不 clone 作者图或 Timeline

#### Scenario: Target 收到未校验的内存 IR

- **WHEN** 调用方绕过 artifact codec 将内存 IR 交给 Target
- **THEN** 正式入口 MUST拒绝该输入，不能因为来自当前 Editor 就视为有效

## ADDED Requirements

### Requirement: Timeline 内容单元必须保持定义与调用方绑定分离

只读 Timeline 单元 MUST包含自身时间、操作、常量、局部状态布局、绑定签名、完整依赖和来源。它 MUST不嵌入某个 Actor/ActionInstance 的可变地址或场景对象；Skill 与独立调用 MUST分别将同一声明链接到各自状态与领域能力。Character 使用的 Timeline 状态 MUST继续进入角色正式布局、codec、snapshot 和 hash，不得保存第二份独立播放状态。

#### Scenario: 同一内容进入两个角色组合

- **WHEN** 两个合法角色组合引用相同 Timeline 内容
- **THEN** 单元内部定义索引 MUST保持一致，外部角色绑定分别校验
- **AND** 每次释放的进度和子树状态 MUST仍属于对应角色事务

### Requirement: 独立 Timeline 产物必须通过明确构建与完整校验发布

独立构建 MUST显式接收 Timeline 资产根、Numeric Target 与精确输出位置，并使用同一正式 artifact/store 基础发布明确 Timeline 根类型的产物。产物 MUST记录内容/绑定/执行模块/状态/Target 版本和完整依赖；与其必需资源映射 MUST组成原子发布组。无 Character 表现依赖时不得生成空 Character Projection。运行准备 MUST严格拒绝过期、未知能力和版本失配，MUST不运行时补编译。

#### Scenario: 发布无角色依赖的 Timeline

- **WHEN** 独立内容只需要场景表现参数能力
- **THEN** 发布组 MUST只包含其真实必要内容和资源映射
- **AND** MUST不要求空角色包、动作策略或角色动画 Projection

#### Scenario: 发布组不完整

- **WHEN** 新 Timeline 内容和输出能力版本不匹配
- **THEN** 准备 MUST失败，不能单独替换一个依赖继续播放
