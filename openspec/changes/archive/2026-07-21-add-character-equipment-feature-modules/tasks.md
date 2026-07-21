# Tasks: 角色装备功能核心

## 1. 基线与所有权清单

- [x] 1.1 核对CharacterPipelineDefinition现有纯引用字段与Inspector入口。
- [x] 1.2 核对Authoring Discovery当前单Root数据合同。
- [x] 1.3 核对Semantic IR root、operation、reference与source map合同。
- [x] 1.4 核对Float32 Program catalog、layout与codec扩展点。
- [x] 1.5 核对Fixed Program catalog、layout与codec扩展点。
- [x] 1.6 核对CharacterSimulationState typed aggregate扩展点。
- [x] 1.7 核对compiled Runnable control的entry与generation生命周期。
- [x] 1.8 核对ActionProfile、ActionInstance与admission执行顺序。
- [x] 1.9 核对Gameplay Tag source与Query evaluator合同。
- [x] 1.10 核对Gameplay Effect apply/remove与EffectHandle合同。
- [x] 1.11 核对Presentation Projection build与source revision合同。
- [x] 1.12 核对Rig binding、Renderer binding与Socket binding正式目录。
- [x] 1.13 记录旧Ability/ActionModule/装备callback与名称查找残留。
- [x] 1.14 确认本change不修改Agent、MCP、Corin配置与Network Model文件。

## 2. Equipment portable identity与authoring合同

- [x] 2.1 定义Equipment capability声明合同。
- [x] 2.2 定义EquipmentSlotId稳定identity。
- [x] 2.3 定义EquipmentActionRouteId稳定identity。
- [x] 2.4 定义EquipmentId稳定identity。
- [x] 2.5 定义EquipmentFeatureId与FeatureRevision。
- [x] 2.6 定义EquipmentParameterId稳定identity。
- [x] 2.7 定义EquipmentLocalStateId稳定identity。
- [x] 2.8 定义EquipmentVisualBindingId稳定identity。
- [x] 2.9 定义EquipmentChangeId稳定identity。
- [x] 2.10 定义Slot required/optional authoring合同。
- [x] 2.11 定义Route owner slot与InputRequest引用合同。
- [x] 2.12 定义Route request consumption合同。
- [x] 2.13 定义Route missing implementation枚举且只保留正式策略。
- [x] 2.14 定义Feature Parameter schema合同。
- [x] 2.15 定义Equipment Parameter typed value合同。
- [x] 2.16 定义Feature local state declaration合同。
- [x] 2.17 定义Feature granted Tag声明合同。
- [x] 2.18 定义Feature passive Effect声明合同。
- [x] 2.19 定义Feature presentation requirement合同。
- [x] 2.20 定义Feature operation/world capability声明合同。

## 3. Equipment Profile与Definition装配

- [x] 3.1 创建CharacterEquipmentProfile资产类型。
- [x] 3.2 创建CharacterEquipmentFeatureDefinition资产类型。
- [x] 3.3 创建EquipmentDefinition authoring类型。
- [x] 3.4 创建EquipmentSlotDefinition authoring类型。
- [x] 3.5 创建EquipmentActionRouteDefinition authoring类型。
- [x] 3.6 创建InitialEquipmentLoadout authoring类型。
- [x] 3.7 创建CharacterEquipmentPresentationProfile资产类型。
- [x] 3.8 为CharacterPipelineDefinition增加可选Equipment Gameplay Profile引用。
- [x] 3.9 为CharacterPipelineDefinition增加可选Equipment Presentation Profile引用。
- [x] 3.10 为CharacterPipelineDefinition增加显式Equipment capability开关。
- [x] 3.11 校验capability、Gameplay Profile与Presentation Profile三者一致。
- [x] 3.12 保持Definition不内嵌Slot、Feature、Equipment或binding表。
- [x] 3.13 更新Definition source dependency收集两个Profile引用。
- [x] 3.14 更新Definition Inspector只绘制两个纯引用与只读状态。
- [x] 3.15 禁止Definition Inspector展开generated Equipment catalog。
- [x] 3.16 删除任何默认Profile自动创建或目录猜测逻辑。

