# 项目配置与扩展

ShulkerRDK 的配置和脚本都放在项目根目录的 `./shulker/` 文件夹里：

```text
├── src/                      # 默认资源根目录
├── build/                    # 默认输出目录
└── shulker/
    ├── proj.json             # 项目配置 (建议提交到 Git)
    ├── tasks/*.lvt           # 任务脚本 (建议提交到 Git)
    └── local/                # 本地配置、下载的扩展和缓存 (建议加到 .gitignore)
```

> [!TIP]
> 把 `./shulker/local/` 加进 `.gitignore`, 只提交 `proj.json` 和 `tasks/` 就行

---

## 项目配置 (`./shulker/proj.json`)

```json
{
  "ProjectName": "My Project",
  "Version": "1.0.0",
  "RootPath": "./src/",
  "OutPath": "./build/",
  "DefaultEnvVars": {},
  "Extensions": [
    "gh:LiPolymer/ShulkerRDK#ShulkerRDK.ResourceMagick@B0.21",
    "gl:LiPolymer/ShulkerRDK#ShulkerRDK.Aseprite@B0.20"
  ]
}
```

- `ProjectName` / `Version` / `RootPath` / `OutPath`: 项目名、版本号、资源根目录、输出目录 (平时直接用 `proj` 和 `verm` 指令改就行, 不用手动编辑)
- `DefaultEnvVars`: 通过 `env set` 保存的项目环境变量和自定义别名
- `Extensions`: 项目需要自动拉取的扩展列表

---

## 安装与管理扩展

### 1. 在 `proj.json` 里声明（推荐）

在 `Extensions` 里按下面的格式填好扩展，下次启动 ShulkerRDK 时会自动下载装好：

```text
<平台>:<作者>/<仓库>#<扩展程序集名>@<Release标签>
```

- `<平台>` 支持 `gh` (GitHub) 和 `gl` (GitLab)
- 从 `Extensions` 里删掉某一项后, 下次启动也会自动把它清理掉

### 2. 手动放置

如果想用自己本地编译或单独下载的扩展包, 可以直接放到 `./shulker/local/extensions/<插件名>/` 目录下 (里面至少包含 `<插件名>.dll`, 依赖文件放同一目录即可)

> 手动放的扩展没有自动生成的 `lock.json`, 不会被上面的自动清理删掉。另外旧式的 `./shulker/extensions/*.dll` 放法也能加载, 不过启动时会提示方式已过时

---

## 用 `scripts/` 启动脚本免提交二进制

仓库里的 `scripts/` 目录带了三个小脚本 (`srdk`、`srdk.ps1`、`srdk.bat`), 适合想把项目丢进 Git 又不想传 `srdk.exe` 的场景：

- **Linux / macOS**: `srdk` (需要 `curl`/`wget` 和 `jq`/`python3`)
- **Windows**: `srdk.ps1` / `srdk.bat` (可以直接双击或命令行跑 `./srdk.bat ...`)

把脚本复制到自己项目的根目录, 运行时它会自动按脚本开头写的 `RELEASE_SOURCE_ID` (例如 `gl:LiPolymer/ShulkerRDK@B0.20`) 下载对应系统的 `srdk` 到 `./shulker/local/bin/`, 再把您传的命令行参数原样转交给它跑

> `scripts/` 里的这几个脚本单独采用 Apache 2.0 协议, ShulkerRDK 本体和第一方扩展则是 GPLv3 协议

---

## 本地终端设置 (`./shulker/local/shulker.json`)

```json
{
  "TerminalMode": "modern"
}
```

- `modern`: ANSI 彩色输出 (大多数现代终端都正常)
- `legacy`: 传统控制台颜色
- `mono`: 纯文本无颜色

在 CI/CD 环境里通常没有 `./shulker/local/shulker.json`。只要启动时带了命令行参数 (比如 `./srdk build`), ShulkerRDK 就会跳过首次的色彩交互引导直接跑起来, 不用担心卡住流水线
