## MODIFIED Requirements

### Requirement: Character Authoring 必须先编译为 Numeric-Neutral Semantic IR

Compiler Frontend MUST以精确Character Definition为组合根，发现控制模块合同、技能定义及可达Tree、Timeline、TreeClip、局部状态机、参数、ActionProfile、GE、Equipment和Motion数据，形成唯一numeric-neutral Semantic IR。角色控制代码只进入稳定模块binding、版本、参数和状态合同，不得重新发射为角色RootTree。IR MUST保存技能执行根、调用关系、精确数值来源、typed状态及producer，不得包含Unity对象、Target运行值、代码实例、网络模型或可变运行状态。Target MUST消费经过canonical重读、header和SemanticHash校验的artifact，不重新遍历作者资产。

#### Scenario: 编译角色与技能组合

- **WHEN** 同一角色配置请求Float32和Fixed构建
- **THEN** 两个Target MUST消费同一validated语义artifact
- **AND** artifact MUST同时锁定控制合同与技能依赖，不包含可执行角色RootTree

#### Scenario: 发现无效子图

- **WHEN** Discovery发现缺失owner、重复identity、子图递归或签名不匹配
- **THEN** MUST在发布前失败并报告精确来源
- **AND** MUST不跳过无效技能、使用旧cache或由Target补齐

#### Scenario: 编译 Corin Semantic IR Artifact

- **WHEN** Compiler Frontend 读取 Corin CharacterPipelineDefinition
- **THEN** MUST只生成一份与 Numeric Target 无关且可 canonical 读取的 Semantic IR artifact
- **AND** Float32 与 FixedQ32.32 Target MUST消费该 artifact，不得重新遍历节点生成另一套业务规则

#### Scenario: Frontend Discovery 失败

- **WHEN** 可达 authoring 存在重复 identity、循环引用、缺失 owner 或缺失 Emitter
- **THEN** Frontend MUST在 Semantic Emission 或 artifact publish 前失败并报告精确 source identity
- **AND** MUST不跳过无效元素、读取旧 cache 或调用 Target Compiler


### Requirement: Semantic IR必须表达Character composition roots

Semantic IR MUST用角色控制binding和技能执行根目录表达组合。技能根 MUST保存SkillId、ActionProfile引用、入口、签名、serialized owner和source map；Equipment Route MUST引用明确控制binding及技能。MUST删除Character Root、Equipment Persistent和Equipment Route的角色级flow roots，不建立第二种Feature flow IR。技能局部状态机与子图继续使用相同operation、value port和控制验证。

#### Scenario: 编译多个装备技能

- **WHEN** 多个Feature导出不同技能
- **THEN** IR MUST锁定各控制binding和技能入口
- **AND** MUST不生成装备角色图Host入口

#### Scenario: 技能使用非法控制环

- **WHEN** 技能图包含未通过显式Loop表达的非法控制环
- **THEN** 共享语义校验 MUST拒绝，不因Feature归属放宽

#### Scenario: 编译多个Feature root

- **WHEN** Equipment Profile包含多个Feature
- **THEN** IR MUST按稳定identity包含控制binding和各Skill执行根
- **AND** MUST不保留Persistent／Route角色flow root

#### Scenario: Feature使用非法控制边

- **WHEN** Feature引用的技能包含非法控制边或子图依赖环
- **THEN** 共享Semantic validation MUST拒绝
- **AND** MUST不因Equipment归属放宽规则


### Requirement: Semantic IR必须使用numeric-neutral Equipment schema

Semantic IR MUST表达Slot、Route、Equipment、Feature、Parameter schema/value、Initial Loadout、Presentation requirement、Action binding、Tag/Effect contribution、代码模块state contract、技能绑定、合法技能内equipment operation与capability union。Scalar/Vector/Yaw值 MUST使用numeric-neutral canonical representation，并由Target lowering选择具体ABI。IR MUST不包含Float32 runtime类型、Unity asset引用、Network Model或visual instance。

#### Scenario: 同一Corin源生成双Target

- **WHEN** Float32与Fixed Compiler消费同一validated Semantic IR
- **THEN** 两者 MUST解析相同Equipment/Feature/Route业务identity
- **AND** numeric value MUST分别降低到目标类型

#### Scenario: Target不支持Equipment operation

- **WHEN** Fixed Target operation manifest缺少Feature实际使用的operation
- **THEN** Target compile MUST拒绝整个Program
- **AND** MUST不从IR删除该Feature或operation


### Requirement: Equipment operation必须进入版本化Operation Set

技能内Equipment identity／parameter read和change begin／commit／cancel MUST保留稳定操作身份、typed端口、状态要求与失败结果，并由两个Target共享语义。角色级host entry／exit和route selection MUST迁入代码控制合同，旧Host／Route控制opcode及writer MUST删除。构建必须核对安装操作与代码合同，未知能力不得反射补齐或当作no-op。

#### Scenario: 技能读取装备参数

- **WHEN** 技能使用明确Context和Parameter引用读取Scalar
- **THEN** 编译 MUST验证参数类型并保存稳定ParameterId

#### Scenario: 加载旧Host opcode

- **WHEN** 产物包含已删除的Equipment Host控制操作
- **THEN** 构建或加载 MUST拒绝旧版本
- **AND** MUST不通过Feature回调继续解释旧角色图

#### Scenario: ReadEquipmentParameter端口

- **WHEN** Graph读取Scalar参数
- **THEN** Frontend MUST验证Context/Slot input、Parameter reference和Scalar output port
- **AND** IR MUST保存稳定ParameterId而不是显示名

#### Scenario: 未知Equipment opcode

- **WHEN** Program target遇到未登记Equipment opcode
- **THEN** compile/load MUST明确失败
- **AND** runtime MUST不将其视为成功no-op
