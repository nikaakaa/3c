## Context

参考效果的本质是把三维蒙皮角色当成“每帧变化的二维形状来源”。三维网格负责动画、透视和遮挡，最终画面则由离散色块、简化轮廓和像素描边重新组成。普通Outline只能在已经渲染完成的像素上加边，不能决定哪些三角形属于同一个色块，也不能让两个区域共享同一条简化边，因此不能产生相同的形状语言。

参考实现的正式数据流是：运行时按颜色聚类三角形，CPU蒙皮，GPU投影并回读坐标，GPU生成每区域Mask和深度，CPU回读Mask恢复边界并用共享边锚点执行RDP，GPU按深度重画色块。它已经证明效果，但运行时聚类、两类回读、逐像素遍历区域三角和跨CPU/GPU往返会带来明显延迟与成本。

本项目使用Unity 2022.3和URP 14，现有Renderer Data采用非RenderGraph的`ScriptableRendererFeature`/`ScriptableRenderPass`路径。角色动画、远端插值和相机都有正式管线；新模块必须位于这些结果之后，只拥有渲染表现，不能成为新的Pose或业务作者源。

## Goals

- 先忠实建立平面色块、直线化轮廓、共享边闭合、像素描边和真实三维遮挡的视觉基线。
- 用一条可定位、可统计、固定容量的GPU Mask到CPU轮廓再到GPU合成链完成首版。
- 将不随帧变化的分区与拓扑工作全部移到Editor显式烘焙。
- 让身体、头发、武器等多个Renderer组成一个稳定Source结果。
- 保留场景深度、角色自遮挡、阴影投射、后续透明VFX和全屏后处理的正式顺序。
- 抽象Profile、Artifact、Source和Backend实现边界，但不提供运行时backend选择。

## Non-Goals

- 不通过法线边缘、Sobel、屏幕空间描边或材质Toon Ramp近似参考效果。
- 不把角色截成Sprite、序列帧或相机专属Billboard。
- 不修改Mesh拓扑、骨骼、动画Clip、Pose Graph、FinalIK或Secondary Motion求解。
- 不接入Action、Timeline或临时按键开关。
- 不在`Ready`阶段并行叠加原始PBR表面，也不提供低质量Mask、同步Readback或CPU Rasterizer fallback。
- 不在首版实现全GPU轮廓连接/RDP、全局Region ID Mask、复杂LOD或跨角色GPU批处理。
- 不在Inspector选择、Repaint、Play Mode启动或正常帧自动烘焙Artifact。

## Selected Architecture

```text
Editor显式Bake
  Mesh/材质采样
    -> 三角颜色与邻接聚类
    -> 小区域合并
    -> 稳定Region + 代表色 + 共享三维边链
    -> CharacterShapeProjectionArtifact

URP当前Camera / 最终可见SkinnedMesh Pose
  -> Source Registry显式绑定
  -> 每Renderer一次变形网格捕获
  -> Burst顶点投影、Region Bounds、Atlas Packing
  -> GPU Per-Region Binary Mask + Raw/Completed Depth
  -> Async Readback，仅R8 Mask
  -> Burst边界环连接 + 共享边锚定 + 自适应RDP + 次要小环过滤
  -> Compact Loop/Region Buffer + Indirect Args
  -> GPU Region Quad合成，写Camera Color和Depth
  -> Transparent/VFX
  -> Existing Post Processing
```

这是一条唯一正式链。抽象边界用于隔离作者数据、生成产品、Prefab绑定和运行时实现，不表示存在多个可切换算法。

## Decision: Profile、Artifact、Source和Workspace分离

`CharacterShapeProjectionProfile`保存作者可理解的效果规则，并把需要重新Bake的lineage参数与可直接调节的runtime tuning分开：

- 稳定Profile identity、Bake revision、Bake content hash和runtime tuning revision。
- 颜色聚类阈值、微小区域合并颜色阈值、微小区域三角上限和发布Region最少三角数。
- 材质、子网格和Alpha处理的显式纳入/排除规则。
- RDP最大像素误差、描边像素宽度、次要屏幕环最小面积和最短共享边长度。
- 最大Renderer、顶点、三角、Region、共享链、Atlas像素、轮廓点、环和在途槽容量。

颜色聚类、微小区域合并、材质规则和固定容量属于Bake lineage，修改后使现有Artifact失效并要求显式重烘焙。RDP最大像素误差、描边宽度、次要屏幕环最小面积、最短共享边长度和描边颜色属于runtime tuning，作者可从Source Inspector直接修改；修改只推进runtime tuning revision，使旧在途结果失效并按新参数重新发布，不改变Artifact lineage。