## 4. Equipment authoring Inspector与inline graph owner

- [x] 4.1 创建CharacterEquipmentProfile正式Inspector。
- [x] 4.2 在Profile Inspector编辑稳定Slot catalog。
- [x] 4.3 在Profile Inspector编辑稳定Route catalog。
- [x] 4.4 在Profile Inspector编辑Equipment catalog引用。
- [x] 4.5 在Profile Inspector编辑Feature catalog引用。
- [x] 4.6 在Profile Inspector编辑Initial Loadout。
- [x] 4.7 创建FeatureDefinition正式Inspector。
- [x] 4.8 在Feature Inspector编辑Parameter schema。
- [x] 4.9 在Feature Inspector编辑local state declaration。
- [x] 4.10 在Feature Inspector编辑Granted Tag与Passive Effect。
- [x] 4.11 在Feature Inspector编辑capability与presentation requirement。
- [x] 4.12 为Feature创建可选Persistent inline graph owner。
- [x] 4.13 为每个Feature Route创建inline graph owner。
- [x] 4.14 为Persistent graph提供正式下钻页面入口。
- [x] 4.15 为Route graph提供正式下钻页面入口。
- [x] 4.16 复用BaseTreeView、Undo与Graph mutation API。
- [x] 4.17 复用现有Graph/Node/Edge/PropertyPort identity规则。
- [x] 4.18 拒绝一次性SubTree、AbilityTree与ActionTree owner。
- [x] 4.19 创建EquipmentDefinition正式Inspector。
- [x] 4.20 按Feature schema绘制类型化item参数值。
- [x] 4.21 创建Equipment Presentation Profile正式Inspector。
- [x] 4.22 为visual binding提供稳定identity与类型选择。

## 5. Authoring validation与source revision

- [x] 5.1 校验Slot identity唯一且owner闭合。
- [x] 5.2 校验Route identity唯一且OwnerSlot存在。
- [x] 5.3 校验Route InputRequest存在且类型匹配。
- [x] 5.4 校验Route missing policy与coverage。
- [x] 5.5 校验Equipment identity唯一。
- [x] 5.6 校验Equipment目标Slot与Feature兼容。
- [x] 5.7 校验Equipment参数无缺失、额外和重复。
- [x] 5.8 校验Equipment参数value kind与有限数值。
- [x] 5.9 校验Feature identity、revision和owner唯一。
- [x] 5.10 校验Feature Parameter与local state identity唯一。
- [x] 5.11 校验Feature ActionProfile引用闭合。
- [x] 5.12 校验Feature Tag与Effect引用进入正式catalog。
- [x] 5.13 校验Feature Layer与Producer需求identity完整。
- [x] 5.14 校验Initial Loadout覆盖required Slot。
- [x] 5.15 校验optional Slot显式None语义。
- [x] 5.16 校验Initial Loadout只引用已登记Equipment。
- [x] 5.17 校验同一Loadout不存在跨Slot Route争用。
- [x] 5.18 将Profile与Feature资产GUID加入source dependency。
- [x] 5.19 将inline graph与Timeline依赖加入source revision。
- [x] 5.20 将参数、Tag、Effect与presentation requirement加入source revision。
- [x] 5.21 将Equipment Presentation Unity资产GUID与内容加入Projection revision。
- [x] 5.22 保持Unity visual内容不进入Gameplay SemanticHash。

## 6. Composition root discovery

