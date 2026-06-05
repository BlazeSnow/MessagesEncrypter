# MessagesEncrypter 项目指导

## 项目定位

本项目是一款 Windows 桌面端公钥消息加密工具，基于 WinUI 3 和 .NET 实现。用户使用接收方公钥加密消息，接收方使用自己的私钥和私钥密码解密消息。公钥由用户自行交换，不内置 PKI/CA 体系。

当前软件聚焦文本消息加密与解密。文件加密页面已暂时移除，后续重新设计后再加入。

本项目不是即时通讯工具，不应在 UI、文案或文档中暗示实时聊天能力。不兼容 PGP 时，不要命名为 PGP Tool 或暗示 PGP 格式兼容。

## 技术栈与项目配置

- UI 框架：WinUI 3
- 桌面运行时：Windows App SDK
- .NET 版本：.NET 10
- 目标框架：`net10.0-windows10.0.26100.0`
- 最低系统版本：Windows 10 `10.0.17763.0`
- 包分发：MSIX / Microsoft Store
- 加密库：优先使用 `System.Security.Cryptography`
- 设置控件：`CommunityToolkit.WinUI.Controls.SettingsControls`

项目使用单项目 MSIX 打包。`MessagesEncrypter.csproj` 中保留：

- `UseWinUI=true`
- `WinUISDKReferences=false`
- `PublishReadyToRun=False`
- `PublishTrimmed=True`
- `TrimMode=full`
- `SelfContained=true`

当前允许裁剪，但必须保持 WinUI、WinRT、System.Text.Json 和 SQLite 相关代码对裁剪友好。新增 JSON 类型时优先使用 System.Text.Json source generator，不要依赖反射序列化。不要重新开启 ReadyToRun，除非已经验证 Release 打包启动和 WinUI 资源加载没有问题。

CI 与 Visual Studio 发布应尽量共用 `MessagesEncrypter.csproj` 内的发布属性。架构由 `AppxBundlePlatforms` 控制，不要在 CI 中额外重复写死架构参数，避免 x64 / ARM64 发布配置不一致。

不需要主动进行编译或运行 build；如果 build 失败，用户会发送错误日志再处理。

## 本地化与文本资源

禁止在 XAML、C# 或其他 UI 相关代码中硬编码用户可见文本。所有用户可见文本必须通过 WinUI 3 的 `.resw` 资源文件管理。

适用范围包括：

- 页面标题、按钮、菜单、标签、占位符和工具提示。
- 对话框标题、正文、确认按钮和取消按钮。
- 错误提示、状态提示、进度提示和空状态文案。
- 设置项名称、说明文本和枚举选项显示名。
- 文件筛选器描述、剪贴板提示和通知文本。

实现要求：

- XAML 中优先使用 `x:Uid` 绑定 `.resw` 资源。
- 如果按钮内容需要图标加文字，按钮本身不要使用会覆盖 `Content` 的 `x:Uid`；应把 `x:Uid` 放到内部 `TextBlock` 上，并使用 `.Text` 资源。
- C# 中需要动态生成文案时，必须通过资源加载器读取 `.resw` 字符串。
- 资源 Key 命名应稳定、清晰，避免使用实际中文文本作为 Key。
- 新增 UI 文案时必须同步更新默认语言资源文件。
- 不允许为了临时开发方便把中文或英文文案直接写入控件属性、异常提示或业务逻辑。
- 日志、调试信息和不会展示给用户的内部常量不强制进入 `.resw`，但不得复用为用户可见文本。

## 编码与调试环境

开发和调试时需要注意目标系统可能是 GBK 系统。源码、`.resw`、Markdown 和配置文件仍应使用 UTF-8 编码读写，避免因 PowerShell、终端输出或日志查看器默认 GBK 导致中文乱码。

注意事项：

- 读取和修改项目文件时显式使用 UTF-8。
- 调试中文输出、异常信息、日志和命令行结果时，先判断是否存在 GBK/UTF-8 编码显示差异。
- 不要因为终端中看到乱码就直接改动资源文本，需先确认文件实际编码。
- 涉及外部进程、命令行参数、文件名和剪贴板文本时，需要验证中文字符在 GBK 系统上的表现。

## 当前功能范围

当前主要页面：

- 主页：用图示说明发送消息和接收消息流程。
- 消息加密：选择接收方公钥，输入明文，输出 Base64 JSON 密文包。
- 消息解密：选择我的私钥，输入密文包，输出明文。
- 接收方公钥：管理、导入、导出接收方公钥。
- 我的私钥：生成、导入、导出、删除私钥，并可导出对应公钥、修改私钥密码。
- 设置：管理导出位置、项目链接、反馈邮箱和软件版本显示。

当前不提供文件加密页面。后续重新加入文件加密时，应另行更新本文档和协议文档。

## UI 约定

