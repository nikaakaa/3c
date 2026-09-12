## 1. 完整盘点与迁移来源

- [ ] 1.1 对实际Skill注册集合逐项盘点kind、variant、系统anchor、payload字段、端口、引用、UI、Document与compiler binding；交付覆盖43个现有kind声明、15个原生wrapper登记及Macro额外登记的完整差异表，不用Corin节点数量代替公开能力集合。
- [ ] 1.2 对旧btsmtl与新Skill的同义节点逐项列出实际代码/资产消费者，区分共用业务合同、合法独立领域与可删除旧Skill类型；交付带调用位置的归属/删除清单，不恢复旧Character RootTree。
- [ ] 1.3 在旧参数与steps仍可读取时，按精确Definition封存作者数据、v7包状态、identity、端点、条件引用、Macro/Timeline/Blackboard闭包和来源hash；交付迁移输入清单及未提交差异记录，拒绝覆盖DocumentDirty/Conflict。
- [ ] 1.4 核对每条状态转移的Step/Edge字段、端口对应和实际并列选择顺序；交付冲突/歧义报告，分别标记字段相同、仅旧值存在、两侧不同、私有条件多owner及顺序变化，未裁决项不得自动迁移。

## 2. 正式参数与节点定义

- [ ] 2.1 建立Skill typed payload与稳定引用值合同，让原生节点/边只持有一份业务参数；交付字段归属表和源码扫描，确认没有壳字段与payload重复保存且没有新增可写图集合。
- [ ] 2.2 在既有共享Capability基础上建立按业务族注册的节点定义，集中kind/variant、默认构造、字段访问/约束/可见性、引用和编译binding；交付注册完整性报告，拒绝缺失binding及重复kind/variant。
- [ ] 2.3 提供不依赖JObject、AgentPackage和插件Port的正式字段读取/修改接口，并接入现有typed Mutation；交付UI与Document使用同一字段访问的调用链和旧字段读写分支删除清单。
- [ ] 2.4 迁移结构、状态生命周期、条件结果、Loop和组合节点的参数定义；交付各kind参数/默认值/端口对照，保留普通步骤语义且不为Loop新增steps。
- [ ] 2.5 迁移Input、Action Request、Blackboard Get/Set与Character State节点定义，复用现有provider/声明合同；交付owner、值类型、访问权限与动态端口覆盖报告，不复制外部provider值。
- [ ] 2.6 迁移Action、Tag、TagQuery、Attribute和Effect节点参数与定义，将5个Ability节点移出Inspector文件；交付字段/资源引用/默认值覆盖表和原定义删除记录。
- [ ] 2.7 迁移Locomotion参数与定义，复用ILocomotionInputMotionAuthoring及现有motion合法性规则；交付ConstantSpeed/曲线位移和执行时长组合的现有Validator/编译结果。
- [ ] 2.8 为15个原生逻辑wrapper登记正式定义与作者端口到Program端口映射；交付完整登记对照及现有编译端口校验结果，不执行Simplex运行逻辑。
- [ ] 2.9 把Macro、State/Condition与Timeline调用参数和TreeClip引用接入正式定义，保留原生Macro接口与Timeline数据唯一来源；交付接口identity、私有/共享owner及typed引用映射表。

## 3. 唯一端口与原生作者表面

- [ ] 3.1 让固定、条件和动态端口统一由正式参数/接口输入共享Port Shape projector，删除Skill中的原型构造反推和重复Blackboard端口规则；交付全部kind/variant的端口身份、类型、方向、容量和顺序报告。
- [ ] 3.2 将原生节点GatherPorts、创建菜单和连接预检改为消费正式形状，保留必要插件钩子并登记实际补丁；交付调用链和源码扫描，确认没有默认实例/已有edge/运行getter推定形状。
- [ ] 3.3 将普通参数Inspector接到正式字段访问和typed控件，删除集中按节点类型绘制字段的重复分支；交付每个可写字段的UI与Document覆盖对照。
- [ ] 3.4 为TagQuery、Effect选择条件、资产引用等复杂字段接入同一定义的typed控件，补齐gameplay-effect-remove.query作者入口；交付字段可见性/必填/读写覆盖表，未知字段仍严格拒绝。
- [ ] 3.5 接通Macro接口编辑、Blackboard变量拖拽、端口默认输入值与改接操作的同一规则/Mutation链；默认输入字面量只使用原生唯一存储，不在payload/properties中复制，交付identity不变、合法改线与非法目标拒绝的现有结构校验记录。

## 4. 状态转移唯一来源

