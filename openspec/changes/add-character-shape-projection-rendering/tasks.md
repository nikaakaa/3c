## 1. 建立模块边界与核心合同

- [x] 1.1 新增独立Shape Projection Runtime、Editor和Shader模块及asmdef
- [x] 1.2 定义稳定`ShapeProjectionProfileId`、`ShapeProjectionArtifactId`、`ShapeProjectionSourceId`、`ShapeProjectionRegionId`和`ShapeProjectionChainId`
- [x] 1.3 定义Profile revision、Artifact lineage、content hash和Source generation合同
- [x] 1.4 定义Renderer slot、Region range、Shared Chain range、Atlas Rect和Loop range的固定布局
- [x] 1.5 定义Camera、Viewport、RenderFrame、submission、slot generation和result identity
- [x] 1.6 定义`Ready`、`WaitingForFirstCompatibleResult`、`Stale`、`Faulted`运行时状态
- [x] 1.7 禁止核心合同保存backend selector、原始材质fallback或自动质量降级字段

## 2. 完成实施门禁

- [x] 2.1 固定看到同帧最终Skinned Pose的URP捕获注入点
- [x] 2.2 证明捕获不调用第二次Animator Evaluate、不写骨骼且不需要shadow skeleton
- [x] 2.3 固定Mask Pass、Composite Pass、透明物和现有后处理的执行顺序
- [x] 2.4 证明Composite可以向当前Camera Color和Depth发布正式结果
- [x] 2.5 证明场景不透明深度、角色形状深度和后续透明VFX的遮挡顺序
- [x] 2.6 证明`ShadowsOnly`绑定Renderer不发布原始Forward颜色且保留明确ShadowCaster职责
- [x] 2.7 证明Async Readback期间Mask、Depth、投影页和identity不会被提前复用
- [x] 2.8 任一门禁失败时停止实施并记录Unity/URP API、Renderer和数据影响

## 3. 建立Profile和Artifact资产

- [x] 3.1 新增`CharacterShapeProjectionProfile`
- [x] 3.2 新增颜色聚类、微小区域合并和最小三角数参数
- [x] 3.3 新增材质、子网格和Alpha纳入/排除规则
- [x] 3.4 新增RDP像素误差、描边像素宽度、最小环面积和最短共享边参数
- [x] 3.5 新增Renderer、顶点、三角、Region、Chain、Atlas、轮廓点、环、slot和Indirect instance容量
- [x] 3.6 新增`CharacterShapeProjectionArtifact`
- [x] 3.7 保存源Mesh、材质、纹理和Profile lineage
- [x] 3.8 保存Renderer slot、三角成员、Region代表色和稳定范围
- [x] 3.9 保存有方向共享三维边链和两侧Region identity
- [x] 3.10 保存运行时Buffer布局、容量和Bake统计
- [x] 3.11 拒绝重复identity、非法范围、非有限参数、空Region和容量不一致

## 4. 实现显式Editor Baker

- [x] 4.1 新增Shape Projection Baker显式窗口和Bake命令
- [x] 4.2 从明确选择的Prefab/Mesh集合建立稳定Renderer slot
- [x] 4.3 按固定三角内部采样点读取材质颜色和Alpha
- [x] 4.4 建立共享拓扑边和三角邻接
- [x] 4.5 按Profile颜色阈值生成连接区域
- [x] 4.6 按正式邻接与颜色规则合并微小区域
- [x] 4.7 计算Region代表色和稳定Region排序
- [x] 4.8 提取外边界与跨Region共享边
- [x] 4.9 将边段连接为稳定有向共享链
- [x] 4.10 生成稳定RegionId、ChainId、范围和content hash
- [x] 4.11 以一次资产事务创建或替换唯一Artifact
- [x] 4.12 输出材质依赖、Region、Chain和容量诊断
- [x] 4.13 禁止Inspector Repaint、Selection、Domain Reload、Play Mode和正常帧自动Bake

## 5. 建立Source绑定与Registry

