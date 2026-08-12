## ADDED Requirements

### Requirement: 系统必须以形状投影替换角色原始彩色表面

系统 MUST 在`CharacterShapeProjectionSource`显式启用并发布首个兼容结果后，把角色重建为屏幕空间平面色块、简化直线边界和固定像素描边。Ready阶段形状投影必须是这些Renderer的唯一Camera彩色发布者；原始PBR Forward表面不得在其下方或上方继续发布颜色。

#### Scenario: Corin发布形状投影画面

- **WHEN** 正式Game Camera渲染已安装且Ready的Corin Shape Projection Source
- **THEN** Camera Color显示由Artifact代表色填充的身体、头发、衣服和武器区域
- **AND** 外轮廓与主要内部边界由简化屏幕线段和固定像素描边表达
- **AND** 原始PBR彩色表面不参与Camera Color发布

#### Scenario: 首个兼容结果尚未完成

- **WHEN** Source已准备但没有与当前Camera、Viewport、Profile和Artifact兼容的完成结果
- **THEN** Source状态为`WaitingForFirstCompatibleResult`
- **AND** 绑定Renderer保持普通Forward彩色发布，不得提前进入`ShadowsOnly`制造透明窗口
- **AND** 系统不绘制低质量替代Mask、不使用旧identity结果也不同步等待GPU

#### Scenario: 首个兼容结果原子接管彩色发布

- **WHEN** 当前Source的首个完整兼容slot完成并被发布
- **THEN** Source在同一发布事务中进入`Ready`并把绑定Renderer切换为`ShadowsOnly`
- **AND** 后续Camera彩色只由形状投影合成发布，原始Forward表面不再参与
- **AND** 任一校验、Workspace或异步阶段进入`Faulted`时必须释放形状发布权并恢复Renderer普通Forward职责

#### Scenario: 作者关闭形状投影

- **WHEN** 作者显式关闭`CharacterShapeProjectionSource`的形状投影开关
- **THEN** Source状态为`Disabled`且不登记到Registry
- **AND** 系统不执行变形捕获、GPU Mask、Async Readback、轮廓简化或间接合成
- **AND** 绑定Renderer恢复普通Forward彩色发布和正常阴影职责
- **AND** 已启用状态中的Waiting或Fault不得自动触发该切换

### Requirement: 系统必须通过显式Artifact定义稳定色块与共享边

系统 MUST 以`CharacterShapeProjectionProfile`定义作者规则，以显式Bake生成的`CharacterShapeProjectionArtifact`定义Region三角成员、代表色、共享三维边链、依赖lineage、content hash和固定容量。正常渲染帧不得重新读取贴图、聚类三角或发现共享边。

#### Scenario: 作者显式发布Artifact

- **WHEN** 作者对明确选择的Profile和Corin Renderer/Mesh集合执行Bake命令
- **THEN** Baker按材质采样、三角邻接、颜色阈值和微小区域合并规则生成稳定Region
- **AND** Baker只尝试合并不超过Profile微小区域三角上限的颜色簇，并丢弃仍低于发布Region最少三角数的颜色簇
- **AND** Shared Chain只表示两个已发布Region之间的共同边，外轮廓和已发布Region到丢弃簇的边不得被锁为共享链
- **AND** Artifact保存代表色、有方向共享边链、源依赖和固定运行时布局
- **AND** 相同输入按稳定排序产生相同identity和content hash

#### Scenario: 普通编辑器生命周期发生

- **WHEN** 作者选择对象、打开Inspector、触发Repaint、Domain Reload或进入Play Mode
- **THEN** 系统不自动Bake或替换Artifact
- **AND** Stale状态只作为诊断显示

#### Scenario: 作者调节运行时效果参数

- **WHEN** 作者修改形状直线化最大容差、描边宽度、次要小环面积、共享边长度或描边颜色
- **THEN** Profile只推进runtime tuning revision并使旧的异步结果失效
- **AND** 系统不使Artifact lineage失效，也不要求重新Bake
- **AND** 修改颜色分区、材质规则或固定容量时仍必须推进Bake revision并重新Bake

### Requirement: Source必须使用显式Renderer绑定和单一Registry

`CharacterShapeProjectionSource` MUST 显式引用唯一Profile、Artifact、有序`SkinnedMeshRenderer` slot和形状投影开关，并在开关启用时通过唯一Registry参与渲染。系统不得按Transform层级、名称、Tag、`Camera.main`或场景搜索恢复Source、Renderer或Camera关系。

#### Scenario: Source成功准备