`CharacterShapeProjectionArtifact`保存不可在正常帧重新推导的数据：

- Artifact identity、源Mesh GUID/local id、材质和纹理依赖、Profile identity/revision以及总content hash。
- 每个Renderer binding slot对应的顶点/三角范围。
- 每个Region的稳定RegionId、三角索引范围、代表色和作者标签。
- 跨Region共享边链的三维顶点序列、两侧RegionId和方向。
- 运行时Buffer布局、固定上限和烘焙统计。

`CharacterShapeProjectionSource`只保存Prefab实例绑定：

- Profile和Artifact强引用。
- 有序Renderer binding，每项保存稳定slot和明确`SkinnedMeshRenderer`引用。
- 显式形状投影总开关。
- 是否参与指定Camera类型以及Source层面的可见性。
- Source启停时向唯一Registry登记/注销，不扫描Transform、不按Renderer名称恢复绑定。

Runtime Workspace由Renderer Feature按Camera和Source identity管理，保存持久Mesh、Native容器、GPU Buffer、RTHandle、Readback Slot和最近可发布结果。Workspace不写回Profile、Artifact或Prefab。

该划分让同一份Corin Profile/Artifact可被不同正式可见Prefab复用，但每个Prefab仍显式声明自己的Renderer引用。形状投影不进入`CharacterPipelineDefinition`，因为后者描述角色业务/动画管线，而Artifact的有效性直接依赖具体Mesh和材质。

Source代码默认值、Installer新建值和Corin正式Prefab作者值统一为关闭。

开关启用表示请求Shape Projection取得彩色发布权。Source先让绑定Renderer保持普通Forward职责，完成校验并登记到唯一Registry；首个完整兼容slot发布时，Source在同一状态迁移中进入`Ready`并把Renderer统一切为`ShadowsOnly`。开关关闭或组件禁用时，Source注销、generation递增、状态变为`Disabled`，绑定Renderer保持普通Forward彩色发布与正常阴影，并且不创建Workspace、不提交GPU任务、不发起回读或合成。

该开关是作者主动请求切换当前唯一表现链，不是backend selector。`WaitingForFirstCompatibleResult`阶段尚未取得发布权，Renderer继续普通Forward；`Ready`阶段由Shape Projection唯一发布；`Faulted`会注销Source并释放已经取得的发布权。任何时刻都只有一个Camera彩色owner，不并行绘制原始表面和形状结果，也不会在结果尚未存在时先移除角色。

## Decision: clean-room重建分区语义

Editor Baker重新实现以下可观察算法语义：

1. 对每个三角形内部多个固定重心坐标采样材质颜色，Alpha不满足规则的采样按Profile处理。
2. 通过共享拓扑边建立三角邻接，只在颜色距离满足阈值时连接。
3. 对连接分量生成颜色簇，仅把不超过微小区域三角上限的簇按邻接颜色阈值尝试合并。
4. 丢弃仍低于发布Region最少三角数的颜色簇，再计算已发布Region代表色。
5. 只从两个已发布Region共同使用的拓扑边生成有方向共享边链；外轮廓和已发布Region到丢弃簇的边不进入Shared Chain，使外形继续由RDP直线化。
6. 用稳定排序生成RegionId、ChainId和Buffer范围，使相同输入得到相同Artifact。

实现不复制参考仓库代码、资源或构建配置。参考仓库只用于确认效果和算法阶段；本项目的数据合同、Shader、Job和Unity集成独立实现。

## Decision: 每帧先取得一次变形顶点，再生成Region Mask

Mask阶段不按Region重复蒙皮。每个绑定Renderer在当前渲染Camera提交时只把最终Skinned Pose烘到一个持久Mesh一次，随后把顶点写入固定Native页面。Burst投影Job使用当前Camera的GPU投影约定、Viewport和反转Z规则生成屏幕坐标与深度，并同时累计每个Region的紧致屏幕包围。

相比参考实现把投影放在GPU后再回读全部顶点，本方案直接使用已经在CPU侧取得的变形顶点完成投影，减少一次GPU往返。其业务代价是首版仍承担一次CPU侧变形网格捕获；这是为了优先获得可核对的正确轮廓和共享边数据。以后若用GPU Skinned Vertex Buffer替换该阶段，必须保持同一Artifact和结果identity，并通过独立change删除CPU捕获路径。