- [x] 5.1 新增`CharacterShapeProjectionSource`
- [x] 5.2 显式引用唯一Profile和Artifact
- [x] 5.3 定义有序Renderer slot与明确`SkinnedMeshRenderer`引用
- [x] 5.4 定义正式Game Camera参与规则和Source可见性
- [x] 5.5 在Source启停时向唯一Registry登记和注销
- [x] 5.6 拒绝缺失、重复、跨Prefab或slot不一致的Renderer绑定
- [x] 5.7 拒绝Profile/Artifact identity、revision或content hash不一致
- [x] 5.8 拒绝Renderer仍发布普通Forward彩色表面
- [x] 5.9 禁止Transform层级、名称、Tag、`Camera.main`或场景搜索
- [x] 5.10 新增显式形状投影总开关，关闭时注销Source、停止全部投影工作并恢复原始Forward彩色发布

## 6. 建立固定容量Workspace与调度

- [x] 6.1 为每个Camera/Source创建固定容量Runtime Workspace
- [x] 6.2 预创建每Renderer持久变形Mesh和Native顶点页面
- [x] 6.3 预创建投影顶点、Region Bounds、Atlas布局和三角上传页面
- [x] 6.4 预创建Mask、Raw Depth、Completed Depth RTHandle
- [x] 6.5 预创建Region、Loop、Point、Indirect Args GraphicsBuffer
- [x] 6.6 预创建固定数量Async Readback Slot和CPU轮廓页面
- [x] 6.7 实现Source/Camera/Profile/Artifact/Viewport/submission/slot完整identity
- [x] 6.8 实现空闲slot提交和slot满跳过新提交
- [x] 6.9 实现late callback不能覆盖更新sequence
- [x] 6.10 在Camera Cut、Viewport、分辨率、Source generation或Artifact变化时清除不兼容结果
- [x] 6.11 在正常帧禁止托管集合创建、Buffer扩容和RTHandle重建
- [x] 6.12 超出任何容量时发布typed Faulted而非裁剪或降级

## 7. 实现变形捕获、投影和Atlas布局

- [x] 7.1 在选定URP阶段读取当前正式Camera Context
- [x] 7.2 每个提交帧对每个绑定Renderer只捕获一次变形Mesh
- [x] 7.3 把变形顶点写入固定Native页面
- [x] 7.4 使用Burst按当前GPU投影约定计算屏幕坐标和深度
- [x] 7.5 从同一投影页计算每个Region紧致屏幕包围
- [x] 7.6 剔除裁剪面外、空包围、无有效三角和屏幕过小Region
- [x] 7.7 按稳定Region顺序把有效包围打进Mask Atlas
- [x] 7.8 生成Region到Atlas Rect和三角范围映射
- [x] 7.9 投影共享三维链端点并保存同一slot锚点页面
- [x] 7.10 禁止按Region重复`BakeMesh`、重复顶点投影或GPU坐标回读

## 8. 实现GPU Region Mask与Depth

- [x] 8.1 新增正式Shape Projection Mask Compute Shader
- [x] 8.2 上传当前slot的投影三角、Region Range和Atlas Rect
- [x] 8.3 按Region Rect生成R8二值Mask
- [x] 8.4 生成Region Raw Depth
- [x] 8.5 生成轮廓合成使用的Completed Depth
- [x] 8.6 让Dispatch只覆盖有效Region Rect
- [x] 8.7 对裁剪、反转Z、退化三角和深度空洞使用统一规则
- [x] 8.8 对R8 Mask发起唯一Async GPU Readback
- [x] 8.9 保持Depth在对应GPU slot直到结果合成或失效
- [x] 8.10 禁止深度回读、同步Readback和全画面逐Region Dispatch

## 9. 实现Burst轮廓连接与简化

