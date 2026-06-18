## MODIFIED Requirements

### Requirement: Body Channel Claim 独立于行为模块
系统 MUST 将 FullBody、UpperBody 或经批准的等价身体输出范围表达为 body/channel claim。Action、Locomotion、Aim、HitReact 或未来 UpperBodyAction 是行为模块或 source；body/channel claim 只描述请求占用范围，MUST NOT 成为 gameplay owner、behavior graph leaf、runtime source、slot owner 或 animation presentation layer。

FullBody claim MUST 表示提交方在本帧请求全身占用。该 claim 被采纳后的正式输出 MUST 是 CommittedAction / Action-side owner 接管 `BaseSlot`，并压制冲突的 `UpperBodySlot`。系统 MUST NOT 把 `FullBody` 当作 slot owner 输出。UpperBody claim MAY 表示提交方请求占用 `UpperBodySlot`，但本要求本身不实现 UpperBody runtime source。

Body claim policy MUST 是正式配置、正式校验或正式错误；系统 MUST NOT 为缺失 claim policy 引入 fallback 配置。

#### Scenario: Dodge 提交 FullBody claim
- **WHEN** Action domain 接受 `Action.Dodge`
- **THEN** Dodge source MUST 输出 FullBody claim、动作 motion candidate、动作 animation candidate 和必要的 action facts
- **AND** 身体仲裁 MUST 将该 claim 解释为 Action-side owner 对 `BaseSlot` 的接管
- **AND** 系统 MUST NOT 要求存在 `FullBody` behavior node 才能执行 Dodge

#### Scenario: Locomotion 不提交 FullBody claim
- **WHEN** Locomotion source 提交基础移动候选
- **THEN** Locomotion MUST 以 movement source 参与 `BaseSlot` 候选
- **AND** Locomotion MUST NOT 通过 FullBody claim 把自己伪装为 Action 或全身动作

#### Scenario: 缺失 claim policy
- **WHEN** 某个 source 输出了当前正式配置无法识别的 body/channel claim
- **THEN** 校验或运行时构建 MUST 报告正式错误
- **AND** 系统 MUST NOT 自动降级到默认 FullBody、默认 UpperBody 或临时 fallback claim

#### Scenario: claim 和 slot owner 不混用
- **WHEN** 测试、compiler 或 editor adapter 检查身体仲裁结果
- **THEN** 结果 MUST 记录 `BaseSlot` owner、`UpperBodySlot` owner 或批准的等价 slot owner
- **AND** 结果 MUST NOT 把 `FullBody` 当作 slot owner 名称
