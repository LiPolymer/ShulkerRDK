# ShulkerRDK

[English](README.md) · [文档站](https://docs.lipoly.ink/ShulkerRDK/)

*面向 Minecraft 的模块化低代码 SDK 与资源装配工具*

<p>
  <img src="https://lipoly.ink/assets/badges/humanity.svg" alt="This project was built by human" width="200">
  <img src="https://lipoly.ink/assets/badges/llmDisclosure.svg" alt="This project contains human-audited ai generated code" width="200">
</p>

## 概述

ShulkerRDK 是一个专为 Minecraft 资源包与整合包开发设计的模块化命令行工具。它把交互式指令、任务脚本语言（**Levitate Tasks**，`.lvt`）以及按需载入的扩展系统结合在一起，用来简化贴图/音频转换、资源引用管理、打包和 CI/CD 自动构建等重复工作。

## 快速开始

### 从项目模板开始

如果您想直接套用配置好的工作流，可以从模板仓库开始：

- [资源包模板 (`ShulkerRDK.RPTemplate`)](https://github.com/LiPolymer/ShulkerRDK.RPTemplate)
- [整合包模板 (`ShulkerRDK.MPTemplate`)](https://github.com/LiPolymer/ShulkerRDK.MPTemplate)

### 单独使用或搭配启动脚本

- 从 [GitHub Releases](https://github.com/LiPolymer/ShulkerRDK/releases) 或 [GitLab Releases](https://gitlab.com/LiPolymer/ShulkerRDK/-/releases) 下载对应平台的可执行文件；
- 或者把 [`scripts/`](scripts/) 目录下的跨平台 Wrapper 脚本（`srdk`、`srdk.ps1`、`srdk.bat`）放到项目根目录，运行时会自动下载对应版本的可执行文件，无需把二进制提交进 Git 仓库。

在空目录运行 `./srdk` 即可按提示初始化 `./shulker/proj.json`，也可以直接在命令行传参运行任务（例如 `./srdk build`，适合 CI/CD 环境）。

### 简单示例

在 `./shulker/proj.json` 中声明需要使用的扩展（启动时自动从 GitHub / GitLab 拉取）：

```json
{
  "ProjectName": "My Pack",
  "Version": "1.0.0",
  "RootPath": "./src/",
  "OutPath": "./build/",
  "Extensions": [
    "gh:LiPolymer/ShulkerRDK#ShulkerRDK.ResourceMagick@B0.21"
  ]
}
```

在 `./shulker/tasks/build.lvt` 中编写构建脚本，随后在交互模式输入 `build` 或在终端运行 `./srdk build`：

```lvt
import shulker.magick

makeCleanup
makeCopy
psdCvt
makePkg
```

更多细节与完整指令参考请前往 [ShulkerRDK 文档站](https://docs.lipoly.ink/ShulkerRDK/)。

## 扩展一览

主程序内置了核心扩展 `shulker.core`（项目管理、版本号步进、打包/解包、`.nfm` 网络文件与 Levitate 基础方法），其他功能由以下第一方扩展按需提供：

| 扩展名称 | 扩展 ID | 说明 |
| :--- | :--- | :--- |
| **ModrinthPSK** | `shulker.modrinth` | 将 Modrinth 资源转为轻量化 `.mrf` 引用文件，支持添加、更新、锁定与生成 `modrinth.index.json` |
| **Prismarine** | `shulker.prismarine` | 基于 TridentCore 的新一代整合包后端（`.prf.json` / `pref://`、Modrinth 搜索、导出 Trident/Modrinth/CurseForge 包） |
| **ResourceMagick** | `shulker.magick` | PNG / PSD 互转、PBR 贴图提取（`_n`/`_s`）与 Java 资源包跨版本迁移（`mrvc`） |
| **AsepriteExtractor** | `shulker.ase` | `.aseprite` 转 PNG，支持通过图层 `#` 标签一次导出多套变体 |
| **ShulkerFFmpeg** | `shulker.ffmpeg` | 批量将音频转换为 OGG Vorbis，支持单声道下混 |
| **ShulkerRRT** | `shulker.rrt` | 监听文件改动并触发游戏内资源包实时重载（需配合 [ShulkerRRT Mod](https://modrinth.com/project/DhnASaqg)） |

## 从源码构建

需要 **.NET 10 SDK**，并拉取 `submodules/Trident.Net` 子模块：

```bash
git clone --recursive https://github.com/LiPolymer/ShulkerRDK.git
cd ShulkerRDK
dotnet build ShulkerRDK.slnx
```

## 许可证

- 本项目（本体及第一方扩展）除 `scripts` 目录内的 Wrapper 以外，均基于 **GPLv3** 协议许可（见 [`LICENSE`](LICENSE)）。
- [`scripts/`](scripts/) 目录内的 Wrapper 脚本基于 **Apache 2.0** 协议许可（见 [`scripts/LICENSE`](scripts/LICENSE)）。
- `ShulkerRDK.ResourceMagick` 中的资源包跨版本迁移规则与数据（`mrvc`）基于 [agentdid127/ResourcePackConverter](https://github.com/agentdid127/ResourcePackConverter) 改写，遵循 **MIT** 协议（见 [`ResourcePackConverter.LICENSE.txt`](ShulkerRDK.ResourceMagick/VersionConverter/ResourcePackConverter.LICENSE.txt)）。