投影阶段只剔除完全在裁剪面外、屏幕包围为空或没有有效三角的Region。已由Artifact发布的Region不会再被运行时小环面积阈值整体删除；其余Region按确定性顺序打进同一Atlas，记录Region到Atlas Rect的精确映射。

## Decision: GPU只生成Mask与深度，CPU拥有首版拓扑简化

GPU Raster Compute按Atlas Rect处理每个Region的三角集合，输出：

- R8二值Region Mask。
- Region Raw Depth。
- 为轮廓合成补全的Region Depth。

Compute必须包含SRP Core的平台API定义，并按`UNITY_REVERSED_Z`选择最近深度。在Direct3D、Metal、Vulkan等反向Z设备上，较大的深度值代表更近表面；缺少该平台定义会错误保留背面深度，使区域前后关系倒置。

Mask语义仍是每区域二值图，不在首版引入全局Region ID。Compute只处理当前Region的裁剪Rect，不Dispatch整个画面。Burst恢复边界时，当前Region Rect之外一律视为空像素，即使Atlas其它Rect保存了相邻Region数据也不得参与当前边界邻域判断，保证每个Region独立闭环。深度始终留在对应Readback Slot的GPU纹理中；CPU只请求R8 Mask Atlas。

每个Readback Slot预分配互不共享安全所有权的`Readback Mask Page`与`Contour Mask Page`。唯一R8 Readback通过`CommandBuffer.RequestAsyncReadbackIntoNativeArray`记录在生成该Mask的同一CommandBuffer全部Raster与Depth Completion Dispatch之后，只写Readback页；不得在CommandBuffer提交后再用独立CPU请求读取尚未完成的Atlas。请求完成后在主线程把固定长度R8数据复制到Contour页，Burst Job只读取Contour页。Slot只有在对应回读与Job都完成后才能复用。该双页合同避开Unity 2022.3中同一个NativeArray先参与Job、再被后续Async Readback复用时的安全句柄冲突，不引入正常帧同步回读、临时分配或第二backend。Corin当前2048×2048、三槽配置因此为每个Camera/Source增加12 MiB固定CPU Native内存和每个完成结果一次4 MiB内存复制，换取稳定异步生命周期。Workspace销毁时允许等待其已经记录的请求结束，再释放持久Native页。

Async Readback完成后，固定容量Burst Job执行：

1. 从Mask像素边界恢复有序环。
2. 投影Artifact共享三维边链并定位到对应环。
3. 把共享链端点和必要转折作为两侧Region共同锚点。
4. 把Profile RDP像素误差作为最大容差，并按当前环的面积与周长自适应收紧后在锚点分段内简化。
5. 统一共享链方向并把同一简化点序列写给两侧Region。
6. 每个产生Mask的已烘焙Region始终保留面积最大的主体环；48像素²基线只过滤同一Region中的次要碎屑环或小孔。若简化结果少于三个点或面积退化到原环的四分之一以下，恢复该合法原始闭环，禁止把Region主体整块删除。

CPU拥有拓扑连接能让首版更容易检查闭环、方向、自交和共享边一致性。代价是必然存在Async Readback和至少一帧延迟。该延迟由有界latest-only调度控制，不通过同步Readback消除。

## Decision: 结果identity与有界latest-only调度

每个提交记录：

- SourceId和Source generation。
- Camera instance identity、projection generation、Viewport和目标尺寸。
- Profile/Artifact identity、Bake revision、runtime tuning revision和content hash。
- Render frame、submission sequence和Readback Slot generation。
- Atlas layout、Mask、Depth以及投影顶点页面的同一slot引用。

只有全部identity一致的Mask、Depth、轮廓和Camera目标可以合成。Camera Cut、FOV/Viewport突变、分辨率变化、Source重绑、Artifact/Profile变化或Slot复用会使旧结果失效并清空发布页。

每个Camera/Source拥有固定数量的在途slot。空闲slot存在时提交当前帧；全部占用时不等待GPU，也不增加队列，只跳过本次新提交并继续显示最近兼容完成结果。回读完成后，旧sequence即使晚到也不能覆盖更新结果。该策略的业务取舍是快速运动时轮廓会比最终骨架晚一到数帧，但输入延迟不会反向污染Gameplay、相机和动画，也不会因为积压越来越慢。

首版不提供可调`frameStride`作为默认降级开关。实际提交频率只由固定slot是否可用决定，并通过Diagnostics公开。

