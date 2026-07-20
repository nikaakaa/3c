## ADDED Requirements

### Requirement: Network Model插件边界必须具有物理程序集所有权

model-neutral Network Model Definition与具体Model Unity实现 MUST位于不同程序集。具体Model程序集 MAY引用公共Simulation、model-neutral Definition、自己的portable Model与Transport程序集以及所需Host adapter，但公共Simulation、model-neutral Definition、Program、Kernel和WorldSolver合同程序集 MUST不反向引用具体Model程序集。

#### Scenario: 增加第二个Unity Network Model

- **WHEN** 第二个Network Model提供自己的Endpoint、Source、Pipeline与Runtime Launcher
- **THEN** 它 MUST以独立模型程序集接入现有公共Composition
- **AND** MUST不修改或重新编译公共程序集源码来登记模型类型
