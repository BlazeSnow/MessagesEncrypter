# AGENTS.md

MessagesEncrypter：Windows 桌面公钥消息加密工具（WinUI 3 + .NET 10，MSIX / Microsoft Store 分发）。用接收方公钥加密文本消息，用私钥和私钥密码解密。

详细开发指南见 [DEVELOPMENT.md](./DEVELOPMENT.md)，协议原文见 [docs/protocol-v1.md](./docs/protocol-v1.md)。

## 红线

- 不是即时通讯工具，UI、文案、文档不得暗示实时聊天；不兼容 PGP，不得命名或暗示 PGP 兼容。
- 禁止硬编码用户可见文本，一律走 `.resw`（XAML 用 `x:Uid`，C# 用资源加载器）。
- 保持裁剪友好（`TrimMode=full`）：JSON 用 System.Text.Json source generator，不开 ReadyToRun。
- 禁止 RSA 直接加密长消息，必须混合加密：AES-256-GCM 加密明文 + RSA-OAEP-SHA256 加密会话密钥。
- 私钥必须以加密 PKCS#8 PEM 存储，不能明文落盘。
- 每次加密生成新 AES 会话密钥和新 nonce；解密失败不得输出部分明文。
- 源码、`.resw`、Markdown、配置文件统一 UTF-8；目标系统可能是 GBK，终端乱码先查编码再改文件。
- 不需要主动编译或 build；build 失败时用户会发送错误日志。

## 关键架构

- 协议实现：`MessagesEncrypter.Protocol.V1`，独立项目，不得依赖 Core、Windows App SDK、SQLite、WinUI、PRI 或 MSIX 配置。
- 密文格式：Base64(UTF-8 JSON)，短字段 `ver`/`ek`/`nonce`/`tag`/`ct`；`nonce` 12 字节、`tag` 16 字节、会话密钥 32 字节；未知字段忽略。
- 密钥存储：`LocalState\keys.db`（SQLite，主表 `keys`，`(category, fingerprint)` 唯一）+ 完整性签名 `keys.db.sig`（HMAC 密钥存 Windows 凭据管理器）。
- 应用设置：`LocalSettings`（导出位置、加密/解密页已选密钥指纹）；不需要 `settings.json`。
- 私钥密码：用户选择记住时存 Windows 凭据管理器，按指纹区分。

## 开发优先级

1. 消息 V1 协议兼容性。
2. 密钥生成、导入、导出、删除和指纹显示。
3. 多接收方公钥和多私钥管理体验。
4. 私钥密码保存、查看、删除和修改流程。
5. WinUI 3 深浅色、标题栏、侧边栏、对话框和设置卡片体验。
6. MSIX / Microsoft Store 发布稳定性。

文件加密暂不实现，直到消息与密钥管理稳定；后续设计方向见 DEVELOPMENT.md。

UI、存储、协议校验、安全与测试的完整约定均见 [DEVELOPMENT.md](./DEVELOPMENT.md)。
