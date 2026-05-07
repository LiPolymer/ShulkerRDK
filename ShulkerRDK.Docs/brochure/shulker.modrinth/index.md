# ModrinthPSK - Modrinth 平台支持

ModrinthPSK 是 ShulkerRDK 的 Modrinth 平台集成扩展, 用于管理托管在 [Modrinth](https://modrinth.com) 上的资源文件

通过将大体积的 Mod/资源包文件替换为小型 `.mrf` (Modrinth Resource File) 引用文件, 可以大幅减小 Git 仓库体积并减少二进制文件数量

## .mrf 文件格式

`.mrf` 是一个 JSON 文件, 包含 Modrinth 版本 ID 和 SHA1 哈希:

```json
{
  "VersionId": "p96k10UR",
  "Sha1": "1c7871b6af04edc8b8f0dbad12606d67f6118a11",
  "ServerSide": null,
  "ClientSide": null,
  "Locked": false
}
```

- `VersionId` - Modrinth 版本 ID, 用于还原时定位文件
- `Sha1` - 文件的 SHA1 哈希值
- `ServerSide` / `ClientSide` - 服务端/客户端兼容性 (可选)
- `Locked` - 锁定状态, 为 `true` 时 `mrp update` 会跳过此资源



## 命令一览

```lvt
mrp serialize [源目录] [输出目录]     (或 mrp s)    # 序列化文件为 .mrf 引用
mrp restore  [源目录] [输出目录]      (或 mrp r)    # 还原 .mrf 为真实文件
mrp export   [源目录] [输出目录]      (或 mrp e)    # 构建 .mrpack 索引
mrp add      <URL或ID> [版本] [目录]  (或 mrp a)    # 添加新资源
mrp add -f   <文件路径> [目录]        (或 mrp a -f) # 批量添加资源
mrp update   [目录]                   (或 mrp u)    # 更新到最新版本
mrp lock     <部分文件名>                          # 锁定不被更新
mrp unlock   <部分文件名>                          # 解锁允许更新
mrp clean                                       # 清理本地缓存
```



### 序列化 (serialize)

将本地文件转换为 `.mrf` 引用文件:

```lvt
mrp serialize [源目录] [输出目录]     (或 mrp s)
```

**工作原理:**
1. 扫描目录中的所有文件, 计算 SHA1 哈希
2. 批量查询 Modrinth API 识别匹配的文件
3. 为每个匹配的文件创建 `.mrf` JSON 引用文件
4. 将原始文件缓存到 `./shulker/local/mrf/`

**示例:**
```
>mrp s
正在编入[./src/mods]
正在与Modrinth通讯... [42]个文件
完成!
```



### 还原 (restore)

将 `.mrf` 引用文件还原为真实的 Modrinth 资源文件:

```lvt
mrp restore [源目录] [输出目录]       (或 mrp r)
```

还原时会优先从本地缓存 (`./shulker/local/mrf/`) 读取, 缓存未命中时从 Modrinth 下载



### 导出 (export)

构建 `.mrpack` 整合包元数据索引:

```lvt
mrp export [源目录] [输出目录]        (或 mrp e)
```

会读取所有 `.mrf` 文件, 查询 Modrinth 获取项目元数据, 生成符合 Modrinth 整合包规范的 `modrinth.index.json`



### 添加资源 (add)

通过 Modrinth URL、项目 ID 或 slug 添加新资源:

```lvt
mrp add <URL或ID> [版本号] [输出目录]     (或 mrp a)
```

**支持的输入格式:**
- Modrinth URL: `https://modrinth.com/mod/sodium`
- 带版本的 URL: `https://modrinth.com/mod/sodium/version/abc123`
- 项目 slug: `sodium`
- 项目 ID: `AflYRJLH`

**版本指定方式:**
- 不指定版本: 自动选择最新 Release 版本 (基于 `mrpack.template.json` 中的 loader/gameVersion 过滤)
- 指定版本号: `mrp add sodium 0.6.13`
- URL 中包含版本: `mrp add https://modrinth.com/mod/sodium/version/abc123`

**输出目录:**
- 默认根据项目类型自动选择: Mod -> `src/mods`, Shader -> `src/shaderpacks`, Resourcepack -> `src/resourcepacks`
- 手动指定: `mrp add sodium src/mods-custom`

**示例:**
```
>mrp a sodium
Modrinth 添加资源
正在获取项目信息 [sodium]
Sodium (Mod)
版本 Sodium 0.6.13 (0.6.13)
正在下载 sodium-fabric-0.6.13+mc1.21.4.jar
已添加 Sodium @0.6.13 -> src/mods/sodium-fabric-0.6.13+mc1.21.4.jar.mrf

>mrp a fabric-api 0.119.4+1.21.4
添加指定版本的 Fabric API

>mrp a https://modrinth.com/resourcepack/fresh-animations
添加资源包
```

> [!TIP]
> 版本过滤信息从 `./shulker/mrpack.template.json` 的 `dependencies` 字段读取:
> ```json
> {
>   "dependencies": {
>     "minecraft": "1.21.4",
>     "fabric-loader": "0.18.6"
>   }
> }
> ```



### 批量添加 (add -f)

从文件批量添加资源:

```lvt
mrp add -f <文件路径> [输出目录]       (或 mrp a -f)
```

**文件格式 (每行一个, `#` 开头为注释):**
```
# 需要添加的 mod
sodium
fabric-api
https://modrinth.com/mod/iris
sodium 0.6.13
```

每行支持 `<slug或URL> [版本号]` 格式, 空行会被跳过



### 更新资源 (update)

将现有 `.mrf` 资源更新到最新 Release 版本:

```lvt
mrp update [目录]                    (或 mrp u)
```

**工作原理:**
1. 扫描目录中所有 `.mrf` 文件
2. 批量查询 Modrinth 获取当前版本信息
3. 对每个项目获取最新 Release 版本列表
4. 版本不同时下载新文件并更新 `.mrf`

**示例:**
```
>mrp u
Modrinth 更新资源
找到 [15] 个资源文件
正在更新 [Sodium] 0.6.12 -> 0.6.13
正在更新 [Fabric API] 0.119.2 -> 0.119.4
完成! 已更新 [2] 个资源
```

> [!NOTE]
> - 只会更新到 Release 版本, 不会更新到 Beta/Alpha
> - 版本过滤基于 `mrpack.template.json` 中的依赖配置
> - 已锁定 (`Locked: true`) 的资源会被跳过



### 锁定/解锁 (lock/unlock)

锁定资源使其不被 `mrp update` 更新:

```lvt
mrp lock <部分文件名>             # 锁定
mrp unlock <部分文件名>           # 解锁
```

**匹配方式:** **按文件名** - 匹配文件名包含输入内容的 `.mrf` 文件

**示例:**
```
>mrp lock sodium-fabric-0.6.13+mc1.21.4.jar.mrf
已锁定 src/mods/sodium-fabric-0.6.13+mc1.21.4.jar.mrf
完成! 已锁定 [1] 个资源

>mrp lock sodi
已锁定 src/mods/sodium-fabric-0.6.13+mc1.21.4.jar.mrf
完成! 已锁定 [1] 个资源

>mrp unlock sodium
已解锁 src/mods/sodium-fabric-0.6.13+mc1.21.4.jar.mrf
完成! 已解锁 [1] 个资源
```



### 清理缓存 (clean)

删除本地 MRF 缓存目录:

```lvt
mrp clean
```

缓存位于 `./shulker/local/mrf/`, 删除后 `mrp restore` 会重新从 Modrinth 下载文件



## 在 Levitate 脚本中使用

`mrp` 同样可以在 Levitate 脚本中作为方法调用:

```lvt
mrp s src/mods          # 序列化
mrp r src/mods          # 还原
mrp e src/mods build    # 导出
mrp a sodium            # 添加
mrp u src/mods          # 更新
```
