## ADDED Requirements

### Requirement: Semantic IR必须表达Character composition roots

Validated Semantic IR MUST包含canonical root catalog，区分Character Root、Equipment Persistent与Equipment Route root，并保存root identity、serialized owner、FeatureId、RouteId、entry operation和source map。全部root MUST共享同一Operation Set、Graph identity规则、control topology验证和Value port contract。Semantic IR MUST不保存Unity Graph对象或为Feature建立第二种flow IR。

#### Scenario: 编译多个Feature root

- **WHEN** Equipment Profile包含Sawblade与Gun Feature
- **THEN** Semantic IR MUST按稳定identity包含二者的Persistent/Route roots
- **AND** 所有entry MUST指向同一validated operation table

#### Scenario: Feature使用非法控制边

- **WHEN** Feature graph包含RootTree同样不允许的control cycle
- **THEN** 共享Semantic validation MUST拒绝
- **AND** MUST不由Equipment compiler放宽规则

### Requirement: Semantic IR必须使用numeric-neutral Equipment schema

Semantic IR MUST表达Slot、Route、Equipment、Feature、Parameter schema/value、Initial Loadout、Presentation requirement、Action binding、Tag/Effect contribution、local state declaration、equipment operation与capability union。Scalar/Vector/Yaw值 MUST使用numeric-neutral canonical representation，并由Target lowering选择具体ABI。IR MUST不包含Float32 runtime类型、Unity asset引用、Network Model或visual instance。

#### Scenario: 同一Corin源生成双Target

- **WHEN** Float32与Fixed Compiler消费同一validated Semantic IR
- **THEN** 两者 MUST解析相同Equipment/Feature/Route业务identity
- **AND** numeric value MUST分别降低到目标类型

#### Scenario: Target不支持Equipment operation

- **WHEN** Fixed Target operation manifest缺少Feature实际使用的operation
- **THEN** Target compile MUST拒绝整个Program
- **AND** MUST不从IR删除该Feature或operation

### Requirement: Equipment operation必须进入版本化Operation Set

Operation Set MUST为Equipment identity/parameter read、change begin/commit/cancel、host entry/exit与route resolution声明稳定opcode、typed ports、state requirement、reference kind和failure result。Frontend、Float32与Fixed backend MUST使用同一semantic contract；新增或改变contract MUST提升Operation Set版本，MUST不通过字符串operation、反射或Feature回调扩展。

#### Scenario: ReadEquipmentParameter端口

- **WHEN** Graph读取Scalar参数
- **THEN** Frontend MUST验证Context/Slot input、Parameter reference和Scalar output port
- **AND** IR MUST保存稳定ParameterId而不是显示名

#### Scenario: 未知Equipment opcode

- **WHEN** Program target遇到未登记Equipment opcode
- **THEN** compile/load MUST明确失败
- **AND** runtime MUST不将其视为成功no-op

