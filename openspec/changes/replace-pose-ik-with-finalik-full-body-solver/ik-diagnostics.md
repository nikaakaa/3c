# Lyra Foot Plant与FinalIK诊断口径

## 先判断问题发生在哪一层

正式目标链只有一条，按以下阶段执行：

```text
Lyra Current Grounding Goal
  -> Stance Stabilization Baseline Goal
  -> Pelvis Resolve
  -> optional Swing Foot Predictive Rewrite
  -> FinalIK FBBIK Solved Pose
```

排查时禁止把“Foot IK”当成一个总结果。必须先确认是Lyra current目标错误、contact/anchor稳定错误、pelvis夹紧错误、预测改写错误，还是FBBIK没有满足正确目标。

## 已有运行证据

旧CSV文件已由用户删除，不再恢复或读取；其Hit Location与Impact Point相差约Sphere半径、脚与骨盆下移约0.5米等结论只作为历史记录保留。

`09979`已证明Anchor不是Swing清障资格；该结论保留为历史记录，本轮不重新读取旧文件。

当前唯一诊断输入为`foot-ik-17359cf4a2b94eb2ba70bd24a9a9b290.csv`，SHA-256为`A25893B2C9C09E7C4ACCC0D7887B503F00485FB8740BBB0C2354525ABCD5E7CA`，包含Frame 2054至2293共240个数据帧、311列：

- 240帧均未连接Predictive Modifier，FBBIK backend身份一致且solver failure为空。
- `Current Offset - Unconstrained Offset`逐帧等于`Sole Constraint Offset`；左9帧、右10帧约束超过`0.05m`，全部处于Swing且没有anchor。
- 左Frame 2234约束前Goal Y只变化`+0.014391m`，Stance硬写回`+0.134998m`，最终Goal变化`+0.149389m`；右Frame 2162对应为`-0.007670m`、`+0.128789m`和`+0.121119m`。
- 右Frame 2110的`0.24 -> 0 -> 0.24m` A-B-A产生`+0.117124m`硬约束和`+0.109043m`最终跳变；但单向跨级也重复出现同量级跳变，Surface切换只是触发输入，不是最终owner。
- Pelvis、Anchor和FBBIK在大多数跳变帧只消费硬改写后的Goal；广泛楼梯吸附属于Swing阶段Stance硬约束。
- 左Frame 2228至2230另有`0.420475–0.460385m`FBBIK残差，约束为零且Pelvis平滑；旧实现把Foot Goal预先降成相对position offset，随后FinalIK内部`LimitBend`改变参考Foot位置，形成独立绝对目标失配。

结论：合法Current Query、Sole Clearance Target和Offset Target都已经形成。Plant Contact的物理安全与Swing的轨迹连续必须在同一Stance owner内按接触阶段分工：前者可硬约束同一Value，后者不得硬写回。Corin当前不接Predictive Modifier，提前预测仍是未启用能力。FBBIK独立异常改为绝对effector position并用`0.001m`残差失败边界守住最终发布。

## 普通FootGrounding必须发布的诊断

### 1. 执行与总权重

- Frame Sequence与Completion；
- FootGrounding节点与operation是否存在；
- Body Grounded只读诊断事实；
- 最终Foot Placement Weight；
- Rig、Calibration、Profile与Projection identity。

项目没有可正式映射的独立`UseFootPlacement`、`DisableLegIK`或`GroundDistance`执行参数。FootGrounding节点存在即执行，Foot Placement Weight只应用一次；Body Grounded、Plant Confidence和Animation Foot Speed都没有关闭普通Goal的权限。

### 2. 每脚Sphere Trace

- Rig Foot BoneId；
- Component Foot Transform；
- trace start/end/radius/channel；
- maximum surface slope与换算后的minimum ground normal dot；
- PhysicsScene identity；
- self-collider filter identity；
- DidTraceHit；
- hit collider/surface identity；
- Control Rig语义Hit Location、Unity Impact Point与Hit Normal；修正后的Hit Location与Impact Point必须来自同一接触点；
- Target Foot Offset Z；
- 未命中branch reason。

