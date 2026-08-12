# Change: 添加角色屏幕形状投影渲染

## 目标效果

Corin的身体、头发、衣服和武器不再以原始PBR彩色表面出现在Game Camera中，而是在当前相机画面里重建为一组平面色块：

- 外轮廓和主要内部边界被压成较少的屏幕空间直线段，形成明显的二维剪纸与动画赛璐璐感。
- 每个色块使用离线提取的代表色，不保留原材质高光、法线细节和连续光照渐变。
- 色块之间共用的三维边在投影后使用同一条简化链，避免身体、头发和衣服边界各算各的而开缝。
- 所有可见轮廓使用稳定的像素宽深色描边，小碎块和短环被过滤，画面不会充满三角网格噪声。
- 角色动画、透视缩放、镜头变化、自遮挡、场景遮挡和武器运动仍来自真实三维角色，不把角色替换成预烘焙二维序列帧。
- 该结果替换角色原始彩色表面，不作为一层半透明描边叠在原模型上。

第一份正式内容覆盖Corin当前可见的多个`SkinnedMeshRenderer`，包括身体、头发和武器。只承担权威逻辑而不发布画面的角色实例不安装该表现源。

## Why

参考仓库`NikuKikai/3Dto2Dshape`的独特效果并不来自普通深度/法线描边。它先按贴图颜色和网格邻接把三角形分成稳定区域，再把每个区域投影成屏幕Mask，从Mask恢复像素轮廓，并结合三维共享边进行RDP简化，最后按深度把简化后的多边形重新画回画面。真正决定风格的是“有语义的色块分区、屏幕空间轮廓简化、共享边一致性和重新合成”，不是某一个Outline Shader。

当前项目只有通用后处理和角色动画表现链，没有角色形状投影能力。现有Edge Scan、Glitch等全屏效果只能从最终颜色、深度或法线找边，不能把曲面稳定地压成共享直边色块，也不能阻止内部三角噪声。因此需要新增一个独立的Presentation渲染模块，而不是继续扩展现有全屏描边。

首版先建立可判断“复刻成功”的视觉基线，同时做不会改变区域、轮廓和简化结果的简单优化。全GPU连接、全局Region ID、复杂LOD等会改变数据结构和调度边界，留给视觉基线成立后的独立change。

## What Changes

`CharacterShapeProjectionSource`默认关闭，只有作者显式开启后才进入形状投影链。

### 忠实复刻算法链

- 新增独立`Character Shape Projection`表现能力，唯一正式链路为：
  1. 显式烘焙阶段按材质采样色、三角邻接和颜色阈值生成稳定色块区域。
  2. 烘焙每个区域的三角成员、代表色、共享三维边链、稳定identity和容量。
  3. 每个渲染帧在最终动画Pose已经作用到`SkinnedMeshRenderer`后，取得一次当前变形网格并投影顶点。
  4. GPU按区域生成紧致二值Mask Atlas、原始深度和补全深度。
  5. 仅回读二值Mask；CPU/Burst恢复边界环，将共享三维边作为锚点，执行RDP直线化和小环过滤。
  6. GPU通过区域包围Quad和点在多边形内判断填充代表色、绘制像素描边，并使用对应深度写入Camera Color/Depth。
- 首版保留“每区域二值Mask”的参考语义，不把它偷换为全局Region ID Mask或新的区域累计算法。
- Source在首个兼容结果发布时原子接管角色彩色发布。Waiting阶段绑定Renderer保持普通Forward职责；Ready阶段Renderer只保留正式阴影投射职责，形状投影合成成为唯一角色彩色发布者。
- 形状投影只读取最终可见骨架和相机矩阵，不读取或修改Gameplay Body、动画Pose Buffer、Rollback状态、网络状态、Action、Timeline或Camera状态。
- 使用URP Renderer Feature接入现有非RenderGraph渲染链；不创建Builtin、RenderGraph或第二Renderer兼容路径。

### 新增唯一作者与运行时合同

- 新增`CharacterShapeProjectionProfile`作为效果参数真相，保存分区阈值、最小区域、轮廓简化像素误差、描边像素宽度、屏幕最小环面积、材质/子网格纳入规则和固定容量。
- 新增`CharacterShapeProjectionArtifact`作为显式生成的只读产品，保存区域三角、代表色、共享边链、源Mesh/材质/Profile lineage、content hash和运行时容量。
- 新增`CharacterShapeProjectionSource`作为Prefab表现绑定，显式引用Profile、Artifact和有序`SkinnedMeshRenderer`。运行时只登记这些引用，不按名称或层级搜索Renderer。
- Profile负责作者语义，Artifact负责生成数据，Source负责Prefab绑定，Runtime Workspace负责每帧临时数据。四者不互相替代。
- 打开Inspector、选择对象、Repaint、Domain Reload和进入Play Mode均不自动烘焙；只有显式Bake命令发布Artifact。

### 本change内完成的简单优化