- [x] 6.1 定义CharacterCompositionRoot role合同。
- [x] 6.2 定义Character Root role。
- [x] 6.3 定义Equipment Persistent Root role。
- [x] 6.4 定义Equipment Route Root role。
- [x] 6.5 为root保存稳定owner identity。
- [x] 6.6 为root保存Feature/Route identity。
- [x] 6.7 为root保存canonical source path。
- [x] 6.8 从Definition RootTree创建Character Root。
- [x] 6.9 从全部允许Feature创建Persistent Roots。
- [x] 6.10 从全部允许Feature创建Route Roots。
- [x] 6.11 未在Initial Loadout中的Feature仍进入root catalog。
- [x] 6.12 递归发现每个root的Graph/Timeline正式引用。
- [x] 6.13 全部root复用同一Graph discovery服务。
- [x] 6.14 全部root复用同一identity与reference validation。
- [x] 6.15 root catalog按稳定identity canonical排序。
- [x] 6.16 拒绝重复owner、重复entry与悬空inline graph owner。
- [x] 6.17 删除目录扫描、全局AssetDatabase查找与命名约定发现路径。
- [x] 6.18 更新discovery diagnostics显示root role与owner。

## 7. Semantic IR与Operation Set

- [x] 7.1 为Semantic IR增加composition root catalog。
- [x] 7.2 为Semantic IR增加Slot catalog。
- [x] 7.3 为Semantic IR增加Route catalog。
- [x] 7.4 为Semantic IR增加Equipment catalog。
- [x] 7.5 为Semantic IR增加Feature catalog。
- [x] 7.6 为Semantic IR增加Parameter schema与typed value。
- [x] 7.7 为Semantic IR增加Initial Loadout。
- [x] 7.8 为Semantic IR增加local state declaration。
- [x] 7.9 为Semantic IR增加Action binding。
- [x] 7.10 为Semantic IR增加Tag/Effect contribution。
- [x] 7.11 为Semantic IR增加presentation requirement identity。
- [x] 7.12 为Semantic IR增加capability union。
- [x] 7.13 定义ReadEquipmentIdentity operation合同。
- [x] 7.14 定义ReadEquipmentParameter operation合同。
- [x] 7.15 定义RequestEquipmentChange operation合同。
- [x] 7.16 定义BeginEquipmentChange operation合同。
- [x] 7.17 定义CommitEquipmentChange operation合同。
- [x] 7.18 定义CancelEquipmentChange operation合同。
- [x] 7.19 定义EnterEquipmentFeatureHost operation合同。
- [x] 7.20 定义ExitEquipmentFeatureHost operation合同。
- [x] 7.21 定义ResolveEquipmentActionRoute operation合同。
- [x] 7.22 为每个operation声明typed input/output/failure port。
- [x] 7.23 为每个operation声明state/reference/capability requirement。
- [x] 7.24 提升Character Gameplay Operation Set版本。
- [x] 7.25 扩展唯一Frontend emitter与lowerer。
- [x] 7.26 复用同一control topology与Value Port validation。
- [x] 7.27 更新Semantic canonical codec与reader。
- [x] 7.28 SemanticHash覆盖Equipment gameplay事实。
- [x] 7.29 缺少operation/world capability时拒绝整个Semantic/Target build。

## 8. Float32与Fixed Program catalog

- [x] 8.1 为Float32 Program定义Equipment catalog结构。
- [x] 8.2 为Fixed Program定义语义一致的Equipment catalog结构。
- [x] 8.3 为两Target降低Parameter typed constants。
- [x] 8.4 为两Target降低Feature graph entry索引。
- [x] 8.5 为两Target降低Initial Loadout。
- [x] 8.6 为两Target降低Tag/Effect contribution。
- [x] 8.7 为两Target降低presentation requirement identity。
- [x] 8.8 将Equipment catalog加入两个Program canonical codec。
- [x] 8.9 将Equipment state declaration加入两个LayoutHash。
- [x] 8.10 将Equipment gameplay内容加入两个ProgramHash。
- [x] 8.11 Program load验证Slot/Route/Feature/Action/entry引用闭包。
- [x] 8.12 Program load验证Target operation capability闭包。
- [x] 8.13 ProgramExecutionLayout一次构建Slot index。
- [x] 8.14 ProgramExecutionLayout一次构建Route index。
- [x] 8.15 ProgramExecutionLayout一次构建Equipment index。
- [x] 8.16 ProgramExecutionLayout一次构建Feature index。
- [x] 8.17 ProgramExecutionLayout一次构建Parameter typed handle。
- [x] 8.18 ProgramExecutionLayout一次构建entry与state address。
- [x] 8.19 Tick热路径只使用stable index与typed handle。
- [x] 8.20 删除Tick内字符串查找、LINQ catalog重建与排序。
- [x] 8.21 更新artifact reader输出Equipment摘要。
- [x] 8.22 reader拒绝unknown Equipment schema/version。