- [x] 9.1 从回读Mask恢复像素边界段
- [x] 9.2 把边界段连接成有序闭环并统一方向
- [x] 9.3 拒绝断环、越界范围和固定容量溢出
- [x] 9.4 将投影共享链匹配到两侧Region边界
- [x] 9.5 建立两侧共用的端点和必要转折锚点
- [x] 9.6 在锚点分段内执行RDP像素简化
- [x] 9.7 统一共享链方向并复用同一简化点序列
- [x] 9.8 过滤点数、面积或长度低于Profile阈值的环
- [x] 9.9 生成连续Point、Loop Range和Region Range页面
- [x] 9.10 生成当前Source的Indirect instance数量和Args
- [x] 9.11 让所有Job只写固定容量Native页面并携带slot identity

## 10. 实现GPU间接合成

- [x] 10.1 新增正式Shape Projection Composite Shader和Material
- [x] 10.2 为每个Region实例生成紧致屏幕包围Quad
- [x] 10.3 在Fragment中执行多环点内判断
- [x] 10.4 使用Artifact代表色填充Region
- [x] 10.5 按屏幕线段距离绘制固定像素宽深色描边
- [x] 10.6 从同一slot Completed Depth恢复形状深度
- [x] 10.7 与Camera不透明深度比较并写Camera Color/Depth
- [x] 10.8 通过一次Indirect Draw提交一个Source全部Region
- [x] 10.9 保持透明VFX和现有后处理位于形状合成之后
- [x] 10.10 拒绝Mask、Depth、Loop、Camera或slot identity不一致的合成

## 11. 接入URP唯一正式链

- [x] 11.1 新增唯一`CharacterShapeProjectionRendererFeature`
- [x] 11.2 新增Mask/Capture Pass和Composite Pass
- [x] 11.3 只从`RenderingData.cameraData`取得Camera和目标
- [x] 11.4 只让正式Game Camera提交与合成
- [x] 11.5 显式引用唯一Compute、Composite Material和固定运行时设置
- [x] 11.6 把Feature安装到当前正式URP Renderer Data
- [x] 11.7 固定Composite为`BeforeRenderingTransparents`正式顺序
- [x] 11.8 保持现有Glitch、Radial Blur、Edge Scan和其它后处理在其后消费结果
- [x] 11.9 禁止Builtin、RenderGraph、第二Renderer Feature或CPU Rasterizer路径

## 12. 配置Corin正式内容

- [x] 12.1 创建唯一Corin Shape Projection Profile
- [x] 12.2 显式选择身体、头发、衣服和武器Renderer slot
- [x] 12.3 明确配置需要排除的材质、子网格和Alpha规则
- [x] 12.4 显式Bake唯一Corin Shape Projection Artifact
- [x] 12.5 检查Artifact源Mesh、材质、纹理和Profile lineage
- [x] 12.6 在Local正式可见Corin Prefab安装Source和全部Renderer绑定
- [x] 12.7 在AI正式可见Corin Prefab安装Source和全部Renderer绑定
- [x] 12.8 在Rollback与Server Authoritative正式可见Corin Prefab安装Source和全部Renderer绑定
- [x] 12.9 不在authority-only或不发布画面的角色体安装Source
- [x] 12.10 将绑定Renderer原始彩色发布迁移为`ShadowsOnly`职责
- [x] 12.11 删除Corin旧Forward彩色发布和任何临时Outline近似接线

## 13. 完成Diagnostics与清理

- [x] 13.1 发布Source、Camera、slot、submission和显示结果identity
- [x] 13.2 发布结果年龄、slot占用、跳过提交数和typed状态
- [x] 13.3 发布Renderer、Region、Atlas、轮廓点、环和Indirect instance容量
- [x] 13.4 发布变形捕获、投影、GPU Mask、Readback、轮廓/RDP和Composite耗时
- [x] 13.5 提供显式Region、Atlas Rect、Mask、共享锚点、简化环和Depth调试视图
- [x] 13.6 禁止调试视图触发Bake、同步Readback或第二次算法执行
- [x] 13.7 删除实验材质、临时Render Pass、重复Shader资源和旧色彩路径
- [x] 13.8 更新`openspec/project.md`记录唯一Shape Projection链、表现边界和后续优化边界
- [x] 13.9 对账全部受影响current spec与仍在active的动画表现change
