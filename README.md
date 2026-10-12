# 遗迹工坊 Unity 2D 战斗作品集

俯视角单机战斗 Demo，包含远程修复师与近战盾卫、三关怪潮、技能组合、精英敌人和滚动地图。仓库提供完整 Unity 源工程与学习文档。

## 演示与下载

- [演示视频 MP4](https://github.com/lanyun1st/shixi1/releases/download/v1.0.0-demo/RuinsPortfolio-Demo.mp4)
- [Windows 64 位可运行版 v1.0.1](https://github.com/lanyun1st/shixi1/releases/download/v1.0.1-hurtbox/RuinsPortfolio-Windows-v1.0.1.zip)
- [完整源码压缩包 v1.0.1](https://github.com/lanyun1st/shixi1/releases/download/v1.0.1-hurtbox/UnityPortfolio-Source-v1.0.1.zip)
- [版本说明和全部附件](https://github.com/lanyun1st/shixi1/releases/tag/v1.0.1-hurtbox)

v1.0.1 修复怪物受击区域：身体受击与脚底移动碰撞分离，统一子弹、近战与范围伤害判定，补充 36 项受击回归检查。详见 [修复说明](Docs/受击判定修复说明.txt)。上方视频保留的是 v1.0.0 修复前演示。

视频作为 Release 附件提供，可能需要下载后播放。游戏无需安装 Unity：完整解压后双击 `RuinsPortfolio.exe`，保留同目录全部依赖，不要只复制 EXE。

## 操作

WASD 移动，鼠标瞄准，左键攻击，右键蓄力或举盾，Space 冲刺，E 与传送门交互，Esc 暂停，R 重新开始。

远程角色围绕元素、穿透与连锁构建技能组合；近战角色围绕举盾反击、连击与冲刺形成独立玩法。

## 开发环境

使用 **Unity 2022.3.62f2c1**，精确版本见 `ProjectSettings/ProjectVersion.txt`。通过 Unity Hub 添加仓库根目录，等待导入，打开 `Assets/Scenes/Ruins.unity`，点击 Play。

工程包含素材、中文字体、配置、场景和 UI 预制体，不需要原开发电脑的缓存或下载目录。首次安装、许可证激活和包解析可能需要网络。当前以 Windows 键鼠为验证目标，其他平台需要另行适配和验证。

## 技术与代码入口

- 技能配置：`Assets/Scripts/SkillDefinition.cs`、`SkillCatalog.cs`。
- 技能状态与联动：`Assets/Scripts/Domain/SkillLoadout.cs`、`SynergyRules.cs`。
- 对象池：`Assets/Scripts/Domain/LeasePool.cs`、`RuinsGame.Pooling.cs`。
- 敌人状态机：`Assets/Scripts/Domain/EliteChargeBrain.cs`、`SentryBrain.cs`。
- 波次调度：`Assets/Scripts/Domain/WaveDirector.cs`、`RuinsGame.Waves.cs`。
- UI 状态快照与命令：`Assets/Scripts/UI/GameUiContract.cs`。
- 伤害反馈与表现分离：`Assets/Scripts/RuinsGame.Reactions.cs`、`ReactionFeedback.cs`。
- 回归测试：`Assets/Editor/PortfolioRuleTests.cs`、`PortfolioSmokeTest.cs`。

部分技能数值通过 ScriptableObject 配置，复杂行为仍由 C# 实现；partial 文件仍共享同一个玩法协调类。测试采用自定义 Editor 入口，并非 NUnit 测试套件。

## 学习文档

- [项目代码学习指南](Docs/项目代码学习指南.docx)
- [技术栈与跨设备迁移说明](Docs/技术栈与跨设备迁移说明.docx)
- [独立运行验证记录](Docs/独立运行验证记录.txt)

## 回归验证

在独立工程副本中运行，替换为本机 Unity 路径，先关闭该副本的 Unity 窗口。

```powershell
.\Run-Tests.ps1 -Suite Rules -UnityPath "D:\Unity\Editor\Unity.exe"
.\Run-Tests.ps1 -Suite Gameplay -UnityPath "D:\Unity\Editor\Unity.exe"
```

Windows 构建入口为 `PortfolioPlayerBuild.Windows`，需要 Windows Build Support，命令示例见验证记录。测试可能准备配置或输出检查画面。

已完成本机无旧缓存导入、玩法回归，以及最终游戏包解压后的两角色独立启动检查。独立启动检查覆盖角色选择、起始技能、怪物生成、暂停恢复，不等于人工完整通关或所有设备性能保证。

## 素材说明

开发与美术制作包含 AI 协作。思源黑体许可证保留在 `Assets/Resources/Fonts/LICENSE.txt`。公开展示不代表所有素材均以开源许可证授权，复用前请分别确认相关权利。