## 9. Equipment typed state与codec

- [x] 9.1 定义portable Equipment slot state合同。
- [x] 9.2 定义Equipment revision合同。
- [x] 9.3 定义PendingEquipmentChange state合同。
- [x] 9.4 定义Feature host generation state合同。
- [x] 9.5 定义Feature contribution handle state合同。
- [x] 9.6 定义Action Equipment Context合同。
- [x] 9.7 为Float32 CharacterState增加Equipment aggregate。
- [x] 9.8 为Fixed CharacterState增加语义一致的Equipment aggregate。
- [x] 9.9 为Feature local declaration分配Target typed state address。
- [x] 9.10 为Float32实现Equipment transaction view。
- [x] 9.11 为Fixed实现Equipment transaction view。
- [x] 9.12 为两Target实现savepoint与rollback语义。
- [x] 9.13 为两Target实现Equipment aggregate canonical codec。
- [x] 9.14 为两Target实现copy/restore/hash。
- [x] 9.15 将Equipment Context加入Action aggregate codec。
- [x] 9.16 将pending change与host generation加入snapshot。
- [x] 9.17 将contribution handle与Feature local state加入snapshot。
- [x] 9.18 Character State Layout identity覆盖全部Feature local state。
- [x] 9.19 Restore拒绝unknown Equipment/Feature/Route/context identity。
- [x] 9.20 Unity visual instance与asset引用不进入state/snapshot/hash。

## 10. Equipment Host与operation runtime

- [x] 10.1 在portable control实现Equipment identity/reference validation。
- [x] 10.2 在Float32 evaluator实现ReadEquipmentIdentity。
- [x] 10.3 在Fixed evaluator实现ReadEquipmentIdentity。
- [x] 10.4 在Float32 evaluator实现ReadEquipmentParameter。
- [x] 10.5 在Fixed evaluator实现ReadEquipmentParameter。
- [x] 10.6 参数读取验证Context或显式Slot/revision。
- [x] 10.7 参数读取拒绝stale revision与value kind不匹配。
- [x] 10.8 定义Route resolution结果与failure code。
- [x] 10.9 在Float32 evaluator实现Route resolution。
- [x] 10.10 在Fixed evaluator实现Route resolution。
- [x] 10.11 实现Persistent Host generation启动。
- [x] 10.12 实现Persistent Host generation正式abort。
- [x] 10.13 实现Route Host进入预编译entry。
- [x] 10.14 实现Route Host Running/complete/cancel lifecycle。
- [x] 10.15 stale generation continuation明确失败。
- [x] 10.16 Host只调用现有compiled Runnable control。
- [x] 10.17 删除Feature Unity asset运行时读取。
- [x] 10.18 删除Equipment callback registry、反射与Service Locator路径。

## 11. Action、Tag与Effect集成

- [x] 11.1 为ActionProfile authoring增加Required Tag Query。
- [x] 11.2 ActionProfile Inspector复用正式Tag Query editor。
- [x] 11.3 ActionProfile validator检查Required Query。
- [x] 11.4 Action compiler将Required Query加入唯一Action catalog。
- [x] 11.5 Float32 Action admission评估Required Query。
- [x] 11.6 Fixed Action admission评估相同Required Query。
- [x] 11.7 固定Required、Block、Target、cancel与request consumption顺序。
- [x] 11.8 Required失败返回结构化reason。
- [x] 11.9 Required失败不激活Route body。
- [x] 11.10 Feature Route activation捕获Equipment Context。
- [x] 11.11 Core Action显式保存None Context。
- [x] 11.12 Action lifecycle fact携带不可变Context。
- [x] 11.13 Action diagnostics显示Slot/Equipment/Feature/revision/Route。
- [x] 11.14 删除Action runtime中的Feature对象与Graph lookup。
- [x] 11.15 删除EquipmentId与WeaponType硬编码admission。
- [x] 11.16 定义equipment Tag source canonical identity。
- [x] 11.17 Tag source覆盖Actor、Slot与EquipmentRevision。
- [x] 11.18 Equipment contribution通过唯一Tag aggregate授予和撤销。
- [x] 11.19 Passive Effect通过正式GE Apply创建。
- [x] 11.20 Equipment aggregate保存EffectHandle。
- [x] 11.21 Equipment remove按handle调用正式GE Remove。
- [x] 11.22 删除第二Tag容器、Buff列表与GE计时路径。