- **WHEN** Profile、Artifact、Renderer slot、lineage和容量全部一致且Renderer处于等待阶段的普通Forward职责
- **THEN** Source以稳定SourceId和generation登记到唯一Registry
- **AND** Runtime按显式slot消费身体、头发、衣服和武器Renderer

#### Scenario: 同一Prefab存在多个运行时实例

- **WHEN** 场景同时实例化两个或更多引用同一Profile和Artifact的Corin Prefab
- **THEN** 每个`CharacterShapeProjectionSource`组件实例 MUST生成并保持自己的运行时SourceId
- **AND** SourceId MUST不作为Prefab序列化身份被多个实例复制
- **AND** Registry MUST同时登记这些实例而不把共享Profile或Artifact视为Source重复

#### Scenario: Renderer或Artifact绑定无效

- **WHEN** Renderer缺失、重复、slot不一致、跨Prefab，或Artifact与Profile/Mesh/material lineage不一致
- **THEN** Source进入typed `Faulted`
- **AND** Source释放形状发布权，Renderer保持正式Forward材质职责
- **AND** 系统不搜索替代Renderer、不自动重烘焙、不替换材质或启动第二backend

### Requirement: 捕获必须位于正式最终可见Pose之后

系统 MUST 只读取当前URP Camera渲染时已经作用到`SkinnedMeshRenderer`的最终可见变形结果。系统不得Evaluate第二份Animator、读取中间Pose、写Transform、修改Pose Buffer或建立shadow skeleton。

#### Scenario: 本地或远端角色提交形状任务

- **WHEN** 角色动画、表现插值、IK和已安装的次级动画已经形成当前可见Renderer结果
- **THEN** Shape Projection对每个绑定Renderer只捕获一次当前变形网格
- **AND** 所有Region复用同一份变形顶点与投影页
- **AND** Gameplay、Rollback、网络和动画事务不接收Shape Projection写入

#### Scenario: 无法证明最终Pose时序

- **WHEN** 当前URP注入点只能取得基础Pose、中间Pose或要求第二次动画求值
- **THEN** 实施或Source preparation必须停止并报告时序错误
- **AND** 系统不创建LateUpdate旁路或第二Pose链

### Requirement: 首版必须使用每Region二值Mask与共享边RDP链

系统 MUST 将有效Region打入紧致Atlas，在GPU生成每Region二值Mask与深度，仅异步回读R8 Mask，并在CPU/Burst恢复边界环、匹配共享三维边锚点、执行自适应分段RDP和次要小环过滤。首版不得以全局Region ID、普通边缘检测或材质描边替代该链。

#### Scenario: Region进入Mask和轮廓阶段

- **WHEN** Region具有有效投影三角和非空屏幕包围
- **THEN** GPU只在其Atlas Rect内生成二值Mask、Raw Depth和Completed Depth
- **AND** GPU按当前SRP平台的深度方向在每个像素保留最近表面
- **AND** 唯一R8 Mask Readback必须记录在生成该Mask的同一CommandBuffer全部Dispatch之后
- **AND** CPU/Burst的Mask邻域取样不得越过当前Region Atlas Rect或读取其它Region像素
- **AND** CPU/Burst从回读Mask生成有序闭环，把Profile像素误差作为上限并按当前环面积与周长自适应收紧
- **AND** 两侧Region对同一共享链使用同一组锚点和简化点序列

#### Scenario: Region或环低于显示阈值

- **WHEN** Region完全离开裁剪范围、没有有效投影三角，或同一可见Region存在低于Profile阈值的次要边界环
- **THEN** 系统过滤不可见Region和低于阈值的次要环
- **AND** 每个产生Mask的已烘焙Region必须保留面积最大的主体环，不得用运行时小环阈值删除整个Region
- **AND** RDP结果少于三个点或面积低于原环四分之一时必须恢复合法原始闭环
- **AND** Corin正式Profile的48像素²基线只用于次要小环过滤

### Requirement: 运行时必须使用固定容量有界调度

系统 MUST 为每个Camera/Source预分配固定数量的变形页、Native页面、GPU Buffer、RTHandle和Async Readback Slot。正常帧不得创建托管集合、扩容或同步等待GPU；所有提交必须使用latest-only有界语义。

#### Scenario: 存在空闲Readback Slot

- **WHEN** 当前Camera/Source有空闲slot且Source处于Ready
- **THEN** 系统为当前Camera、Viewport、Source、Profile、Artifact和submission记录完整identity并提交任务
- **AND** 对每个Renderer只执行一次变形捕获
- **AND** 只发起一次R8 Mask Async Readback
- **AND** GPU回读只写固定Readback Mask页，Contour Job只读取复制后的固定Contour Mask页

