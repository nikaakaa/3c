## ADDED Requirements

### Requirement: Action admission必须以统一顺序评估Required Tag

Action admission MUST在创建ActionInstance前使用当前Character transaction的typed Tag view评估Required Tag Query，并与Target、Block、cancel/transition eligibility、request consumption保持一个明确稳定顺序。Required失败 MUST返回结构化reason且不消费未声明消费的request，不得进入Feature graph后再自我终止。

#### Scenario: 装备Tag缺失

- **WHEN** PrimaryAction请求命中Sawblade Route但Required Tag已因authority correction撤销
- **THEN** admission MUST在ActionInstance创建前拒绝
- **AND** Feature Route body MUST不激活

#### Scenario: 同Tick装备commit后提交动作

- **WHEN** Equipment commit在同一transaction先授予Feature Tag，后续合法操作提交Action request
- **THEN** admission MUST读取候选Tag view并允许动作
- **AND** 整个Tick失败时两者 MUST一起回滚

### Requirement: Equipment Route选择不得进入Action runtime

Action runtime MUST继续只处理Action catalog、admission、instance和lifecycle。Slot/Feature/Route entry选择 MUST由compiled Equipment Host处理；Graph、Timeline和equipment change执行 MUST留在各自operation模块。系统 MUST不恢复ActionModule、AbilityBody或ActionId到Graph callback registry。

#### Scenario: Route已选择Attack ActionProfile

- **WHEN** Equipment Host解析出Sawblade PrimaryAction entry
- **THEN** Action runtime MUST只接收正式Action activation request和Equipment Context
- **AND** MUST不持有Sawblade Feature对象

#### Scenario: Action被取消

- **WHEN** Dodge通过正式cancel规则结束Feature Action
- **THEN** Action lifecycle MUST发布取消结果
- **AND** compiled control lifecycle MUST按该结果abort Route body

