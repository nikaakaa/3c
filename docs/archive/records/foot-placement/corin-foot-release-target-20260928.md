# 释放阶段的脚掌支撑目标修正

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

## 本次解决范围

针对 c28a1158cfdc4658b8471fdba48fadae 的左脚 3858 及随后帧：释放目标仍要求下降，输出脚掌保护持续补高。本次把释放目标限制在其脚掌姿态需要的支撑高度，然后进入现有残差和响应计算。跨边前的提前抬脚、查询失效恢复、膝盖可达性和鞋网格校准不在本次修改范围，不能据此宣称所有踩地问题解决。

## 代码链路

1. Lifecycle 生成正式状态目标。仅对有效的 ReleaseResidual 目标，使用该目标的位置、正式脚掌旋转和输出权重计算脚跟/脚尖，沿用已有 SoleSupportQuery 查询。
2. StateTargetResolver.ConstrainReleaseTarget 读取两个有效命中所需的高度位移。仅当目标脚掌会低于地面时，按权重换算、沿 ComponentUp 抬高目标，并同步 SupportTarget.Position。没有命中时不捏造支撑，不把未来落点当作当前支撑。
3. 现有 EvaluateRelease 接收修正后的目标；残差捕获、衰减、响应历史及释放完成判断均围绕这个目标执行。没有直接清空残差，也没有锁死水平运动。
4. 插值后的脚掌姿态可能与目标不同，仍执行原输出双点保护和历史写回。该保护不是本次删除对象。

已有 QueryOutputSupport 提取成 QueryFootSupport，目标和输出使用同一个姿态计算及查询入口，没有新增物理后端、运行配置或 MonoBehaviour。释放阶段仍保留原支持法线，不在这一改动里另加脚掌旋转策略。

## 采样

正式 Foot 诊断新增 ReleaseTargetSupport，记录修正前释放目标的双点查询。OutputSupport 仍表示插值后、最终夹取前的查询。释放阶段的 desired correction 改为正式修正后目标，避免采样继续把原始 SwingCorrection 当作当前释放目标。

两个观察分别回答：原目标是否会穿地，以及平滑后的实际输出是否仍需要补高。不能把目标查询当作最终骨骼位置。

## 代价与边界

每个有效释放脚每帧额外两次既有支撑探针，沿用预分配命中数组；没有新增正常路径托管分配。其它状态不增加目标查询。该开销尚未做 profiler 测量。

若脚尖在本帧才跨入高踏面，本次仍可能发生末端补高，因为它没有提前多帧准备高度。修正目标能消除持续追向低于踏面的释放终点，但不证明响应过程和最终画面已经平滑，也不解决所有 Landing 高度残差。

## 检查

按用户要求不运行 replay、不新增测试、不运行 Unity batchmode。修改前通过显式实例确认目标为主 3C_Client，退出 Play 后才写入代码。

ThirdPersonClient.Editor.csproj 编译通过，0 错误、91 警告；日志为 tmp/foot-release-target-build.log。使用 --disable-build-servers /nr:false /p:UseSharedCompilation=false，结束后已执行 dotnet build-server shutdown。编译不代表视觉效果通过。

Unity 实例 e852139597e42532 完成域重载后，反射确认 ConstrainReleaseTarget 与 ReleaseTargetSupport 已加载，项目路径为主 3C_Client，Edit 模式、不编译、不刷新；Console 错误查询返回 0 条。尚无修改后的运行采样。

Center 改动记录：修正释放阶段脚掌支撑目标，change_id=fad820d44e4640c591c085fb0260b605。用户要求不运行 replay 和 batchmode，本次不创建运行或前后对比，不把本机编译记录伪装成正式回放结果。
