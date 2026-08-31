# 当前组合112611自动回放：骨盆问题仍在

## 本次确实运行了什么

用户指出上一轮只有历史核对、没有新自动测试。本轮没有更改算法或继续调Spring，而是对当前已提交721b69f组合执行一次完整正式回放。该版本包含7ae5793／a9e5f42表面范围与诊断、902019b首次有效查询才创建地面缓存，以及7308791诊断查询工具GUID修复。

确认Unity为Edit、无其他录制／回放／Finalizer，显式Refresh后Console零错误；通过character.fixed_input_trace/replay_start启动原Record43357ff3cd384e5cba75d2c31175b116。没有batchmode、手动推Tick、第二采样器或新测试代码。运行完成后等待诊断封口，再退出本任务Play。

骨盆依然使用已恢复的20毫米余量、3Hz和原目标／响应。当前是在测试整个已提交组合，不是新的骨盆修法，也不把并行Ground Path变化当成由骨盆算法单变量产生。

## 输出与完整性

- 新包：20260831-112611-018-2429691288e6434a8588a55de100efc2。
- 1044个输入Proof帧，1043个采样帧，2086脚行，1224列；geometry67186行。
- facts66语义／analysis1／diagnosis35／quality-score2；37项质量规则未改。
- Finalizer完整发布14文件，failure为空；使用正式character.foot_diagnostics成功读取summary及420帧，不通过反射绕开查询工具。
- Proof：20260831-112721-595-e621459f07d94d57aae4d8765464d721.json；官方是baseline-created，baseline_available=false、compared_frame_count=0，不称官方A/B通过。
- 另直接读取已保存101451原字节Proof ZIP：1044条frames、Runtime identity、trace内容、input sequence、start Body、Body trajectory和表现时钟完全相同。不是只比较帧数。
- samples SHA与Proof／analysis.json一致。完整[归档与清单](../../../../3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/current-integration-112611/README.md)已保存，不覆盖原件。

## 当前路径有效，不是11:10空缓存重演

新表面事实为Ready1787脚行、None299行，没有SurfaceGeometryUnavailable。420左脚正式查询给出Ready／56条表面线段，Foot仍Swing。Ground Path拒绝行数仍为此前101451的299，而不是11:10的空缓存让全部已有Capsule路径失败。

本次只证明这一Record的真实场景加载链能发布合法表面。不能扩成其他小楼梯、斜坡或不同场景都已验证，也不能只凭Ready就宣布全部几何正确。geometry增加表面线段，因此不按新旧总行号强行比较。

## 原始输出逐列对账

与101451按FrameSequence／Side一一对应。原1221列全部仍在，新增GroundSurfaceState、GroundSurfaceWorldRevision、GroundSurfaceSegmentCount三列。

共同列1196个逐字符串相同；24个不同的是运行／采样、Surface／Path身份，另一个实质变化是1786脚行的GroundPathEdgeCount。旧接触采样Edge数量与新有限表面断差的含义不能当作运动改善；本轮没有把它算成“零障碍”或减少错误。

所有Foot状态、输入／动画、目标、Anchor几何、Interpolation、Reach、Goal、骨盆和Solved Knee原始输出均逐值保持。不是只因七维总分相同就认定行为一样。[逐列结果](../../../../3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/current-integration-112611/raw-audit.json)保留差异列与首个位置。

## 骨盆实际质量：没有改善

以下是1042个相邻公共帧，不重复计左右脚。

| 指标 | 101451 | 112611 |
| --- | ---: | ---: |
| 最终World Y绝对步超过50毫米 | 33 | 33 |
| 其中向下超过50毫米 | 24 | 24 |
| 其中向下且本帧硬Reach夹紧 | 17 | 17 |
| 加权Correction步超过50毫米 | 28 | 28 |
| 最大世界下降／420 | 80.210毫米 | 80.210毫米 |
| 最大Correction步／322 | 89.362毫米 | 89.362毫米 |
| Solved Knee实际步超过100毫米 | 157 | 157 |
| Solved Knee最大步／R826 | 628.236毫米 | 628.236毫米 |

420正式工具读取：上一骨盆修正+42.069毫米，本帧目标／输出−1.674毫米，HardReachOutputClamped=true，世界Y=1.78710008米。Correction下降43.744毫米，加上Root及原动画下降，最终世界下降80.210毫米。脚仍是Swing，不是接触锁定才会收紧边界。

968世界下降52.236毫米而Correction只下降0.145毫米；989世界下降63.256毫米、Correction下降37.532毫米且没有硬夹紧。当前仍不能把所有下降归为一个弹簧慢或重复夹紧。

Knee是FBBIK的Solved记录，没有最终Physical Knee测量；本轮没有用方向dot或窄Landing诊断代替全程膝盖统计。

## Foot质量与判决

37个Target的id、question、eventKinds、rules、eligible／matched／rate、scorePolicy和完整score与101451一致。穿透34/90，持续Gap3/60，Stable143/344，Path199/668，Contact433/1051，Landing腿0/2，FullAnchor水平0/15；总分61.9、Evidence86.9。腿部仍只有2个eligible，不能据Health100宣称反弯安全。

本轮判决分开：

1. 自动回放、采样和正式诊断发布完成；当前场景未重演空地面缓存。
2. 本Record上的Foot保护保持，未出现新的骨盆／Knee输出变化。
3. 原骨盆突然下陷完整复现，质量未通过；这次不是修复成功。

接下来仍应沿目标与硬可达区间的最早差异排查，而不是用本轮“输入匹配／能跑完”证明Spring或骨盆算法合格。本轮不继续添加参数、减Goal权重、放宽腿长或修改Bend。

## 保存

原始包和所有旧包保留，14文件ZIP逐个解压流核SHA，Proof直接以原字节ZIP保存。

- samples：958a67ed9883928f075147431049038c1364911fa4479f41d258eb5bc87311c6。
- geometry：54b27182bec1e500ccff2f88b6b6a1e10800ae6dc9159238e2dea7ffcea3cb7c。
- run ZIP：862fe9c0761b2cc2c2e076bd1a0f32a1c2cbd44397ee18bc6b79e6a6e3c9b71a。
- Proof原字节：990e930defee5f85372f0fac3c04fdf7f507ebde0a002e1dc8db3520c28520f5。
- Proof ZIP：de6286545d0df0648b29328a57eff286b3e2b833ff547870e59b7ff6e6cd6138。
