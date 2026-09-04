## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose controls -> Goal Contributions -> Goal Assembler -> FullBodyIK -> 显式后续Component Pose controls -> ComponentToLocalPose -> OutputPose`。前后Component控制只在图中声明时存在，不是必须自动插入的阶段。FootPlacement与PoseBoneIKGoals MUST从同一Component Pose扇出typed Goal Contribution，唯一Goal Assembler MUST形成一个Goal Set，唯一FullBodyIK MUST消费该同一Component Pose与该Goal Set；前置修正后的Pose成为它们共同的输入基线。

Runtime MUST不在图外补建Goal Assembler、Foot Placement、FBBIK、空间转换、姿态修正、第二Goal Set、第二Pose Graph或第二Output路径。

#### Scenario: 查看完整Foot Placement拓扑

- **WHEN** 作者查看包含FootPlacement与PoseBone Goal来源的正式Pose Graph
- **THEN** 图 MUST明确显示两个Goal Contribution进入唯一Assembler，再进入唯一FullBodyIK
- **AND** MUST不存在多个Goal Set并行汇入FBBIK的隐藏拓扑

#### Scenario: 前置修正形成不同求解基线

- **WHEN** 修正后Pose只接FullBodyIK而Goal Sources仍接修正前Pose
- **THEN** Compiler MUST拒绝该混用并报告两个Pose来源

#### Scenario: FBBIK后连接姿态修正

- **WHEN** 作者在唯一FBBIK输出后连接合法Component姿态修正
- **THEN** 后续转换和Output MUST消费修正结果
- **AND** MUST不增加第二求解或第二物理写入

### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose Graph MUST声明稳定ParameterId、类型、默认值与允许来源。`ProgramParameterInput` MUST只读取committed parameter page；source-local curve参数 MUST随Pose Value传播；`PoseParameterResolve` MUST按显式`Base | Overlay | Weighted | Max | Min`规则合成。节点 MUST不按字符串、GameplayTag或State显示名查找参数。

角色局部运动参数 MUST由唯一同帧Body/Intent到Presentation Fact的正式投影产生，并明确左右/前后轴及单位；世界MovementDirection、DesiredDirection和LocomotionPlanarBasis各自原有含义 MUST保持。参数页面 MUST只读取正式Fact，不能将世界XZ速度直接标成Local、由某个节点另算方向，或在正式运行和预览中使用不同坐标算法。

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** Compiler MUST校验ParameterId、类型和page layout
- **AND** Runtime MUST不读取Gameplay对象

#### Scenario: 不同朝向下保持相同局部速度

- **WHEN** 两个合法表现帧具有相同角色局部速度但不同世界朝向
- **THEN** Local命名的运动参数 MUST相同，世界方向Fact MUST仍表达各自世界方向

## ADDED Requirements

### Requirement: 后置姿态修正必须显式声明末端影响政策

姿态修正节点 MUST声明`PreserveSolvedEffectors`或`AllowEffectorDisplacement`。前者在FBBIK后要求其静态写入影响集合不触及此前求解的潜在受约束骨骼，后者允许该影响但 MUST向作者显示受影响末端。影响集合 MUST包含修改骨骼、受影响后代及Virtual依赖，不能只检查直接列出的腿或脚，也不能按当帧权重或接触状态跳过检查。

#### Scenario: 保足政策下后置修改骨盆

- **WHEN** 后置节点声明PreserveSolvedEffectors且骨盆修改会影响受约束脚
- **THEN** Build MUST拒绝该连接并报告骨盆到脚的影响链

#### Scenario: 作者明确接受末端位移

- **WHEN** 作者选择AllowEffectorDisplacement并后置修改影响脚的骨骼
- **THEN** 图 MUST保留该显式修正并显示末端影响，最终Pose MUST包含其结果
- **AND** MUST不重写原Solver Result或自动补一次IK

#### Scenario: 两个连续修正有意叠加

- **WHEN** 两个节点通过Pose依赖明确排序并合法修改同一骨骼
- **THEN** 系统 MUST按图顺序施加两次修正，不能仅因骨骼相同而拒绝合法组合
