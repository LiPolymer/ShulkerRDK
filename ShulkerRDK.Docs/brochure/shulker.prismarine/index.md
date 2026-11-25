# Prismarine - 新一代整合包管理后端

Prismarine (`shulker.prismarine`) 是基于 TridentCore 构建的整合包管理与调试实例扩展

和 ModrinthPSK 一样, 它用轻量化的 JSON 引用文件 (`.prf.json`) 代替大体积二进制文件提交到 Git, 支持直接打包导出 **Trident**、**Modrinth (`.mrpack`)** 和 **CurseForge** 整合包, 同时内置项目隔离的 **Minecraft 调试实例管理 (`inst`)**

---

## 开始之前

Prismarine 默认通过 `./shulker/trident.profile.json` 控制 Minecraft 版本与加载器筛选条件 *(首次使用如果不存在会自动创建一份默认配置)*：

```json
{
  "Name": "My Modpack",
  "Setup": {
    "Version": "1.21.4",
    "Loader": "fabric"
  }
}
```

> [!NOTE]
> `Setup.Loader` 同时支持**简写**与**完整 Trident LURL**：
> - 简写：如 `fabric`、`neoforge`、`forge`、`quilt` *(自动解析推荐/最新版本)*，或附带版本号如 `fabric:0.16.10`
> - 完整 LURL：如 `net.fabricmc:0.16.10`、`net.neoforged:21.4.0`
> - 原版游戏可留空或填 `vanilla` / `none`

### .prf.json 与 `pref://` 格式

```json
{
  "Pref": "pref://modrinth/AflYRJLH@p96k10UR",
  "Sha1": "1c7871b6af04edc8b8f0dbad12606d67f6118a11",
  "FileName": "sodium-fabric-0.6.13+mc1.21.4.jar",
  "Type": "Mod",
  "Locked": false,
  "Enabled": true
}
```

`pref://` 是 TridentCore 使用的包引用格式 (比如 `pref://modrinth/sodium` 或带指定版本的 `pref://modrinth/AflYRJLH@p96k10UR`), 也兼容旧式 PURL 字符串

---

## 命令一览

```lvt
pfm search    <关键词> [MC版本] [加载器]       (或 pfm f)   # 在 Modrinth 搜索资源
pfm create    <Pref/PURL> [输出目录]          (或 pfm c)   # 创建 .prf.json 引用文件
pfm serialize [目录] [--keep]                (或 pfm s)   # 扫描并识别现有包为 .prf.json
pfm solidify  [输入路径] [输出目录] [--keep]   (或 pfm r)   # 还原 .prf.json 为真实文件
pfm update    [输入路径]                      (或 pfm u)   # 更新 .prf.json
pfm lock      <文件或目录>                    (或 pfm l)   # 锁定不被更新
pfm unlock    <文件或目录>                    (或 pfm ul)  # 解锁允许更新
```

### 常用说明

- **搜索并添加**:
  ```
  >pfm f sodium 1.21.4 fabric
  >pfm c pref://modrinth/sodium ./src/mods
  ```
- **序列化现有文件 (`pfm s`)**: 扫描目录下的 `.jar`、`.zip`、`.disabled`、`.ban` 文件并识别生成 `.prf.json`。默认会删掉原文件, 加 `--keep` 可保留
- **还原文件 (`pfm r`)**: 批量解析下载并还原 `.prf.json` (支持单文件或目录)。默认会删掉原 `.prf.json`, 加 `--keep` 可保留
- **更新 (`pfm u`)**: 检查未锁定的 `.prf.json` 并更新到最新匹配版本, 若文件名发生变化会自动顺带重命名 `.prf.json`

> [!NOTE]
> 在交互模式中, `serialize`、`solidify` 和 `update` 不传路径时默认对 `%project.src%` 操作

---

## 在 Levitate 脚本中打包导出

在 `.lvt` 脚本中导入 `import shulker.prismarine` 后, 除了能用 `serialize`、`solidify` (`restore`)、`update`、`lock`、`unlock` 之外, 还可以用 `index` 和 `export` 直接构建整合包：

```lvt
import shulker.prismarine

makeCleanup
makeCopy
pfm index "%project.cache%"
pfm export modrinth "%project.cache%" "%project.output%%project.name%_%project.ver%.mrpack"
makeCleanup
```

