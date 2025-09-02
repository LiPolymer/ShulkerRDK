# 从整合包模板开始

### 本模板包含:
- 一份 `.gitignore` 文件
- 必要的项目配置
- 基本的整合包项目结构
- 一些 LevitateTask 脚本
- ShulkerRDK 二进制文件
  - **ShulkerRDK** 本体 `srdk` `srdk.exe`
  - **ModrinthPSK** 扩展 `./shulker/extension/ShulkerRDK.Modrinth.dll`

*ShulkerRDK 和扩展均基于 [GPLv3](https://www.gnu.org/licenses/quick-guide-gplv3.zh-cn.html) 协议获得许可*

## 开始使用
#### 0. 获取[这个仓库](https://github.com/LiPolymer/ShulkerRDK.MPTemplate)的内容, 打开 ShulkerRDK
您可以直接下载,或克隆/分叉... *您怎么方便怎么来!*

下载完成后,您可以通过以下方式运行 ShulkerRDK 并进入交互模式

**双击 `srdk.exe` *( 仅限 Windows )*** 或 **在模板根目录打开终端,运行 `./srdk` *( Windows 用户请使用 PowerShell )***

 在交互模式中,您可以执行一些指令来对您的项目进行半自动操作
### 最初的配置
#### 1. 修改项目名称
这个模板的默认项目名称为 `Template` , 您应该将其修改为您自己的项目名称

您可以通过在交互模式中执行以下指令来修改项目名称 (这里假定您要修改为"My Project")
```
proj chname "My Project"
```
> [!IMPORTANT]
> 如果您准备使用此模板开启一个新项目, 您可能还需要手动修改 `pack.mcmeta` 文件

> [!TIP]
> 您可能注意到 `My Project` 是被包裹在一对引号中间的
>
> 这和 ShulkerRDK 解析指令和 Levitate 语句 的方式有关
>
> 这里 `proj chname <name>` 指令读取第三个参数作为项目名称, 若去掉引号, 则会导致第三个参数仅为 `My`, 而"多余"的第四个参数 `Project` 则并不会被使用
>
> 这一点在使用中较为重要,还请您 ~~理解并记忆~~ 注意
>
> 当然,如果您的项目名称不包含空格,您可以不使用引号包裹, 例如
> ```
> proj chname MyProject
> ```
#### 准备好您的项目
好的,现在请把目光移到我们的 `src` 文件夹

在这个模板中 `src` 文件夹被定义为 `资源根`, 这里存放的将会是您的项目的基本内容

我们已经为您提前准备了一份最最基本的 Minecraft 整合包项目目录结构, 您可以直接使用

> [!TIP]
> 您可以在 `mods` 文件夹内创建数个子目录来为您的模组分类, 在本模板中, 构建时 ShulkerRDK 会自动将里面的文件提取并整理放置到同一个文件夹中以便加载器能够正确识别

> [!NOTE]
> 如果您想开始一个新项目,您可以[跳过接下来的内容](#第一次构建)

**迁移到 ShulkerRDK**

1. 将您的整合包的模组文件放在 `./src/mods` 中
2. 将您的整合包的配置文件放在 `./src/config` 中
3. (可选) 打开 ShulkerRDK, 执行 `mrp s`, 以序列化托管于 Modrinth 的文件

#### 第一次构建
现在您可以使用以下命令来使用 ShulkerRDK 创建您项目
```
build
```
执行后, 检查 `build` 文件夹, 您应该可以看到您的项目已经被自动打包了

如果您已经将托管在 Modrinth 的文件序列化为 `.mrf` 文件 (在交互模式下执行 `mrp s`), 那么 ShulkerRDK 将会把这些文件写入构建出的 `.mrpack` 包的元数据内, 大幅缩小包体大小

如果您有需要导出离线包 (不从 Modrinth 下载文件), 请执行 `offline`, 这会生成一个包含所有需要的整合包文件的完整包

#### 配置开发环境
借助 ShulkerRRT, 您可以自动部署整合包到版本, 不过需要进行一些小小的配置

首先, 您需要安装适合您整合包的 Minecraft 版本(包括 Mod 加载器)

之后找到启动器的 `导出启动脚本` 功能 *(此处以 HMCL 为例, 选中目标版本, 从主页面依次点击 `版本管理 -> 管理 -> 生成启动脚本`)*, 导出为 `.ps1` (PowerShell 脚本格式) 到本仓库内容的 `./shulker/local/client.ps1`

完成后, 打开 `./shulker/tasks/settings.lvt`, 修改其中的 `X:\Path\To\Your\Game` 为您的游戏目录(请注意核对版本隔离情况)

完成后, 您可以尝试使用以下命令启动客户端
```
run
```

好了, 到现在, 您已经完成 ShulkerRDK 的配置了!

#### (可选)内置版本管理
如果您想使用 ShulkerRDK 内置的版本管理来管理您的项目, 下面是相关的指令:

展示版本号
```
verm show
```

设置版本号 (这里假定您要设为 `1.0.0`)
```
verm set 1.0.0
```

如果您要继续使用 `x.x.x` 格式的话,还可以使用下面的指令

步进修复版本 (`x.x.x.` -> `x.x.x+1`)
```
verm  sfix
```
步进小版本 (`x.x.x.` -> `x.x+1.x`)
```
verm sminor
```
步进大版本 (`x.x.x.` -> `x+1.x.x`)
```
verm smajor
```

#### 碎碎念
首先感谢您选择 ShulkerRDK! 🤗

这个小玩意花费了我不少精力, 希望你喜欢! 😋

由于这个玩意可自定义程度极高, 特性较多, 文档可能还需要一段时间, 于是先做了这个模板仓库, 让大家能先用上这些核心功能 😚

如果您在使用中遇到任何问题, 欢迎前往Discussion, 我们会在能力范围内尽可能帮助你!

再次感谢您的使用!