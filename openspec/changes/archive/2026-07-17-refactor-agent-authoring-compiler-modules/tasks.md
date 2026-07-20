## 1. 锁定重构边界

- [x] 1.1 盘点schema v7 Snapshot、Intent、Patch IR与Report入口。
- [x] 1.2 盘点`AgentPatchCompiler`全部operation name与现有apply方法。
- [x] 1.3 盘点支持operation、identity shape、asset reference与apply的重复字符串分派。
- [x] 1.4 盘点Condition term kind、引用要求、节点类型与输出Port。
- [x] 1.5 盘点`AgentPatchAuthoringService`的dry-run、Undo、rollback、dirty与save顺序。
- [x] 1.6 盘点Compiler调用方并锁定MCP、Window与Synthesis Evaluator入口。
- [x] 1.7 记录current spec schema v6与代码schema v7的版本矛盾。
- [x] 1.8 记录`bind_asset_reference` no-op及全部引用位置。
- [x] 1.9 记录Corin专用Validator方法、helper与业务名称依赖。
- [x] 1.10 锁定本change不得编辑Network、Simulation Runtime、Presentation、Scene、Build和协议。

## 2. 建立Schema v8与Typed Command合同

- [x] 2.1 将唯一`AgentAuthoringSchema.Version`提升为v8。
- [x] 2.2 定义正式`AgentPatchCommandKind`集合。
- [x] 2.3 定义authoring identity与operation output的typed reference。
- [x] 2.4 定义Graph、StateMachine、State、Element和Timeline目标reference形状。
- [x] 2.5 定义immutable typed command公共合同。
- [x] 2.6 定义StateMachine与State command payload。
- [x] 2.7 定义Transition与ConditionRule command payload。
- [x] 2.8 定义StateBehavior、Action、Timeline和Input command payload。
- [x] 2.9 定义Flow与Property link command payload。
- [x] 2.10 定义typed Condition group与term payload。
- [x] 2.11 建立唯一operation catalog并登记v8正式operation。
- [x] 2.12 从operation catalog删除`bind_asset_reference`。
- [x] 2.13 让未知operation在lowering阶段返回结构化错误。
- [x] 2.14 让v6/v7输入返回unsupported schema且不进入lowering。
- [x] 2.15 删除任何旧schema reader、版本转换或兼容分支。

## 3. 建立Command Lowering与Prepared Plan

- [x] 3.1 建立`AgentPatchCommandLowerer`唯一入口。
- [x] 3.2 将Patch operation唯一id检查迁入lowerer。
- [x] 3.3 将必需字段与互斥字段检查迁入operation catalog。
- [x] 3.4 将authoring identity格式检查迁入typed reference lowering。
- [x] 3.5 将前序operation reference顺序检查迁入planning symbol。
- [x] 3.6 将Timeline ownership与其它枚举解析迁入lowerer。
- [x] 3.7 将Condition group空值与term kind检查迁入Condition lowering。
- [x] 3.8 建立immutable`AgentPatchCommandPlan`。
- [x] 3.9 建立`operation id -> planned output kind/owner scope`窄symbol table。
- [x] 3.10 禁止prepared plan保存Graph、Node、Edge序列化镜像。
- [x] 3.11 保持原始Patch IR只在JSON与lowering边界可见。
- [x] 3.12 让lowering错误保持operation path、code、message和suggestion。

## 4. 建立单次Compile Session

- [x] 4.1 建立`AgentPatchCompileSession`。
- [x] 4.2 将Definition、Snapshot和RootTree所有权迁入session。
- [x] 4.3 将`AgentAssetResolver`迁入session。
- [x] 4.4 将`AgentGraphAuthoringIndex`迁入session。
- [x] 4.5 将planned/apply operation symbol迁入session。
- [x] 4.6 将planned/applied diff写入迁入session。
- [x] 4.7 将touched serialized owner收集迁入session。
- [x] 4.8 建立拓扑mutation后的唯一Index刷新入口。
- [x] 4.9 让`AgentPatchCompiler`只编排lower、preflight与apply。
- [x] 4.10 删除Compiler实例上的Definition、Snapshot、Resolver、Index和operation结果字段。
- [x] 4.11 删除Compiler实例上的dirty owner字段。
- [x] 4.12 保证同一Compiler实例连续调用时不共享上次compile状态。

## 5. 迁移Patch Handler

- [x] 5.1 定义handler的typed preflight与apply合同。
- [x] 5.2 建立command kind到handler的唯一静态catalog。
- [x] 5.3 迁移StateMachine创建与引用逻辑。
- [x] 5.4 迁移State创建、inline StateBehavior和placeholder清理逻辑。
- [x] 5.5 迁移Transition查找、创建与priority逻辑。
- [x] 5.6 迁移ConditionRule edge ownership入口。
- [x] 5.7 迁移StateBehavior node创建与删除逻辑。
- [x] 5.8 迁移Action activation与Action Context绑定逻辑。
- [x] 5.9 迁移Action lifecycle与exit lifecycle逻辑。
- [x] 5.10 迁移TimelineNode inline/shared ownership与template clone逻辑。
- [x] 5.11 迁移Input node与`AgentNodeEmitterRegistry`调用。
- [x] 5.12 迁移flow link与正式Port检查。
- [x] 5.13 迁移property link与PropertyPort Id检查。
- [x] 5.14 让handler通过session注册实际operation输出。
- [x] 5.15 让handler通过session记录精确planned/applied diff。
- [x] 5.16 删除旧`Apply`总switch与旧operation专用分派。