#### Scenario: 全部Readback Slot占用

- **WHEN** 当前Camera/Source的固定slot全部在途
- **THEN** 系统跳过当前新提交且不阻塞主线程
- **AND** 系统不增加队列、不分配新slot、不执行同步Readback
- **AND** 最近兼容完成结果可以继续显示

#### Scenario: 运行时输入超过Artifact容量

- **WHEN** Renderer、顶点、三角、Region、Atlas、轮廓点、环或Indirect instance超过正式容量
- **THEN** Source进入typed `Faulted`
- **AND** 系统不裁剪超限数据、不降低分辨率、不切换备用backend

### Requirement: 异步结果必须保持完整identity一致

系统 MUST 让Mask、Depth、投影页、轮廓结果和Camera目标携带一致的Source、Camera、Viewport、Profile Bake/runtime revision、Artifact、submission和slot generation。任何identity不一致的结果不得合成。

#### Scenario: 旧Readback晚于新结果返回

- **WHEN** 较旧submission的Async Readback在较新兼容结果之后完成
- **THEN** 较旧结果不得覆盖当前发布结果
- **AND** 对应slot只在其GPU和CPU消费者完成后回收

#### Scenario: Camera或内容generation变化

- **WHEN** Camera Cut、Viewport、目标尺寸、Source generation、Profile revision或Artifact content hash变化
- **THEN** 系统使全部不兼容在途和完成结果失效
- **AND** 新Mask不得与旧Depth、旧投影页或旧轮廓组合

### Requirement: GPU合成必须恢复颜色、描边和正式深度

系统 MUST 把简化结果压成连续Point、Loop、Region和Indirect Args Buffer，以Region屏幕包围Quad进行间接实例合成。Fragment必须按多边形内部填充Artifact代表色、按屏幕距离绘制固定像素描边，并从同一slot深度恢复形状深度。

#### Scenario: Source存在兼容完成结果

- **WHEN** Composite Pass处理与当前Camera完全兼容的Source结果
- **THEN** 一个Source的全部有效Region通过一次Indirect Draw提交
- **AND** 每个Region只覆盖自身紧致屏幕包围
- **AND** 颜色、轮廓和深度全部来自同一submission slot
- **AND** Y向上的屏幕坐标通过SRP平台约定转换为裁剪空间且合成结果不得上下翻转

#### Scenario: 角色被场景不透明物遮挡

- **WHEN** Region形状深度位于当前Camera不透明深度之后
- **THEN** 被遮挡像素不写Camera Color或Depth
- **AND** 通过深度的形状像素写入Camera Depth供后续透明VFX消费

### Requirement: 形状投影必须遵守现有URP表现顺序

系统 MUST 从当前`RenderingData.cameraData`取得正式Game Camera和目标，并在`BeforeRenderingTransparents`发布形状结果。Scene View、Preview、Reflection、Shadow Camera不得提交形状任务；现有通用后处理必须消费合成后的结果。

#### Scenario: 正式Game Camera渲染

- **WHEN** 当前Camera类型满足Source正式参与规则
- **THEN** Mask和Composite使用该Camera的矩阵、Viewport、Color和Depth target
- **AND** 形状合成位于场景不透明物之后、透明VFX和现有后处理之前

#### Scenario: 非正式Camera渲染

- **WHEN** Scene View、Preview、Reflection或Shadow Camera执行Renderer Feature
- **THEN** 系统不提交、不回读也不合成Source任务
- **AND** 系统不使用`Camera.main`寻找替代Camera

### Requirement: 运行时必须公开结果年龄、成本与错误

系统 MUST 公开Source/Camera/slot identity、提交帧、显示结果帧、结果年龄、slot占用、跳过提交数、各阶段耗时、容量使用、过滤数量和typed状态。调试视图必须读取正式中间产品，不得触发第二次算法或同步GPU操作。

#### Scenario: 异步链产生视觉延迟

- **WHEN** 当前显示的是早于当前Render Frame完成的兼容结果
- **THEN** Diagnostics显示准确submission frame、display frame和结果年龄
- **AND** Gameplay、动画和Camera不等待该结果

#### Scenario: 作者开启显式调试视图

- **WHEN** 调试兴趣要求显示Region、Atlas Rect、Mask、共享锚点、简化环或Depth
- **THEN** 调试视图只读取当前正式slot已有数据
- **AND** 不触发Bake、同步Readback、额外变形捕获或第二次轮廓计算