普通基线每脚只应出现一次Lyra Sphere Trace。命中页中的立面和锐边必须由同一minimum normal dot拒绝；诊断中若出现FinalIK Quality、heel/toe两套current hit、Capsule、Root Cast或第二次查询，说明旧路径尚未删除。

### 3. Foot smoothing和alignment

- Lyra Target Offset、Stance Sole Clearance Target、合成Offset Target、SpringInterpV2候选`Unconstrained Offset`、写回量`Sole Constraint Offset`与约束后`Current Offset`；
- Previous/Target/Current Hit Normal；
- offset spring velocity/state；
- normal spring velocity/state；
- Lyra source constant identity；
- ProcessFootOffset输入normal/offset；
- Current Grounding Foot Transform与节点总weight。

必须能看出trace结果如何经过Lyra相同顺序形成Goal。不得只显示最终ankle目标。

### 4. Stance Stabilization

- Plant Confidence、sole speed、surface distance与Swing/stance输入；
- contact enter/exit threshold、滞回状态与原因；
- surface identity；
- anchor local point/normal；
- anchor world target；
- Lyra current与anchor blend/release weight；
- 当前鞋底支撑面与normal；
- `Sole Clearance Target`及其Component Up向量；
- 当前spring候选鞋底的向上安全缺口、Plant Contact资格、上一帧鞋底surface/平面距离、同surface连续跨面资格、`Sole Constraint Offset`与写回后的唯一spring Value；新surface首次命中的Swing约束必须为零；
- 最终Ankle、Heel/Toe、两点平面距离与`Residual Sole Penetration`；
- Baseline Foot Transform与总weight；
- 不可达或释放原因。

contact不关闭普通Foot Goal，且只决定anchor生命周期。Sole Clearance Target来自唯一Current Surface并进入现有Foot Offset spring；Plant Contact对求值后的候选鞋底持续执行单向安全约束。同surface上，上一帧约束后Heel/Toe均在当前面上、本帧候选首次越到面下时，非Plant脚也执行同一Value写回并取消向下Velocity；新surface首次命中的大缺口仍只走spring。Corin当前没有预测改写。Anchor只保存surface-local稳定位置；释放退混合时旧anchor不再作为鞋底支撑权威，最终残余穿透按Current Surface诊断。

`surface distance`必须来自Lyra spring求值后的候选Ankle/Rotation，而不是IK前动画鞋底。`17359`左脚有45帧满足Plant Confidence为1、鞋底速度低、候选Heel/Toe距支撑面约`0m`至`0.002m`，旧距离却约为`0.289m`，导致高台阶脚被错误保留为Swing且没有anchor。该字段如果随Target Offset升高而同步变大，而最终鞋底平面距离已接近零，就是contact读错阶段，不是需要调contact阈值。

### 5. Pelvis

- Left/Right Target Foot Offset Z；
- Lyra Target Pelvis Offset Z；
- reach共同可达区间与夹紧前/后值；
- Previous/Current Pelvis Offset Z；
- pelvis spring strength、critical damping、target velocity amount、velocity、previous target与initialized状态；
- 最终Component空间竖直Pelvis Pre-Solve Translation。
- Reach失败时的Render Frame、左右Hip/Goal、Goal Weight、腿长、左右区间、全局升降范围与最终交集。

普通基线 MAY显示唯一reach安全区间，但不应再出现AllPlantedFeet、Directional第二目标、horizontal rebalance、heel lift或Actor Movement Compensation。

### 6. 预测Modifier

- Left/Right Swing eligibility；
- Next Landing delay/local offset；
- Body future transform；
- Future Landing hit；
- Ground Envelope segments；
- Swing Clearance；
- Baseline Goal；
- Final Goal；
- rewrite/no-rewrite reason；
- contact handoff与anchor owner。

