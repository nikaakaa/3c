## ADDED Requirements

### Requirement: 动画事件宿主必须按正式输入需求装配

显式绑定事件图的动画实例 MUST由唯一宿主在本次Fact准备后、Pose推进前执行一次合法事件调用，并向需要变量的消费者发布完整只读结果。没有绑定图且没有变量或事件需求时，实例 MUST沿同一正式Fact/Pose链运行，不创建占位图、宿主或虚构变量结果；存在必需变量而缺失生产者时 MUST失败，不切换为默认补值。

事实、动画变量、子图输入、曲线与节点配置 MUST保持各自正式来源。依赖发现和变量消费绑定 MUST属于既有编译或实例装配，运行帧只传递本次实际输入。Pose根事务、Action、source、混合、Foot、FBBIK、Evaluate、Seal和最终Writer MUST继续由现有唯一链路承担，事件图 MUST不接管这些引擎职责。

#### Scenario: 无作者变量需求

- **WHEN** 角色未绑定事件图且Pose仅使用正式Fact、曲线与配置
- **THEN** 正式动画运行与完整Preview MUST使用同一无事件需求合同
- **AND** MUST不通过空EventGraph或伪造变量帧满足非空检查

#### Scenario: 已装配事件图正常更新

- **WHEN** 正式Fact到达且事件调用成功
- **THEN** 同次Pose MUST读取本次实际需要的只读变量
- **AND** 事件更新、Pose求值和最终写入各自只执行所属工作一次

#### Scenario: 事件输入失败

- **WHEN** 已需要的事件调用发生节点错误或输出非法
- **THEN** 部分变量 MUST不发布，Pose MUST不以旧值或默认值继续
- **AND** MUST保持既有实例Fault/Reset规则，不能回退到无事件需求模式