- `pfm index [目标目录]`: 收集目标目录 (默认 `%project.cache%`) 下的所有 `.prf.json`, 结合 `./shulker/trident.profile.json` 生成 `profile.json`
- `pfm export <格式> <目标目录> <输出文件>`: 读取 `profile.json` 与 `import/` 里的覆盖文件并打包输出。`<格式>` 支持 `trident`、`modrinth`、`curseforge`

---

## 调试实例管理 (`inst`)

Prismarine 内置轻量级 Minecraft 调试实例管理, 方便在开发整合包或资源包时一键拉起本地测试客户端, 无需依赖外部启动器或硬编码本机路径

### 隔离存储、账号与运行时

- **项目级隔离存储**: 实例文件、资源缓存与运行时默认保存在 `./shulker/local/trident/` *(位于 `shulker/local/` 下, 默认已被 Git 忽略, 不污染仓库)*。如果外部已设置环境变量 `TRIDENT_HOME`, 则优先沿用外部目录
- **本地启动配置**: 默认实例、内存、窗口尺寸与 Java 覆盖等个人设置保存在 `./shulker/local/prismarineLocal.json` *(可通过 `inst config` 与 `inst java` 管理)*
- **离线开发账号**: 默认使用离线账号 `DevPlayer` *(自动生成稳定的离线 UUID)*, 可通过 `inst config set username <名称>` 或启动时传入 `--user <名称>` 修改
- **自动 Java 管理**: 部署与启动时默认自动获取 Mojang 官方对应版本的内置 Java 运行时；如需使用本地 JDK, 可用 `inst java scan` 自动扫描或 `inst java set [主版本] <路径>` 手动指定
- **启动模式**:
  - **默认 (`Managed` 前台托管)**: 在当前终端实时输出游戏日志；按下 `Ctrl+C` 会优雅停止游戏并返回 ShulkerRDK 命令行, **不会退出 ShulkerRDK**
  - `--bg`: 后台启动 (`FireAndForget`), 立即返回命令行 *(可用 `inst stop` 或 `inst detach` 后续控制)*
  - `--debug`: 输出启动命令与环境诊断信息 (Launch Dump)

### `inst` 命令一览

> [!NOTE]
> 所有省略 `[key]` 的命令都会作用于当前默认实例 *(初始为 `dev`, 可用 `inst use <key>` 切换)*

| 命令 | 简写 | 用法 | 说明 |
| :--- | :--- | :--- | :--- |
| `create` | `c` | `inst create <key> [mcVer] [loader]` | 创建新实例 *(省略版本与加载器时继承 `trident.profile.json`)* |
| `ensure` | `e` | `inst ensure [key] [mcVer] [loader]` | 幂等确保实例存在 *(已存在则跳过, 适合脚本)* |
| `list` | `ls` | `inst list` | 列出所有调试实例及运行状态 |
| `info` | `i` | `inst info [key]` | 查看实例版本、加载器、包列表与目录 |
| `use` | - | `inst use <key>` | 切换默认实例 (`DefaultInstance`) |
| `sync` | - | `inst sync <packDir> [key]`<br>`inst sync import <srcDir> [key] [--clean]`<br>`inst sync rp <src> [packName] [key]` | 同步整合包目录 (`profile.json` + `import/`)、覆盖文件, 或热同步资源包到 `build/resourcepacks/` |
| `pkg` | - | `inst pkg <add\|remove\|list> [key] [pref]` | 增删查实例依赖包 *(省略 `[key]` 时直接作用于默认实例)* |
| `deploy` | `d` | `inst deploy [key] [--full-check]` | 下载资源、安装加载器并部署实例 |
| `launch` | `l` | `inst launch [key] [--bg\|--debug] [--server <addr>] [--memory <mb>] [--java <path>] [--user <name>]` | 启动已部署的实例 |
| `run` | `r` | `inst run [key] [--bg\|--debug] [--full-check] [--server <addr>] [--memory <mb>] [--java <path>] [--user <name>]` | 执行 `deploy` 后直接启动实例 |
| `stop` | - | `inst stop [key]` | 停止运行中的实例 |
| `detach` | - | `inst detach [key]` | 解除托管, 让游戏进程独立继续运行 |
| `import` | - | `inst import <packFile> [key]` | 从 `.mrpack` / Trident / CurseForge / MultiMC / Packwiz 包导入实例 |
| `reset` | - | `inst reset [key]` | 清空 `build/` 与锁文件以重新部署 *(保留 `persist/` 存档与配置)* |
| `delete` | `rm` | `inst delete <key> [--yes]` | 删除实例 *(加 `--yes` 跳过确认)* |
| `dir` | - | `inst dir <home\|build\|import\|persist\|rp\|mods> [key]` | 输出实例指定目录的完整路径 |
| `config` | `cfg` | `inst config [show \| set <字段> <值> \| unset <字段>]` | 查看或修改本地配置 *(支持 `instance`/`default`、`username`/`user`、`uuid`、`memory`/`mem`、`width`、`height`、`java`、`server`、`args`、`wrapper`、`cfkey`)* |
| `java` | - | `inst java [scan \| list \| set [主版本] <路径> \| clear [主版本]]` | 扫描、查看或覆盖本地 Java 运行时路径 |