- [ ] 4.1 实现唯一Transfer payload及同来源唯一order，状态节点/入口/任意状态切换固定Transfer端口并退出Composite；交付字段/端口清单，保留StateIn与多转移容量，正常入口拒绝未迁移形状。
- [ ] 4.2 在正式引用合同中表达条件图edge owner与重映射，明确graphId、edgeId和condition槽；交付仅由Edge引用条件图的完整关系输出及重复私有owner诊断。
- [ ] 4.3 实现精确Step到Edge、旧源端口到Transfer及稳定order映射，保留合法UID/目标并处理1.4已裁决差异；交付逐实体迁移计划，不自动选择冲突值或重排并列转移。
- [ ] 4.4 让连线Inspector、编译和Document目标仅使用Transfer参数，普通组合步骤继续自己的字段规则；交付正常消费者扫描，确认不再从状态机steps补读条件/优先级/中止策略。
- [ ] 4.5 将转换接入现有显式迁移和资产事务，旧数据读取只服务一次性转换；交付迁移计划到typed Mutation的调用链，删除第二apply/自动重建方案。

## 5. 引用闭包与Document事务

- [ ] 5.1 让Closure、ClosureIndex、Exporter和Validator统一消费正式引用关系，覆盖Macro、StateBody、Step/Edge条件、Timeline/TreeClip和Blackboard owner；交付各消费者目标集合一致报告与环/角色/悬空引用诊断。
- [ ] 5.2 让GraphCopy、重映射和私有资产回收消费同一引用描述，补齐Edge条件复制；交付副本私有identity与共享引用对照，以及旧节点类型引用分支删除清单。
- [ ] 5.3 将根、私有图、共享Macro、Timeline与跨域Presentation实际owner收集接入既有唯一事务；交付owner关系表和原生编辑/Document写入顺序，领域binding不拥有第二Undo/Save生命周期。
- [ ] 5.4 将参数Apply/Export、创建/删除和失败清理接入正式字段/引用binding，删除包侧重复参数树与旧迁移补读；交付Mutation计划与canonical reverse export字段/引用一致结果。
- [ ] 5.5 实现Document v8版本、状态机固定端点、edge owner、order与严格字段解析；交付旧版拒绝、错误owner/order/steps拒绝及全部非Skill分片保持原业务形状的包对照报告。

## 6. 编译与作者版本

- [ ] 6.1 建立单一原生图读取适配，把正式参数、端口、引用与UID暴露给Occurrence和业务lowering；交付实际调用链，确认无持久化第二图、旧作者节点构造或实际Port getter求值。
- [ ] 6.2 将叶节点与业务域参数发射迁入按族注册的compiler binding，保留现有Semantic IR/Builder与Float32/Fixed运行链；交付公开能力、UI/Document与编译支持集一致报告，以及集中FlowNode类型分支删除清单。
- [ ] 6.3 分离作者语义hash和画布布局hash，让引用图/Timeline语义经正式关系参与计算；交付纯位置/分组变化与参数/顺序变化的hash对照，Document整包hash仍包含layout。
- [ ] 6.4 接通Node/Port/Edge/调用路径的SourceMap与观察版本，区分画布布局hash和Program State LayoutHash；交付同业务目标编译语义与来源identity对照，正常运行仍只消费Program。

## 7. 精确资产迁移与正式收口

- [ ] 7.1 复核1.3冻结来源与当前精确资产/包未发生未解释变化，生成最终完整迁移diff；交付root、source revision、document hash、冲突裁决和全部owner清单。
- [ ] 7.2 通过唯一正式事务迁移精确Skill闭包并保存、反向导出v8目标；交付各阶段结果、稳定identity映射与applied/saved/Clean证据，不手改Unity YAML。
- [ ] 7.3 对迁移后的精确Definition执行重新checkout、无修改dry-run、正式validate和技能编译；交付完整闭包/字段/顺序语义一致报告，不能以Clean代替编译或以旧job代替本次结果。
- [ ] 7.4 使用现有事务/校验入口核对失败路径与跨owner恢复，覆盖新根/私有条件/共享Macro/Timeline及package；交付失败诊断与恢复前后identity/引用/hash报告，不新增故障注入旁路或测试代码。
- [ ] 7.5 通过精确Definition的正式Build生命周期发布本次所需产物并读取当前Console；交付产物身份、来源版本和当前错误记录，阻塞项按所属领域说明，不用历史日志宣称通过。

## 8. 删除与文档归并

- [ ] 8.1 在迁移闭包确认后删除旧参数字段、状态机steps、重复字段/端口/引用规则、零消费者旧Skill类型及一次性迁移器；交付逐项删除与全链消费者扫描，保留合法非Skill模型和必要DTO/编译映射。
- [ ] 8.2 合并本change与原FlowCanvas、metadata、转移change的独有规范增量，保留全部有效Requirement/Scenario并同步current/project/技能的v8与owner说明；交付差异对账和严格校验，禁止旧v7 delta覆盖新合同，任务转交不得当作归档完成。
- [ ] 8.3 回填design交接表中各原任务的实际接收结果和剩余范围；交付精确任务链接，不重复创建数据层实施任务，不改原完成证据。
- [ ] 8.4 汇总代码小步提交、资产/包迁移、正式编译与结构检查结果；交付仍未完成的运行观察/网络/跨域事项清单，不新增手动验证任务或用文档工件齐全宣称功能完成。
