# unity-simulation-assembly-ownership Specification

## Purpose
定义公共 Unity Simulation、具体 Network Model、可选 WorldSolver、客户端 Runtime 与 Editor 之间的程序集所有权和单向依赖。
## Requirements
### Requirement: Unity Simulation公共基座必须由独立程序集拥有

系统 MUST以独立`ThirdPersonSimulation.Unity`程序集唯一拥有model-neutral Session Composition Definition、Float32 Unity request lowering、标准Local Pipeline authoring、Actor registration合同与通用roster output/diagnostics aggregate。该程序集 MUST不引用具体Network Model、Fantasy、Character Presentation、Animancer、Camera或可选DotRecast Unity实现，也 MUST不通过`Assembly-CSharp`、friend assembly、反射或字符串registry取得这些实现。Timeline Authoring Preview MUST由Character Presentation程序集拥有，并且 MUST不创建或依赖Preview Simulation Composition。

#### Scenario: 编译公共Unity Simulation程序集

- **WHEN** 项目编译`ThirdPersonSimulation.Unity`
- **THEN** 其直接和传递业务依赖 MUST只包含portable Simulation、Unity model-neutral adapter合同与明确允许的Unity API
- **AND** 删除ServerAuthoritative、Fantasy和DotRecast Unity程序集后，公共程序集 MUST仍可独立编译

### Requirement: Network Model Unity实现必须与model-neutral Definition分程序集

系统 MUST以`ThirdPersonGameplay.NetworkModel.Unity`唯一拥有model-neutral Network Model Definition、Source requirement与preparation validation，并以模型自己的Unity程序集拥有Model Definition、Pipeline/Pass Definition、Endpoint、Source preparation和Scene/Presentation adapter。新增模型 MUST只新增或修改自己的模型程序集，MUST不修改`ThirdPersonSimulation.Unity`或model-neutral Network Model程序集。

#### Scenario: 安装新的Float32 Network Model

- **WHEN** 新模型提供自己的Unity adapter程序集、Source、Pipeline Runtime Package与Runtime Launcher
- **THEN** 公共Unity Simulation和model-neutral Network Model程序集 MUST无需修改
- **AND** 编译依赖 MUST从模型程序集单向指向公共程序集

### Requirement: 可选WorldSolver Unity adapter必须独立拥有具体实现

DotRecast Unity asset、state-only binding与WorldSolver Definition MUST由`ThirdPersonSimulation.DotRecast.Unity`唯一拥有。`ThirdPersonSimulation.Unity` MAY声明通用`Float32WorldSolverDefinition`合同并拥有Unity CharacterController对照实现，但 MUST不引用DotRecast具体类型、artifact wrapper或query实现。

#### Scenario: 仅安装Unity CharacterController组合

- **WHEN** Unity Client只选择CharacterController Solver组合
- **THEN** 公共Composition MUST不需要加载DotRecast Unity程序集类型
- **AND** Session仍 MUST通过同一显式WorldSolver Definition插槽创建唯一Solver

### Requirement: 客户端Runtime与Editor程序集必须保持单向依赖

Character、Camera、Animation、Presentation和Scene Host MUST进入明确客户端Runtime程序集；Editor authoring、compiler、Inspector与Agent工具 MUST进入明确Editor程序集且不得进入Player。Editor程序集 MAY引用Runtime程序集，Runtime程序集 MUST不引用Editor程序集、Editor API或模型Editor工具。

#### Scenario: 编译Player运行程序集

- **WHEN** Unity编译Player运行程序集
- **THEN** Action、Behavior、Pipeline和Simulation Editor代码 MUST不进入运行程序集
- **AND** Runtime程序集 MUST不依靠特殊Editor目录或`Assembly-CSharp-Editor`提供业务类型

### Requirement: 程序集迁移必须保持唯一序列化身份

脚本程序集迁移 MUST保留原`.meta` GUID、namespace、类型名和序列化字段。系统 MUST不保留旧assembly空壳、重复类型、`MovedFrom`兼容链、一次性runtime migrator或双份ScriptableObject。若受影响类型存在无法安全迁移的managed-reference assembly typename，实施 MUST停止并报告缺口。

#### Scenario: 迁移现有Composition资产

- **WHEN** Composition、Pipeline、Model、Endpoint、Solver或Scene组件脚本进入新程序集
- **THEN** 现有资产 MUST继续通过原MonoScript GUID引用唯一类型
- **AND** ProgramHash、PipelineHash、Composition identity与Model identity MUST不因程序集迁移改变
