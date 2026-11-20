# ShulkerRRT - 文件监视与实时重载

ShulkerRRT (`shulker.rrt`) 用于在开发资源包时监视文件变动, 并在修改保存后自动触发热部署脚本与游戏内资源包重载

需要配合客户端 Mod [ShulkerRRT](https://modrinth.com/project/DhnASaqg) 使用 *(安装后启动一次客户端, 将配置文件 `shulkerRRT.json` 中的 `isShulkerRDKManaged` 设为 `true`)*

---

## 命令 / 方法一览

```lvt
pw <start|stop> [事件过滤正则] [监视目录]    # 命令 & Levitate 方法: 启动或停止监视
rrt <目标资源包目录>                         # Levitate 方法: 触发游戏重载
```

---

## 详情

### pw - 文件监视

```lvt
pw start [事件过滤正则] [监视目录]
pw stop
```

- `监视目录` 不填时默认为项目资源根目录 (`%project.src%`)
- 检测到符合过滤条件的文件变动后, 会注入 `%rrt.file%` 环境变量并自动运行 `./shulker/tasks/hotDeploy.lvt`
- `%rrt.file%` 的格式为 `<事件类型>|<文件路径>`:
  - `created|<路径>`、`changed|<路径>`、`deleted|<路径>`、`renamed|<新路径>|<旧路径>`

> [!TIP]
> 忽略删除事件以及 Photoshop (`.tmp`) / Krita (`.psd~`) 的临时文件, 可以这样写过滤正则:
> ```text
> ^(?!deleted).*(?<!\.tmp|\.psd~)$
> ```

---

### rrt - 触发重载

在 `<目标资源包目录>` 下写入 `reload.flag`, 通知客户端中的 ShulkerRRT Mod 立即重载资源包:

```lvt
rrt "X:/Path/To/Game/resourcepacks/debugging"
```

---

## 配合示例

**`./shulker/tasks/dev.lvt`:**
```lvt
import shulker.rrt

pw start "^(?!deleted).*(?<!\.tmp|\.psd~)$" "%project.src%"
```

**`./shulker/tasks/hotDeploy.lvt`:**
```lvt
import shulker.rrt
import shulker.magick

echo "文件变更: %rrt.file%"
var file {list get "%rrt.file%" 1}
check {regex "\.psd$" match "^file^"} psdcvt "^file^" "{path remapper "%project.src%" "^file^" "X:/Path/To/Game/resourcepacks/debugging" "png"}"
rrt "X:/Path/To/Game/resourcepacks/debugging"
```
