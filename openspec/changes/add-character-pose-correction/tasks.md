## 1. 接口与既有内容对账

- [ ] 1.1 记录实施基线、当前参数/节点/角色资产和并行修改，产出仅包含本变更目标的文件与 owner 清单。
- [ ] 1.2 对接已安装的 Node Definition、Worker、作者模块及场景预览接口，并逐项核对 Animation Rigging、SnapPose 与现有二维混合代码，产出复用文件、许可证、适配调用点及新增逻辑清单；缺口明确归属，不新增替代链。
- [ ] 1.3 列出 MotorLocalVelocityX/Y 的全部代码、作者轴和生成引用消费者，产出世界/局部语义对照；有已验收行为冲突时定位具体资产供用户裁决。
- [ ] 1.4 对齐本 change 与相关 active delta 的同名 Requirement，形成合并对账记录，保持已完成 Foot/IK 和被否决膝角实验的现行结论。

## 2. 样本与资源所有权

- [ ] 2.1 增加 Correction Set 的中性参考、二维轴、受影响骨骼、样本目标及提取出处合同，以正式序列化数据完整表达两种制作结果。
- [ ] 2.2 增加 Graph-owned Correction Slot 与 Profile-owned Binding/Set 子资产关系，以角色 Validator 证明唯一绑定和 owner 引用闭合。
- [ ] 2.3 实现样本、中性点、二维覆盖、缩放、旋转、Rig 和骨骼引用校验，以现有 Validator 输出精确 SampleId/BoneId 错误定位。
- [ ] 2.4 实现样本集、Slot、Binding 的创建、配置、整集重标定和删除 Mutation，以 planned/applied diff 与完整事务 Undo 为交付结果。

## 3. 正式参数与驱动合同

- [ ] 3.1 在唯一 Fact 投影中产生明确的实际/期望角色局部平面速度，保留现行世界 Fact；以同帧坐标定义和正式参数页面字段完成映射。
- [ ] 3.2 统一 FromFact 的 Local 速度读取并移除被替代的重复推导，完成已确认消费者的一致迁移；引用搜索不得残留世界速度冒充 Local 或新节点专用绕行入口。
- [ ] 3.3 为参数驱动节点注册完整 Definition、typed payload、X/Y/权重端口和 Correction Slot 引用，以共享 Capability/Port Shape 及 compiler lowering 通过为完成条件。
- [ ] 3.4 为骨骼方向节点注册独立 Definition、角色标定和同帧输入测量合同，以显式参考骨骼、方向轴、角度域和单位校验通过为完成条件。
- [ ] 3.5 完成参考姿态变更时样本差值与骨骼标定的一次性更新，以整集 Mutation 结果和可定位的非法标定诊断为交付结果。

## 4. 样本编译与纯姿态运行

- [ ] 4.1 编译 Slot/Binding/Set 为固定参考姿态、样本常量、骨骼索引和驱动数据，产出纳入 Program Image 身份的正式布局。
- [ ] 4.2 复用并收敛二维权重与局部 TRS 差值数学，形成两个节点共享的纯数据计算模块；固定中性、零权重、参考半球与输入限域语义。
- [ ] 4.3 实现修改前局部基值采集、多骨骼更新及受影响子树/Virtual 依赖传播，以编译读写集合证明没有边读已修改父骨骼边求子骨骼基值。
- [ ] 4.4 接入现有 PurePose Kernel、Stage、Worker Batch、Value/Workspace 和 Completion 机制，以正式编译计划显示每节点一次求值及 Actor 页隔离。
- [ ] 4.5 完成 Invalid、零权重和 Pose 参数/Curve/lineage 透传，以现有帧 Validator 与 Final Publication 只接受完整结果为完成条件。
- [ ] 4.6 按实施时正式版本升级必要的 Image/Runtime ABI 和生成依赖，旧产物由既有 stale 检查拒绝；引用检查不得出现旧 reader 或运行时补编译。

## 5. IK 拓扑与结果事实

