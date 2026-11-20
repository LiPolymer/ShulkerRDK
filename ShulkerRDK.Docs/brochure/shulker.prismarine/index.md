# Prismarine - 新一代整合包管理后端

Prismarine (`shulker.prismarine`) 是基于 TridentCore 构建的整合包管理扩展

和 ModrinthPSK 一样, 它也用轻量化的 JSON 引用文件 (`.prf.json`) 代替大体积二进制文件提交到 Git, 并支持直接打包导出 **Trident**、**Modrinth (`.mrpack`)** 和 **CurseForge** 整合包

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