非Swing脚、anchored脚与Pelvis必须逐值显示为PassThrough。预测无效时Final Goal必须等于Baseline Goal；Swing转contact时必须显示Modifier停止改写并由FootGrounding接管。

### 7. FBBIK

- 输入Pose Completion；
- Goal Set Completion与Rig revision；
- Pelvis pre-solve前后Transform；
- Left/Right Foot Placement Goal；
- 可选Hand Goals；
- foot pre-rotation与绝对effector position；
- bend constraint；
- iterations；
- per-effector residual；
- solver failure；
- 输出Pose Completion。

FBBIK不发布Grounding结果，不读取PhysicsScene或Foot Placement Profile。Foot Placement Position不得在FinalIK内部`LimitBend`之前按旧Foot参考Pose预计算成相对offset；满位置权重Foot residual超过`0.001m`必须typed fail并阻断最终Pose发布。

## 固定排查顺序

遇到“IK没效果、脚悬空、脚穿地、骨盆跳、停下才恢复或膝盖异常”时按以下顺序：

1. 检查输入Pose、Baseline Goal、Final Goal和FBBIK Completion是否同Frame、同Rig。
2. 检查FootGrounding节点存在、总Foot Placement Weight、Body Grounded只读诊断和各项identity；若出现项目侧`UseFootPlacement`、`DisableLegIK`或`GroundDistance` gate，说明旧语义仍未清理。
3. 检查每脚唯一Sphere Trace是否命中正确Collider，minimum dot是否来自现有最大坡度，立面是否已被拒绝。
4. 检查Lyra Target Offset、Sole Clearance Target、合成Offset Target、Unconstrained Offset、Sole Constraint Offset、约束后Current Offset与Hit Normal spring是否按唯一顺序更新。
5. 检查contact、anchor与唯一鞋底支撑面；Swing时Sole Clearance Target不得消失。新surface首次命中必须保持Sole Constraint Offset为零；同surface连续跨面时只有`Continuous Sole Contact=true`才可向上写回同一spring状态，Plant Contact继续持续约束，不得出现状态外硬抬。
6. 检查Lyra Target/Current Pelvis Offset Z和唯一reach夹紧，确认没有第二pelvis target参与。
7. 若启用预测，检查只有未被anchor拥有的Swing脚发生rewrite，stance脚、anchor和Pelvis是否PassThrough。
8. 最后检查FBBIK effector输入、bend constraint、residual和failure。

如果第1至7步的目标错误，不能通过调FBBIK iterations、pull或reach修复。

## 已知错误模式

### 把SphereCast球心和相对脚踝差值当成Lyra Target Offset

旧实现曾把SphereCast球心停止位置保存为Hit Location，再使用`Location - AnimatedAnkle`形成Target Offset。UE 5.7 Control Rig实际把Impact Point写入VM空间Hit Location，并直接读取其Component绝对竖直坐标。

固定规则：Current Grounding的Location必须等于Impact Point；Target Offset必须等于该点的PoseRoot Component竖直坐标，不得减动画Ankle高度。

### 把楼梯立面当成合法支撑

旧查询把minimum ground normal dot固定为`-1`，SphereCast可选中立面、锐边和近竖直圆角，随后脚掌会朝错误normal旋转。

固定规则：Current Grounding直接复用现有最大坡度换算minimum dot，在同一次命中页中选最近合法踏面；不得追加第二查询。

### 只对齐Ankle而忽略鞋底长度

Ankle Goal位于命中面之上不代表Calibration Heel/Toe都在面上，斜坡旋转后其中一点仍可能穿模。

固定规则：在唯一Current Surface的目标Ankle Transform下重建Heel/Toe，把沿Component Up的最小间隙并入现有Foot Offset spring目标；没有合法支撑面时不得使用固定高度。

### 把离散鞋底间隙直接写入Swing脚Goal

