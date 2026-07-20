## MODIFIED Requirements

### Requirement: Gameplay Effect 状态必须使用类型化 Character State Aggregate

每个Actor的GameplayEffect committed状态 MUST由Character State Layout中的唯一typed aggregate拥有，包含canonical ordered Tag sources、Attributes/Modifiers、Active Effects、Period schedule、Prediction journal、lifecycle revisions与change cursor。GE Runtime MUST通过当前Character State Transaction取得typed view并直接读写，不得在每次Evaluate加载或保存多份opaque bytes。Effect Apply、Remove、Period和Additional Effect的局部原子失败 MUST使用同一State Transaction的typed savepoint恢复，不得使用canonical Snapshot codec作为业务undo机制。

#### Scenario: 当前 Tick 没有 Effect 变化

- **WHEN** Actor当前Tick没有Tag、Attribute、ActiveEffect、Period或Journal变化
- **THEN** State Commit MUST复用原GameplayEffect aggregate
- **AND** MUST不解码或重新编码GE状态

#### Scenario: Additional Effect 失败

- **WHEN** Additional Effect在父Effect事务中失败
- **THEN** GE Runtime MUST恢复typed savepoint中的aggregate与change cursor
- **AND** 当前Character State Transaction其它合法领域写入 MUST不被错误回滚

### Requirement: Gameplay Effect Runtime 必须形成独立编译模块

通用Gameplay Effect contracts、operation semantics与canonical typed state codec MUST位于portable Core可共享源集；当前Float32 magnitude、typed aggregate、transaction view与operation evaluator MUST位于Float32 Target源集。二者 MUST不引用BTSMTL authoring object、Networking、Presentation或UnityEngine。Character Compiler MAY将其catalog与typed state declaration编入CharacterSimulationProgram，Kernel MAY通过正式operation contract执行；模块 MUST不创建独立Tick、Manager、隐藏mutable runtime object或第二份GE状态。项目 MUST不要求不存在的独立`ThirdPersonGameplay` assembly作为运行时所有权边界。

#### Scenario: Character 编译 GE

- **WHEN** Character Compiler引用通用Effect compiler contracts
- **THEN** MUST生成portable catalog与Target typed state declaration
- **AND** Gameplay Effect模块 MUST不引用CharacterPipeline或GraphContext

#### Scenario: 普通 DotNet 执行 GE

- **WHEN** 普通.NET Host的Float32 Kernel执行GE operation
- **THEN** MUST使用同一Float32 typed aggregate、State Transaction与canonical codec
- **AND** MUST不创建server专用GE runtime