- 主窗口使用 Mica 背景。
- 使用 WinUI 风格自定义标题栏，标题栏负责侧边栏折叠按钮。
- 侧边栏保留导航，不在侧边栏顶部重复显示软件名。
- 页面内部不再重复放置大标题，标题由导航和标题栏承担。
- 设置页和密钥管理页优先使用 `SettingsCard`。
- 设置页避免堆叠大量输入框，需要编辑时优先使用弹窗。
- 密钥列表使用 `SettingsCard` 展示别名、指纹和密钥类型，具体操作放在卡片右端的三点菜单中。
- 长耗时操作使用页面中央的 `ProgressRing`，不要用会被状态栏遮挡的底部进度条。
- 对话框需要跟随当前主题，深色模式下不能显示浅色弹窗。
- 常规按钮优先使用 `SymbolIcon` 加文字；当内置 Symbol 不足以表达语义时再使用 `FontIcon`。如图标含义不明显，应补充工具提示或明确按钮文字。
- 设置页显示软件版本时，应读取 `Package.Current.Id.Version`，不要硬编码版本号，也不要优先使用程序集版本。
- 发送和接收页面应记忆用户已选密钥。选择事件只应在真正选中新项时触发，不要在列表刷新或清空选择时清除已保存指纹。

## 存储与设置

当前存储分工：

- 应用设置：使用 `ApplicationData.Current.LocalSettings.Values`。
- 当前 LocalSettings 保存导出位置 `ExportFolderPath`。
- 当前 LocalSettings 保存加密/解密页已选密钥指纹：`SelectedRecipientKeyFingerprint`、`SelectedPrivateKeyFingerprint`。
- 密钥列表：保存到应用本地数据目录 `LocalState\keys.db`，使用 SQLite。
- 密钥存档完整性签名：保存到 `LocalState\keys.db.sig`。
- 完整性签名 HMAC 密钥：保存到 Windows 凭据管理器普通凭据，TargetName 为 `MessagesEncrypter.KeyStoreIntegrityKey`。
- 私钥密码：用户选择记住时保存到 Windows 凭据管理器普通凭据，按私钥指纹区分。

不需要额外实现 `settings.json`。小型设置优先放入 `LocalSettings`，密钥和较复杂数据继续使用 `LocalState` 下的 SQLite 数据库。

密钥数据库约定：

- 主表为 `keys`。
- `category` 区分接收方公钥和我的私钥。
- `(category, fingerprint)` 是唯一身份，不应依赖自增 ID。
- 列表展示按别名排序，再按指纹排序。
- 写入时应容忍旧数据中的重复指纹，迁移或保存不能因为重复主键直接中断。
- 旧 `keys.json` 仅用于一次性迁移，迁移成功后改名为 `keys.json.migrated`。

完整性校验约定：

- 启动读取密钥前需要校验 `keys.db.sig`。
- 缺少签名或签名不匹配时，不应直接加载密钥列表。
- 弹窗提供“忽略并重新签名”和“退出应用”。
- “忽略并重新签名”属于危险操作，应使用危险按钮样式。
- 应用自身创建表、迁移旧结构或迁移旧 JSON 后，必须重新签名，避免下次启动误报篡改。
- 完整性校验和重新签名应把 IO、权限、Base64、加密和凭据错误统一转为用户可理解的错误，不要让底层异常逃逸。

密钥存档目录应位于打包应用的 `LocalState`，例如：

```text
C:\Users\<User>\AppData\Local\Packages\BlazeSnow.MessagesEncrypter_<PackageId>\LocalState
```

导出位置默认是当前用户的下载文件夹。用户导出密钥后，应打开资源管理器并选中导出的文件；密钥管理页和设置页均可打开当前导出位置。

## 核心加密设计

禁止直接使用 RSA 加密长消息。RSA-OAEP 单次加密长度有限，必须采用混合加密：

1. 随机生成 AES-256-GCM 会话密钥。
2. 使用 AES-GCM 加密 UTF-8 明文。
3. 使用接收方 RSA 公钥以 OAEP-SHA256 加密 AES 会话密钥。
4. 打包输出加密后的会话密钥、nonce、tag 和密文。
5. 解密时先用 RSA 私钥恢复 AES 会话密钥，再用 AES-GCM 解密并验证完整性。

当前算法组合：

- 非对称加密：RSA + OAEP-SHA256
- 支持生成 RSA 位数：2048、3072、4096、8192
- 默认 RSA 位数：4096
- 最小接受 RSA 位数：2048
- 对称加密：AES-256-GCM
- 随机数：`RandomNumberGenerator`
- 完整性校验：AES-GCM tag

当前 V1 不支持 X25519、Ed25519 或其他非 RSA 算法。X25519 相关方向只记录在 `docs/protocol-v2.md`，不属于当前兼容承诺。

## 消息协议

当前正式协议文档是 `docs/protocol-v1.md`。

V1 使用 Base64 包裹 UTF-8 JSON，字段名为短字段：

```json
{
  "ver": 1,
  "ek": "<base64>",
  "nonce": "<base64>",
  "tag": "<base64>",
  "ct": "<base64>"
}
```

字段含义：

- `ver`：消息格式版本，当前固定为 `1`。
- `ek`：RSA-OAEP-SHA256 加密后的 32 字节 AES 会话密钥，Base64 编码。
- `nonce`：AES-GCM nonce，Base64 编码，原始长度 12 字节。
- `tag`：AES-GCM authentication tag，Base64 编码，原始长度 16 字节。
- `ct`：AES-GCM ciphertext，Base64 编码。