- 把颜色分区、邻接、微小区域合并和共享边发现全部移到Editor显式烘焙，运行时不读贴图、不聚类三角形。
- 每个Renderer每个提交帧只取得一次变形网格并复用全部区域，禁止按区域重复蒙皮或`BakeMesh`。
- 顶点投影和区域屏幕包围计算在CPU/Burst侧由同一份变形顶点完成，省去“GPU投影后再把坐标读回CPU”的额外同步；这不改变投影数学和Mask结果。
- 所有Mesh、`NativeArray`、`GraphicsBuffer`、RTHandle、Async Readback槽位和间接绘制参数使用预分配固定容量；正常帧不创建托管集合、不扩容。
- 所有有效区域打进同一紧致Atlas，按屏幕包围裁剪Dispatch；空区域、背后区域、过小区域和无效三角在进入Mask阶段前剔除。
- 每个提交只回读R8二值Mask，不回读深度；深度纹理跟随同一环形槽保留在GPU，直到对应简化结果完成合成。
- 轮廓恢复、共享边匹配、RDP和小环过滤使用Burst Job处理固定容量数据。
- 使用有界latest-only调度。最多保留固定数量的GPU回读槽；槽满时丢弃新的旧价值提交，不阻塞主线程、不排无界队列。相机切换、分辨率变化、Profile/Artifact变化会清除不兼容旧结果。
- 一个Source的全部区域通过一次间接实例绘制合成，禁止每个色块单独提交Draw Call。

### 后续优化边界

以下内容不进入本change，也不以隐藏开关、备用backend或并行路径预埋：

- 全局Region ID Mask与每区域累计重构。
- GPU端轮廓连接、环排序、共享边约束和RDP。
- 完全GPU Driven的跨角色批处理与跨Source间接绘制。
- 基于距离的复杂LOD、动态分辨率、时间拓扑匹配和预测插值。
- Narrow-band深度、Tile Bin、Mesh Shader或平台专用实现。

视觉基线成立后，后续change必须替换现有瓶颈阶段并删除旧实现，不能让CPU轮廓与GPU轮廓长期共存为可选backend。

## Impact

- 新增capability：`character-shape-projection-rendering`。
- 新增独立Runtime、Editor、Shader和Generated Content模块，不修改`CharacterPipelineDefinition`的Gameplay/Animation配置合同。
- 影响URP Renderer Data、Corin可见Prefab、Corin Renderer阴影/彩色发布设置，以及角色渲染资源生命周期。
- 不修改角色输入、状态机、KCC、动作时间轴、Animancer、Pose Graph、FinalIK、Secondary Motion、GameplayCue、网络包或Rollback快照。
- 不把效果Activation直接接到Action或Timeline。当前项目没有统一GameplayCue视觉消费者；Source只由作者通过同一显式开关启用，避免创建第三条业务到表现的临时调用链。
- 参考仓库作为效果与算法研究来源；实现采用clean-room方式重新定义项目合同和代码，不复制其源文件，也不形成运行时依赖。仓库未提供可直接继承到本项目的明确License时，不把其源码或资产搬入项目。

## 与Current Spec及Active Change对比

- current specs没有角色形状投影或同类渲染能力，因此本change新增独立spec，不修改现有全屏后处理能力的语义。
- current `character-animation-pipeline`和`character-pipeline-runtime`要求最终动画Pose由唯一正式链发布。本change只在URP渲染阶段读取已经作用到`SkinnedMeshRenderer`的最终变形结果，不采样第二份Animator、不写骨骼、不发布Pose，因此不形成第二动画链。
- current `character-camera-pipeline`把Camera定义为本地Presentation消费者。本change从当前URP Camera Context取得视图、投影、Viewport和Depth，不使用`Camera.main`、场景搜索或Gameplay相机旁路。
- current `character-presentation-interpolation`决定远端角色最终可见Pose。本change处理插值后的可见Renderer结果，不回读Simulation Pose或Snapshot Pose，因此不会绕开现有平滑链。
- `CharacterPipelineDefinition`当前只拥有Program、Projection、Input等角色管线依赖。形状投影是Mesh/Prefab和Renderer专属内容，放进Definition会把Gameplay/Animation配置与具体美术网格绑定，因此本change不扩展Definition。
- active `add-secondary-motion-pose-node`与`replace-pose-ik-with-finalik-full-body-solver`都可能改变最终Physical Pose的形成过程。本change没有同一文件级或数据合同级依赖；无论最终Pose内部由哪些节点形成，Mask捕获只能发生在其最终结果已应用之后，不能捕获基础Pose或中间Pose。
- 现有Edge Scan、Glitch、Radial Blur等Renderer Feature继续作为形状投影合成后的通用画面处理，不成为形状投影的边界生成器或fallback。

现行spec没有与本change直接矛盾的要求，也没有需要删除的重复capability。实施时若发现只能在Final Pose之前取得变形网格，或只能通过第二Animator/第二Renderer彩色路径得到结果，必须停止而不是修改现行动画合同绕过问题。

## Hard Stop Gates

1. 必须证明选定URP注入点看到的是同帧最终可见`SkinnedMeshRenderer`变形结果，并且不要求修改骨骼、重复Evaluate Animator或建立shadow skeleton。
2. 必须证明绑定Renderer可在不发布原始Forward彩色表面的前提下继续提供明确的阴影投射职责；不能通过“原模型照画、投影效果盖在上面”完成复刻。
3. 必须证明对应Mask、Depth、Camera、Viewport、Source和Artifact generation可以在异步回读期间保持同一identity；不能把新一帧深度和旧一帧轮廓拼在一起。
4. 必须证明Camera Depth在`BeforeRenderingTransparents`合成点可正确接受形状深度并与场景不透明物、后续透明VFX和后处理保持正式顺序。
5. 必须证明所有正常帧资源存在固定容量和明确上限；超限必须报告配置错误，不能临时分配、降质量或切换备用路径。

任一门禁失败时停止实施并记录Unity/URP API、受影响Renderer和数据identity；不得在`Ready`阶段加入原始表面叠加fallback、同步GPU Readback、第二轮Animator Evaluate、自动降低分辨率或CPU/GPU双backend。