---

## Levitate (`.lvt`) 实战示例

在 `.lvt` 脚本中导入 `import shulker.prismarine` 后, 可直接调用 `inst` 将实例准备、同步与启动串进项目任务

### 整合包开发一键运行

**`./shulker/tasks/run.lvt`:**
```lvt
import shulker.prismarine

makeCleanup
makeCopy
pfm index "%project.cache%"
inst sync "%project.cache%"
inst run
makeCleanup
```

执行后会自动将源码中的 `.prf.json` 与覆盖文件索引并增量同步到默认实例 (`dev`), 部署更新后前台拉起游戏, 同时保留 `persist/` 里的测试存档与设置

### 资源包开发 + ShulkerRRT 零路径硬编码热重载

结合 [ShulkerRRT](../shulker.rrt/index.md), 无需再手写本机 `.minecraft` 路径, 团队成员克隆项目后即可开箱调试：

**`./shulker/tasks/dev.lvt`:**
```lvt
import shulker.prismarine
import shulker.rrt

# 幂等准备调试实例与热重载依赖
inst ensure dev 1.21.4 fabric
inst pkg add dev pref://modrinth/fabric-api
inst pkg add dev pref://modrinth/DhnASaqg
inst deploy dev

# 同步项目资源包到实例的 resourcepacks 目录
inst sync rp "%project.src%" debugging dev

# 启动文件监视 -> 前台运行游戏 (Ctrl+C 优雅退出) -> 游戏结束后停止监视
pw start "^(?!deleted).*(?<!\.tmp|\.psd~)$" "%project.src%"
inst launch dev
pw stop
```

**`./shulker/tasks/hotDeploy.lvt`:**
```lvt
import shulker.prismarine
import shulker.rrt
import shulker.magick

var rp "{inst dir build dev}/resourcepacks/debugging"
echo "文件变更: %rrt.file%"
var file {list get "%rrt.file%" 1}
check {regex "\.psd$" match "^file^"} psdcvt "^file^" "{path remapper "%project.src%" "^file^" "^rp^" "png"}"
rrt "^rp^"
```

> [!TIP]
> 也可以用 `{inst dir rp dev}/debugging` 简写目标资源包目录。首次使用 ShulkerRRT 客户端 Mod 时, 记得将实例配置 `shulkerRRT.json` 中的 `isShulkerRDKManaged` 设为 `true` *(也可以放到 `import/config/` 下随实例自动部署)*

### Levitate 表达式返回值助手

以下 `inst` 子命令在 `{...}` 表达式中会返回实用值, 方便与 `var`、`check`、`path` 等方法组合：

| 表达式 | 返回值说明 |
| :--- | :--- |
| `{inst dir <home\|build\|import\|persist\|rp\|mods> [key]}` | 返回实例对应目录的完整路径 *(如 `{inst dir build dev}`、`{inst dir rp dev}`)* |
| `{inst exists [key]}` | 判断实例是否存在, 返回 `"true"` 或 `"false"` |
| `{inst running [key]}` | 判断实例是否正在运行, 返回 `"true"` 或 `"false"` |
| `{inst current}` | 返回当前默认实例名称 (`DefaultInstance`) |

*(此外, `ensure`、`create`、`import` 会返回实例 `key`, `sync rp` 会返回同步后的目标资源包路径)*