- [ ] 5.1 在统一 Topology 检查前置修正、Goal Sources 与 FBBIK 的共同 Pose 基线，以编译诊断明确拒绝新旧输入混用。
- [ ] 5.2 编译受影响骨骼、后代与 Virtual 依赖集合，接入 PreserveSolvedEffectors/AllowEffectorDisplacement 两种正式政策，以 Source Map 列出末端影响。
- [ ] 5.3 支持合法连续节点对同骨骼的有序叠加，同时拒绝无依赖冲突，以拓扑计划和正式诊断区分两者。
- [ ] 5.4 将后置节点输出接入既有转换/Output/Final Publication，以编译计划保持一个 Goal Set、一个 FBBIK 和一个最终 Writer。
- [ ] 5.5 在现有 Committed 观察中区分节点输出、Solver Result 与最终 Pose，以只读字段来源映射证明没有改写 Foot 结果或重算节点。

## 6. 作者编辑与静态提取

- [ ] 6.1 在 Profile/样本 owner 入口接入中性姿态、二维点、受影响骨骼及目标编辑，以每项操作产生同一正式 Mutation 差异为完成条件。
- [ ] 6.2 接入精确 Clip/时间/Rig 的只读采样和原子重提取，以完整候选、输入 revision 校验及目标 SampleId 保持为交付结果。
- [ ] 6.3 完成静态骨骼手柄、样本选择、增删与整集撤销，以作者数据结果完整且无运行 Actor Transform 写入为完成条件。
- [ ] 6.4 接入 References 的精确 Slot/Binding/Set 导航及单位/末端影响显示，以共享 owner 信息取代节点内样本镜像。
- [ ] 6.5 将完整效果观察与权重调参接到已有正式场景 Actor 接口，以真实运行 lineage、Stale/需构建状态及 Committed 权重为交付结果，不扩展旧 fixture。
- [ ] 6.6 把提取、重标定与发布安排在显式命令，检查 Inspector/selection/focus 路径只读且没有重操作或自动 Build。

## 7. Document 与 Corin 装配

- [ ] 7.1 扩展现有 Profile/Graph 分片中的 Correction 集合和节点 codec，以 strict parser 拒绝未知字段、错误单位和不完整目标状态。
- [ ] 7.2 将 local/stable 身份、owner 顺序、引用和删除接入唯一 Presentation Reconciler/Mutation，以一份完整 planned diff 覆盖全部新对象。
- [ ] 7.3 完成样本集和标定的 canonical reverse export、整包 hash 与失败恢复，以现有 dry-run/apply 生命周期保持整包原子语义。
- [ ] 7.4 同步 btsmtl-agent-authoring 技能、只读 Capability/Asset context 和相关薄桥说明，以人工与 Agent 支持相同最终作者数据为完成条件，不新增局部 MCP。
- [ ] 7.5 为 Corin 配置参数驱动和骨骼方向驱动的正式样本、Binding、状态适用与图接线，以 Definition/Projection Validator 无悬空引用为完成条件，保持 TrainingEnemy 不在迁移集合。
- [ ] 7.6 通过精确 Corin Definition 的正式 Build 发布匹配 ABI 的 Projection 及所需数值目标产物，以生成清单、依赖身份和 source map 完整为交付结果。

## 8. 清理与实施收口

- [ ] 8.1 清理本次替代的重复坐标推导、废弃字段和无消费引用，以定向搜索及正式编译确认只保留一条输入和修正求值链。
- [ ] 8.2 运行适用的现有编译与静态 Validator；若使用 dotnet build/msbuild，带上 --disable-build-servers /nr:false /p:UseSharedCompilation=false 并立即执行 dotnet build-server shutdown，保留实际结果。
- [ ] 8.3 复用现有 Document、Projection 和节点能力检查收口跨模块合同，产出完整接口/资产/生成数据对账，不增加测试代码或人工验收任务。
- [ ] 8.4 更新实施记录中的能力范围、成本变化、规范对账和 project 口径，运行 openspec validate add-character-pose-correction --strict 并确保全部必要规划产物存在。
- [ ] 8.5 按独立修改单元提交中文 Git 记录，仅包含本变更文件；最终任务状态与真实已完成实现、依赖及验证结果一致。