## 12. Equipment change事务

- [x] 12.1 定义typed Equipment change request。
- [x] 12.2 实现RequestEquipmentChange catalog validation。
- [x] 12.3 实现BeginEquipmentChange创建pending记录。
- [x] 12.4 Begin校验Slot、item、from revision与source ActionInstance。
- [x] 12.5 Begin拒绝同Slot重复pending change。
- [x] 12.6 实现CommitEquipmentChange transaction savepoint。
- [x] 12.7 Commit校验old identity与revision。
- [x] 12.8 Commit校验旧Feature Active Action冲突。
- [x] 12.9 Commit正式abort outgoing host generation。
- [x] 12.10 Commit撤销outgoing Tag/Effect contribution。
- [x] 12.11 Commit替换Slot identity并提升revision。
- [x] 12.12 Commit重置outgoing Feature local state。
- [x] 12.13 Commit初始化incoming Feature local state。
- [x] 12.14 Commit安装incoming Tag/Effect contribution。
- [x] 12.15 Commit启动incoming persistent generation。
- [x] 12.16 Commit追加equipment lifecycle output。
- [x] 12.17 Commit追加durable visual selection output。
- [x] 12.18 任一步失败恢复Equipment/Action/Tag/GE/host savepoint。
- [x] 12.19 实现CancelEquipmentChange撤销未提交pending记录。
- [x] 12.20 Action abort显式触发未提交change取消。
- [x] 12.21 commit后Action abort不回滚已提交Equipment。
- [x] 12.22 删除normalized time、Renderer与Timeline completion猜测提交路径。

## 13. Equipment Presentation Projection

- [x] 13.1 定义portable Equipment visual selection。
- [x] 13.2 selection包含Actor、Slot、Equipment、Binding、revision与source tick。
- [x] 13.3 将Equipment visual catalog加入Presentation Projection。
- [x] 13.4 从唯一Equipment Presentation Profile编译visual catalog。
- [x] 13.5 定义ExistingRigObject binding authoring合同。
- [x] 13.6 ExistingRigObject使用正式Rig binding与Renderer set。
- [x] 13.7 定义SpawnedVisualAsset binding authoring合同。
- [x] 13.8 SpawnedVisualAsset使用显式Prefab、Socket、local pose与lifecycle。
- [x] 13.9 Validator检查Slot与binding唯一性。
- [x] 13.10 Projection解析Gameplay Profile VisualBindingId引用。
- [x] 13.11 Projection revision覆盖binding Unity资产GUID与内容。
- [x] 13.12 实现按Actor/Slot持有visual的Equipment Visual Runtime。
- [x] 13.13 ExistingRigObject consumer只启停登记Renderer set。
- [x] 13.14 SpawnedVisualAsset consumer只在登记socket创建唯一instance。
- [x] 13.15 以单调EquipmentRevision应用和替换selection。
- [x] 13.16 拒绝stale selection。
- [x] 13.17 Actor Presentation创建时从committed selection重建visual。
- [x] 13.18 visual创建失败进入明确Presentation invalid诊断。
- [x] 13.19 visual失败不反写EquipmentState或取消Action。
- [x] 13.20 删除Hierarchy名称、模糊Transform路径与默认socket fallback。
- [x] 13.21 删除Visual Runtime直接调用Animancer/Animator的路径。
- [x] 13.22 同revision帧先应用visual再解析动画输出。