楼梯踏面会让当前支撑平面高度离散变化。若在Lyra offset spring之后完整应用鞋底间隙，Swing脚会绕过现有连续状态并单帧吸到高一级；即使surface identity不变，动画旋转造成的Heel/Toe原始需求也可往返。

固定规则：`Sole Clearance Target`进入现有Foot Offset spring；contact先用该spring候选鞋底相对唯一支撑面的距离更新。Plant Contact求值后若Heel/Toe仍穿入同一支撑面，只允许把向上最小修正写回该spring Value并取消向下Velocity。非Plant脚只有在surface identity与上一帧相同、上一帧约束后Heel/Toe对当前面均非负、本帧候选首次越到面下时执行同一修正；新surface首次命中的大缺口不写回Value。Corin当前不接预测。Anchor Blend只负责锁点交接；不得另建clearance滤波或恢复spring状态外平移。

### 把安全Target误当成安全Current Value

连续spring只保证逐步追踪target，不保证离散碰撞约束每帧成立。`0ef04`中正确高踏面出现后，Offset Target已经安全，但Current Value可落后`0.20m`并产生`0.18m`穿透；FBBIK只会忠实执行这个Current Goal。

固定规则：碰撞安全必须检查spring求值后的当前鞋底。Plant Contact持续拥有硬约束；同surface连续跨面属于已知物理边界，也可写回同一Value。除此之外Swing必须保留原spring连续性，不能通过调高频率、离散高踏面Value teleport、FBBIK iterations或输出后处理掩盖业务阶段错误。

### 在FinalIK内部预处理前把绝对Foot Goal降成相对offset

FinalIK `ReadPose`会在effector应用前执行bend constraint的`LimitBend`。若项目先用旧Foot Pose计算`Goal - Foot`，内部预处理改变Foot参考位置后，输出会变成`Goal + 参考位移`；`17359`左Frame 2228至2230因此产生`0.42–0.46m`残差。

固定规则：Foot rotation可以先写入Pending Pose；Foot Position必须作为绝对`effector.position`交给FBBIK。满位置权重Foot residual超过`0.001m`必须返回`FootEffectorResidualExceeded`，不得发布错误Pose。

### Plant Confidence重复缩放

旧链曾把源贡献、Foot Placement Weight和Plant Confidence连续相乘，导致动画混合中的Goal长期偏低。Lyra普通基线没有这层项目自定义contact总闸门。

固定规则：Foot Placement Weight只作为Lyra节点总alpha应用一次；Plant Confidence不乘普通Goal。

### 把Body速度加到动画脚速

烘焙sole local velocity再叠加Body/yaw速度会重复计算运动。sole世界差分又天然包含actor平移，持续移动时两脚都可能高速。

固定规则：这些脚速只能作为明确contact滞回证据，不能关闭普通Goal。预测Swing资格只能读取最终Foot Analysis明确字段，不从世界速度猜测。

### 用surface distance关闭Goal

距离越大往往越需要脚步修正。把距离作为总闸门会在楼梯、高低差和穿插最明显时关闭效果。

固定规则：Lyra current trace和目标公式决定offset；surface distance MAY参与contact进入/退出滞回，但不得作为整个Goal的连续权重。

### 把UE AnimNode FootPlacement当成Lyra

UE通用节点包含Plant/Replant、Ball Pivot、Plant Plane、复杂pelvis和finalizer，本地Lyra内容没有引用该节点。把两者混合会让普通基线无法对照。

固定规则：current grounding只对照`CR_Mannequin_FootPlant`。项目保留的contact/anchor/reach必须明确显示为后置稳定层，不能借UE节点名伪装成Lyra原生，也不能形成第二套current query。

### 把FinalIK Grounding当成Lyra

FinalIK Grounding的Ray/Capsule Quality、velocity prediction、rotation和pelvis与Lyra Control Rig的Sphere Trace、offset/normal spring、pelvis Z链不同。

