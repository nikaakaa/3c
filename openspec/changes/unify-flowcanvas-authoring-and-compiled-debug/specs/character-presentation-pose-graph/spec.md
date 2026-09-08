## ADDED Requirements

### Requirement: Pose作者图必须直接编译且独立于编辑器执行

Pose正式作者节点、严格空间端口及子图接口 MUST采用统一原生作者图合同，直接编入既有Pose编译阶段和不可变运行产物。现有拓扑、Source所有权、同帧求值、Worker、Frame事务及唯一最终输出要求 MUST保持。MUST不建立旧作者图镜像，不通过作者端口委托执行Pose，也不因编辑器观察额外采样或输出姿态。

#### Scenario: 观察正在求值的Pose子图
- **WHEN** 作者进入指定角色的Pose子图运行观察
- **THEN** 编辑器 MUST按编译来源和调用路径显示真实求值状态
- **AND** 正式姿态计算与最终输出次数 MUST不因观察增加