## Decision: GPU间接合成替换原始彩色表面

CPU简化结果被压成连续Point Buffer、Loop Range、Region Range和Indirect Args。每个Region实例只覆盖自身屏幕包围Quad；Fragment阶段执行点在环内判断、填充Artifact代表色，并按到轮廓线段的屏幕距离绘制固定像素宽深色边。

投影、Region包围和轮廓点统一保留Unity的Y向上屏幕坐标语义。Composite Vertex必须通过SRP平台裁剪空间转换处理`UNITY_UV_STARTS_AT_TOP`，不能手写`uv * 2 - 1`后直接作为裁剪坐标，否则在裁剪空间Y向下的平台会把完整结果上下翻转。

Fragment从同一slot的Completed Depth取样，将形状深度与Camera不透明深度比较，并向Camera Depth写入通过的形状深度。合成位于`BeforeRenderingTransparents`：

- 已完成的不透明场景可以遮住角色。
- 角色形状深度可以遮挡之后的透明粒子或被其按正式材质规则覆盖。
- 现有Glitch、Radial Blur、Edge Scan和其它后处理看到的是已经合成完成的角色结果。

一个Source的全部Region使用一次间接实例绘制。首版不跨Source合批，因为Source的Readback Slot和Depth Atlas有独立生命周期；后续若跨Source批处理，必须先统一资源identity和容量，而不是只拼Draw Args。

Source进入`Ready`后，安装Source的Renderer不再参与普通Forward彩色绘制。状态迁移把其阴影模式设为`ShadowsOnly`，使原材质只承担ShadowCaster职责；相机彩色和相机深度由形状投影合成发布。Waiting期间Renderer仍是唯一彩色owner；首个兼容slot发布后才完成所有权交接，不能把投影和原始表面作为两层长期叠加。

## Decision: 相机与动画边界

Renderer Feature只使用当前`RenderingData.cameraData`提供的Camera、矩阵、Viewport、XR/Target信息和Color/Depth target。首版明确支持正式Game Camera，不通过`Camera.main`、Tag或场景搜索决定相机；Scene View、Preview Camera、Reflection和Shadow Camera不提交Source形状任务。

捕获发生在角色正式动画、远端插值、FinalIK和已安装的Secondary Motion已经把最终结果作用到Renderer之后。模块不读取Animation Pose Buffer，不调用Animancer Evaluate，不写Transform，也不决定哪个Pose是最终Pose。若URP注入点无法证明这一时序，触发Hard Stop，而不是在`LateUpdate`再维护一份Pose。

## Decision: 固定容量与错误语义

Profile声明并由Artifact固化容量。Source preparation核对Renderer数、顶点、三角、Region、共享链、Atlas、轮廓点、环、在途slot和Indirect instance上限。任一输入超限或Artifact lineage不一致时，Source进入typed Faulted、注销并释放形状发布权；不扩容、不裁掉超限区域、不降低分辨率，也不启动第二个形状backend。

运行时可暂时没有兼容完成结果，例如首帧、Camera Cut后或回读尚未结束。此时Renderer继续发布普通Forward颜色，Diagnostics显示`WaitingForFirstCompatibleResult`；完整兼容结果出现后才由Shape Projection原子接管。它是单一发布权的状态迁移，不是两套backend的质量回退。

## Authoring and Content Flow

1. 作者创建或修改`CharacterShapeProjectionProfile`。
2. 作者在显式Baker窗口选择Corin源Prefab/Mesh集合和Profile。
3. Baker校验材质/纹理可读数据、Renderer slot、Mesh拓扑和容量，生成或替换唯一Artifact。
4. Source Inspector只显示绑定、Artifact lineage、容量和Stale状态；点击显式命令才重新Bake。
5. Corin正式可见Prefab绑定同一Profile/Artifact，并为各Prefab填写明确Renderer引用。
6. 绑定Renderer由Source状态机管理发布权：Waiting保持Forward，Ready迁移为`ShadowsOnly`，Disabled或Faulted恢复Forward。
7. URP Renderer Data安装唯一Shape Projection Feature和正式Shader/Compute资源引用。

正常Play和渲染帧只消费Artifact。没有Artifact、Artifact stale、Renderer slot不一致或Shader资源缺失均为配置错误。

## Diagnostics

运行时提供只读统计：Source/Camera/slot identity、提交帧与显示结果帧、视觉延迟帧数、slot占用、跳过提交数、Renderer/Region/Atlas容量、变形捕获耗时、投影耗时、Mask GPU时间、Readback时间、轮廓/RDP时间、合成Region数、过滤Region数和typed fault。