V1 不包含 `alg` 字段。未知字段应忽略，不应导致解密失败。为 `null` 的可选字段不应写入协议 JSON。

解密校验要求：

- `ver` 必须等于 `1`。
- 必须提供 `ek`、`nonce`、`tag`、`ct`。
- `ek`、`nonce`、`tag`、`ct` 必须是合法 Base64。
- `nonce` 解码后必须为 12 字节。
- `tag` 解码后必须为 16 字节。
- AES-GCM 认证失败时，必须统一视为解密失败。

禁止加密空白消息。明文为 `null`、空字符串或全空白字符时，应提示用户输入消息。

## 密钥管理

密钥格式：

- 公钥导出：PEM SPKI，形如 `-----BEGIN PUBLIC KEY-----`
- 私钥存储：加密 PKCS#8 PEM，形如 `-----BEGIN ENCRYPTED PRIVATE KEY-----`
- 公钥交换：`.pub` 文件或剪贴板文本
- 私钥导入：支持导入加密或未加密私钥，导入后统一转为加密 PKCS#8 PEM 保存

功能要求：

- 生成 RSA 密钥对时允许用户选择位数。
- 导入私钥后必须自动派生并保存对应公钥。
- 同一私钥派生出的公钥是确定的，不应每次变化。
- 支持多个接收方公钥和多个我的私钥。
- 支持密钥别名管理。
- 显示密钥指纹，指纹用于线下核对。
- 密钥类型在列表中显示，例如 `RSA4096`。
- 同一分类下密钥指纹不能重复，重复时应弹窗提示。
- 密钥列表按别名排序，不按创建时间或数据库 ID 排序。
- 导出密钥文件名只使用用户自定义别名，不附加指纹。
- 删除密钥和删除已保存密码前需要确认弹窗。
- 修改私钥密码时必须输入旧密码、新密码和确认新密码，本质是用旧密码解密私钥后再用新密码重新加密。
- 私钥必须加密存储，不能明文落盘。

私钥保护使用 .NET 原生 PKCS#8 加密导出能力：

```csharp
string pem = rsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters);
```

PBKDF2 迭代次数当前为 `600_000`。

## 文件加密后续方向

文件加密当前未实现。未来加入时应重新设计并补充独立协议文档。

初步要求：

- 文件可能达到 GB 级，必须支持流式或分块处理，不能一次性全部载入内存。
- 所有文件统一使用分块 AES-GCM。
- 分块大小允许用户选择，并写入文件 Header。
- 解密时根据格式魔数和 Header 自动识别文件格式与分块参数。
- 每块使用独立 12 字节随机 nonce。
- `chunk_index` 应作为 AES-GCM associated data 参与认证，防止块重排、删除或替换。
- Header 中记录总块数，解密时检查截断攻击。
- 所有文件读写使用异步 I/O，避免阻塞 UI 线程。

## 安全注意事项

- 每次加密必须生成新的 AES 会话密钥。
- 每次 AES-GCM 加密必须生成新的 nonce。
- 不要复用同一 nonce 和同一 AES key 的组合。
- 解密失败时不要输出部分明文。
- 解密异常需要统一为用户可理解的错误，不要泄漏内部堆栈。
- 当前密钥没有自动过期机制，生命周期由用户手动管理。
- 不支持前向安全性。静态私钥泄漏后，历史密文可能被解密；如未来需要前向安全，可考虑 X3DH / Double Ratchet 方案。

## 开发优先级

当前阶段优先维护：

1. 消息 V1 协议兼容性。
2. 密钥生成、导入、导出、删除和指纹显示。
3. 多接收方公钥和多私钥管理体验。
4. 私钥密码保存、查看、删除和修改流程。
5. WinUI 3 深浅色、标题栏、侧边栏、对话框和设置卡片体验。
6. MSIX / Microsoft Store 发布稳定性。

暂不展开文件加密，直到消息与密钥管理稳定。

## 测试重点

需要覆盖：

- 空消息、空白消息、短消息、长消息。
- 复杂 Unicode 文本内容，包括 Emoji 和多语言字符。
- 错误公钥、错误私钥、错误密码。
- 加密私钥和未加密私钥导入。
- 导入私钥后公钥派生和指纹稳定性。
- 密文被篡改。
- `ver` 不支持。
- `ek`、`nonce`、`tag`、`ct` 缺失。
- `nonce`、`tag` 长度异常。
- Base64 格式错误。
- AES-GCM 认证失败。
- 深色模式下所有弹窗和设置卡片显示。
- Release 打包启动，避免黑屏或资源加载失败。
- 单实例启动行为。
- 导出路径不存在、无权限或包含中文路径。
- 密钥别名包含文件名非法字符时的导出行为。
- 旧 `keys.json` 到 `keys.db` 的迁移。
- `keys.db` 缺少签名、签名不匹配、重新签名成功和重新签名失败。
- 旧数据库结构迁移后签名是否同步更新。
- LocalSettings 中保存的已选接收方/私钥指纹在重启后是否恢复，密钥删除后是否安全回退。