## 14. Animation Profile需求闭环

- [x] 14.1 Feature authoring选择Required LayerId。
- [x] 14.2 Feature authoring声明Expected BlendMode。
- [x] 14.3 Feature authoring声明Expected OutputPolicy。
- [x] 14.4 Feature authoring选择Required ProducerId集合。
- [x] 14.5 Animation Profile validator按Feature验证Layer存在。
- [x] 14.6 Validator验证AvatarMask与BlendMode。
- [x] 14.7 Validator验证OutputPolicy。
- [x] 14.8 Validator验证producer唯一binding与transition覆盖。
- [x] 14.9 Timeline compiler验证Feature Route producer使用允许Layer。
- [x] 14.10 Persistent Feature producer继续经过Timeline command queue。
- [x] 14.11 Feature Action producer继续经过Animation Playback Lifecycle。
- [x] 14.12 Equipment Host不提交Layer weight或播放调用。
- [x] 14.13 Feature不保存Animation Profile、Transition或Animancer对象副本。
- [x] 14.14 Projection diagnostics显示Feature需求与resolved binding。
- [x] 14.15 缺失Layer/producer时阻止Projection发布。

## 15. Diagnostics、清理与文档

- [x] 15.1 Program diagnostics显示Equipment catalog与composition root。
- [x] 15.2 Runtime diagnostics显示每Slot Equipment/Feature/revision。
- [x] 15.3 Runtime diagnostics显示pending change与failure code。
- [x] 15.4 Runtime diagnostics显示Persistent/Route host generation与entry。
- [x] 15.5 Action diagnostics关联Equipment Context。
- [x] 15.6 Tag/GE diagnostics关联equipment source与EffectHandle。
- [x] 15.7 Presentation diagnostics显示VisualBinding与resolved Rig/Socket。
- [x] 15.8 全局删除ActionModule、AbilityAsset/Body与Equipment runner残留。
- [x] 15.9 全局删除Hierarchy名称与Transform模糊装备查找。
- [x] 15.10 全局删除武器专用Action admission硬编码。
- [x] 15.11 全局删除Feature内Animation Profile与Layer副本。
- [x] 15.12 全局确认Runtime不读取Equipment/Feature Unity asset。
- [x] 15.13 全局确认只有一个Action catalog、Tag aggregate与GE aggregate。
- [x] 15.14 全局确认Float32与Fixed实现全部Equipment operation和codec。
- [x] 15.15 全局确认Agent、MCP、Corin配置与Network Model未被本change修改。
- [x] 15.16 更新`openspec/project.md`记录Equipment Core正式边界。
- [x] 15.17 记录Corin迁移、Agent接入与网络接入三个后续边界。

## 16. 编译、产物与OpenSpec校验

- [x] 16.1 构建portable Core项目并使用规定的build server参数。
- [x] 16.2 构建Float32 Target项目并使用规定的build server参数。
- [x] 16.3 构建Fixed Target项目并使用规定的build server参数。
- [x] 16.4 构建Character Runtime与Presentation项目并使用规定参数。
- [x] 16.5 构建BTSMTL/Character Editor项目并使用规定参数。
- [x] 16.6 每次编译后立即执行`dotnet build-server shutdown`。
- [x] 16.7 运行Authoring Discovery结构校验。
- [x] 16.8 运行Semantic IR canonical validation。
- [x] 16.9 运行Float32/Fixed Program artifact reader校验。
- [x] 16.10 运行Presentation Projection闭包校验。
- [x] 16.11 使用`rg`确认无fallback、兼容reader、回调注册与名称搜索残留。
- [x] 16.12 `rg`确认没有Agent、MCP、Corin配置与Network Model改动落入本change。
- [x] 16.13 运行`openspec validate add-character-equipment-feature-modules --strict --no-interactive`。
- [x] 16.14 核对全部task勾选只反映真实实现。