## 6. 迁移ConditionRule Builder

- [x] 6.1 建立独立ConditionRule builder。
- [x] 6.2 建立Condition term emitter合同。
- [x] 6.3 登记`move_stop` emitter。
- [x] 6.4 登记`move_has` emitter。
- [x] 6.5 登记`move_run` emitter。
- [x] 6.6 登记`move_walk` emitter。
- [x] 6.7 登记`turn_facing_angle` emitter。
- [x] 6.8 登记`blackboard_bool` emitter。
- [x] 6.9 登记`state_root_completed` emitter。
- [x] 6.10 登记`action_request` emitter。
- [x] 6.11 将term资产与Input request引用预检迁入对应emitter。
- [x] 6.12 将AND组组合迁入builder。
- [x] 6.13 将OR组组合迁入builder。
- [x] 6.14 将Result连接与PropertyPort校验迁入builder。
- [x] 6.15 保持ConditionRule清理和重建使用正式Graph API。
- [x] 6.16 删除旧Condition term字符串switch与旧组合helper。

## 7. 收敛Dry-Run与资产事务

- [x] 7.1 让Service先lower一次typed command plan。
- [x] 7.2 让dry-run对同一plan执行全部handler preflight。
- [x] 7.3 让dry-run解析现有authoring target与前序planned output。
- [x] 7.4 让dry-run生成handler提供的精确planned diff。
- [x] 7.5 在apply前核对Definition、RootTree和必要Graph identity未变化。
- [x] 7.6 在mutation前收集完整serialized owner。
- [x] 7.7 让apply消费dry-run使用的同一typed command plan。
- [x] 7.8 保持一个Undo group覆盖全部owner。
- [x] 7.9 让Compiler只返回touched owner而不调用`EditorUtility.SetDirty`。
- [x] 7.10 让Service统一标记touched owner dirty。
- [x] 7.11 保持Service唯一调用`AssetDatabase.SaveAssets`。
- [x] 7.12 保持apply或Validator失败时完整rollback。
- [x] 7.13 保持MCP Bridge与Editor Window只调用同一Service。
- [x] 7.14 保持`AgentCompileReport`外部字段和Bridge response形状不变。

## 8. 分离通用Validator与业务Coverage

- [x] 8.1 删除`ValidateCorinAttackHierarchy`调用。
- [x] 8.2 删除Definition名称包含Corin的判断。
- [x] 8.3 删除外层Action状态名白名单检查。
- [x] 8.4 删除Attack1/Attack2具体状态与transition检查。
- [x] 8.5 删除Attack1Cancel/Attack2Cancel具体条件形状检查。
- [x] 8.6 删除仅由Corin路径使用的Validator helper。
- [x] 8.7 保留Graph kind、Timeline ownership、identity、TreeClip和Action Context通用检查。
- [x] 8.8 保留正式Character Simulation dry-run compile report合并。
- [x] 8.9 建立Macro sample coverage evaluator。
- [x] 8.10 让`two_hit_combo` coverage检查typed plan中的外层Attack与内层combo命令。
- [x] 8.11 让`two_hit_combo` coverage检查Action、Timeline、combo和exit命令覆盖。
- [x] 8.12 让`AgentSynthesisEvaluator`调用Macro coverage evaluator。
- [x] 8.13 禁止普通`validate` action运行任何具体角色或Macro coverage规则。

## 9. 清理旧路径并同步文档

- [x] 9.1 删除`bind_asset_reference` no-op处理和支持列表。
- [x] 9.2 删除旧operation shape、identity shape和reference switch。
- [x] 9.3 删除Compiler内dirty owner写入路径。
- [x] 9.4 删除Corin专用Validator代码与错误码。
- [x] 9.5 删除v6/v7 schema描述和旧版本接受路径。
- [x] 9.6 更新Agent authoring实现说明为v8 typed plan链路。
- [x] 9.7 更新Agent operation与Condition term清单。
- [x] 9.8 更新并行边界说明，确认未编辑网络主线所有文件。
- [x] 9.9 搜索并清除废弃Compiler helper、字符串operation分派和无引用类型。
- [x] 9.10 确认没有新增Graph clone、fallback handler、兼容parser或第二application service。

## 10. 编译与严格校验

- [x] 10.1 使用规定参数编译`Assembly-CSharp.csproj`。
- [x] 10.2 使用规定参数编译`Assembly-CSharp-Editor.csproj`。
- [x] 10.3 编译命令使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 10.4 编译后立即执行`dotnet build-server shutdown`。
- [x] 10.5 运行`openspec validate refactor-agent-authoring-compiler-modules --strict --no-interactive`。
- [x] 10.6 运行`openspec validate --all --strict --no-interactive`并解决本change引入的冲突。