固定规则：FinalIK只保留FBBIK。诊断中仍出现Grounding backend说明迁移未完成。

### Profile已改但Projection仍旧

Foot Placement Profile、Tuning Layout、Presentation Projection、Float32/Fixed Program与Native Pose Program具有revision依赖。只改源码或Profile不会自动更新正式产品。

固定规则：实现和作者资产完成后等待用户明确触发Character Build；不自动构建、不手改Generated资产、不保留兼容reader。

### Inspector执行重操作

Inspector repaint中的资产扫描、导出、构建或大量格式化会卡住Unity主线程。

固定规则：Inspector只读取完成snapshot；CSV导出和Character Build只能由明确按钮或命令触发。

## 正常结果口径

普通Corin基线完成后，一个合法接地表现帧应满足：

- 每脚只有一次Lyra参数Sphere Trace；
- Current Grounding minimum dot来自现有最大坡度，立面与锐边不成为支撑；
- Target/Current Offset Z和Hit Normal按Lyra顺序更新；
- contact/anchor诊断存在且只影响stance稳定，不把普通Goal归零；
- Sole Clearance Target进入同一个Foot Offset spring；Plant Contact持续把向上安全修正写回同一Value，同surface连续跨面也只消除本帧越界；新surface首次命中的Swing保持原spring连续且Sole Constraint Offset为零，向下继续由原spring释放，并且没有第二鞋底查询、第二spring或固定高度补偿；
- surface-local anchor随同一移动surface重建并连续混合，Swing脚没有anchor；
- Pelvis只有一个Lyra竖直Target/Current链和一个最终reach安全夹紧；
- Baseline Goal不因Plant Confidence、动画脚速或actor移动速度归零；
- FinalIK Grounding、Plant Plane、并列pelvis target与重复contact/anchor owner诊断不存在；
- FBBIK Completion匹配；Foot Placement使用绝对effector position，满位置权重Foot residual不超过`0.001m`，超限时typed failure阻断发布；
- 预测关闭时没有Future query；预测开启时只有Swing脚Final Goal与Baseline不同。

## 用户在Unity中的验证方法

Corin作者资产已通过BTSMTL Document apply和显式Character Build发布，Document checkout/validate为Clean，GameplayLab可进入运行态。用户可在当前已运行的正式Host场景中直接验收本轮Predictor效果：

1. 打开Host Foot IK诊断，先确认Profile、Pose Plan、Rig、Calibration与Projection identity全部匹配。
2. 在平地、斜坡、台阶边缘和移动平台上观察唯一Sphere Trace、minimum dot、Hit Location、Impact Point、Sole Clearance Target、Offset Target、Unconstrained Offset、Sole Constraint Offset、Current Offset、Residual Sole Penetration、Baseline Goal与FBBIK residual。
3. Corin当前必须确认`has_modifier=false`，FootGrounding Baseline Goal直接进入唯一FBBIK；不得出现Future Landing query或Modifier rewrite。
4. 让角色持续移动、停下和重新起步，确认Plant Confidence、sole speed、surface distance与Body Grounded不会把普通Goal总weight归零。
5. 触发表现分支替换或Reset，确认旧contact、anchor与spring状态不会进入新分支；长时间运行时保留任何Reach共同区间错误全文。

验证期间不需要也不应运行Unity batchmode。若作者资产或Generated产品仍是stale，先停止验证，不使用旧Projection冒充本change结果。

## 发布边界

本轮源码已通过Runtime/Editor本地编译、strict OpenSpec validate和Unity显式刷新编译。19个Foot Analysis artifact已由用户明确授权的正式Character Build刷新；Document rebase、dry-run、apply、最终Float32/Fixed Build、checkout与validate均已完成。GameplayLab成功进入Play Mode且没有Foot、FullBodyIK或Presentation failure；Editor保持运行供直接测试。构建只能由明确命令触发，仍禁止Inspector、OnValidate、selection、Preview或运行时自动刷新生成产品。