可视化调试能按显式开关查看Region代表色、Atlas Rect、原始Mask、共享边锚点、简化环和Depth，但不得触发重烘焙、同步Readback或第二次算法执行。

## Migration and Cleanup

1. 建立独立Shape Projection Runtime/Editor/Shader模块与核心合同。
2. 完成Hard Stop Gates并固定URP注入顺序、阴影职责和slot identity。
3. 建立显式Baker和Artifact格式，生成Corin唯一正式Artifact。
4. 实现固定容量Workspace、变形捕获、Burst投影和Atlas布局。
5. 实现GPU Mask/Depth、单Mask Readback和Burst轮廓简化。
6. 实现Indirect Composite并接入Camera Color/Depth。
7. 迁移Corin正式可见Prefab与Renderer设置，把Forward与`ShadowsOnly`职责统一交给Source发布权状态机。
8. 安装唯一Renderer Feature资源引用并接入现有后处理之前的正式顺序。
9. 完成Diagnostics和显式调试视图。
10. 更新`openspec/project.md`记录表现边界、唯一链路和后续优化边界。

不保留旧材质彩色发布、普通Outline近似、同步Readback、自动Bake、CPU Rasterizer、第二Renderer Feature实现或backend selector。

## Tradeoffs

### 选择每区域二值Mask复刻

收益是算法和参考视觉一一对应，区域隔离、边界环和共享边都容易检查，先能回答“效果是否真的复刻”。

代价是Atlas面积和Mask工作量会随Region数量增长。全局Region ID更省重复像素，但它需要重新定义可见性、区域累计和边界归属，不属于简单优化。

### 选择CPU/Burst轮廓连接与RDP

收益是闭环、方向、自交、共享边锚定和过滤逻辑可直接诊断，首版实现风险低于GPU并行拓扑构建。

代价是Async Readback导致一到数帧视觉延迟，并有CPU轮廓成本。它适合作为效果基线，不是最终性能上限。

### 选择CPU侧变形捕获和投影

收益是首版不再把GPU投影结果回读一次，Region Bounds、共享链锚点和Mask输入使用同一坐标页，数据identity更简单。

代价是CPU仍要取得完整变形网格。未来转为GPU Skinned Vertex Buffer会降低CPU成本，但需要重新设计共享边投影和readback边界，因此留给独立优化change。

### 选择latest-only而不是同步等待

收益是GPU慢时不会卡住主线程，也不会让任务队列越积越长；Gameplay、动画和相机保持当前帧运行。

代价是显示轮廓可能落后最终骨架。同步Readback可以减少结果年龄，却会把延迟变成整帧卡顿，对动作Demo更差，因此不提供该路径。

### 选择替换原始彩色表面

收益是视觉语言统一，PBR高光和曲面渐变不会从色块后面漏出，能真正得到三渲二形状效果。

代价是首个兼容结果出来前会短暂显示原PBR表面，Camera Cut或runtime tuning失效时也会出现明确的所有权交接。收益是不会产生透明角色；Ready后仍严格只有Shape Projection发布彩色，不允许两条美术链并行。

### 选择Profile/Artifact不进入CharacterPipelineDefinition

收益是角色业务与动画资产不被具体Mesh、材质和渲染分区污染；同一角色管线可以装配不同外观。

代价是Prefab需要一个明确Source表现绑定。该绑定是Renderer引用的自然owner，不是额外业务旁路。

## Open Questions Deferred by Evidence

- `SkinnedMeshRenderer.BakeMesh`在当前Unity版本和Corin多Renderer规模下的实际主线程成本，需要在实现Diagnostics中记录；不因预估成本提前改变首版算法。
- Corin各材质的Alpha与头发卡片是否需要按子网格排除或按采样规则纳入，属于Profile内容决定，不引入运行时Alpha fallback。
- Corin代表色是直接使用贴图聚类均值，还是由作者在Bake结果上覆盖，二者可共用同一Artifact字段；首版先提供显式可追踪的Bake代表色与可选作者覆盖，不从运行时光照推导。
- 阴影是否需要使用原材质Alpha Clip取决于现有ShadowCaster Pass；若`ShadowsOnly`无法保持正式阴影，触发Hard Stop并重新确定单一阴影owner，不能让`Ready`阶段继续使用Forward彩色覆盖问题。
