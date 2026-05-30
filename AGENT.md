# MessagesEncrypter 项目指导

## 项目定位

本项目目标是开发一款 Windows 桌面端公钥加密工具，基于 WinUI3 和 .NET 实现消息与文件加密。用户使用接收方公钥加密内容，接收方使用自己的私钥解密。公钥由用户自行交换，不内置 PKI/CA 体系。

## 技术栈

- UI 框架：WinUI3
- 桌面运行时：Windows App SDK
- .NET 版本：.NET 10
- 加密库：优先使用 `System.Security.Cryptography`
- 目标平台：Windows 10.0.17763.0 +

WinUI3 是桌面应用模型，不受 UWP 沙箱限制，可以直接使用本地文件系统、剪贴板和 .NET 原生密码学 API。

## 核心加密设计

禁止直接使用 RSA 加密长消息或文件。RSA-OAEP 单次加密长度有限，RSA-4096 + OAEP-SHA256 最多只能加密约 470 字节。

统一采用混合加密：

1. 随机生成 AES-256-GCM 会话密钥。
2. 使用 AES-GCM 加密消息或文件内容。
3. 使用接收方 RSA 公钥以 OAEP-SHA256 加密 AES 会话密钥。
4. 打包输出加密后的会话密钥、nonce、tag 和密文。
5. 解密时先用 RSA 私钥恢复 AES 会话密钥，再用 AES-GCM 解密并验证完整性。

推荐算法组合：

- 非对称加密：RSA-4096 + OAEP-SHA256
- 对称加密：AES-256-GCM
- 随机数：`RandomNumberGenerator`
- 完整性校验：AES-GCM tag

## 消息加密格式

消息通常较小，使用单块 AES-GCM 加密即可。

推荐 JSON 包格式，外层可再做 Base64 编码，方便复制和粘贴：

```json
{
  "ver": 1,
  "encryptedKey": "<base64>",
  "nonce": "<base64>",
  "tag": "<base64>",
  "ciphertext": "<base64>"
}
```

如需紧凑格式，可使用二进制包：

```text
[4 bytes]  magic "PKEM"
[2 bytes]  encrypted key length
[N bytes]  RSA encrypted AES session key
[12 bytes] AES-GCM nonce
[16 bytes] AES-GCM tag
[remaining] AES-GCM ciphertext
```

## 文件加密格式

文件可能达到 GB 级，必须支持流式或分块处理，不能一次性全部载入内存。

推荐策略：

- 小文件，小于 10MB：整文件 AES-GCM。
- 大文件，大于等于 10MB：分块 AES-GCM。
- 解密时根据格式魔数自动识别。

分块格式建议：

```text
[Header]
  magic "PKEF"
  RSA encrypted AES session key
  chunk size
  chunk count
  encrypted original metadata

[Chunk N]
  chunk index
  nonce
  tag
  ciphertext length
  ciphertext
```

关键要求：

- 每块使用独立 12 字节随机 nonce。
- `chunk_index` 必须作为 AES-GCM associated data 参与认证，防止块重排、删除或替换。
- Header 中记录总块数，解密时检查截断攻击。
- 大文件读写使用异步 I/O，避免阻塞 UI 线程。
- `AesGcm` 实例可以复用，避免每块重复创建带来的开销。

## 密钥管理

密钥格式建议：

- 公钥导出：PEM SPKI，形如 `-----BEGIN PUBLIC KEY-----`
- 私钥存储：加密 PKCS#8 PEM，形如 `-----BEGIN ENCRYPTED PRIVATE KEY-----`
- 公钥交换：`.pub` 文件或剪贴板文本

功能要求：

- 生成 RSA-4096 密钥对。
- 导出公钥到文件或剪贴板。
- 从文件或剪贴板导入他人公钥。
- 显示公钥指纹，建议使用 SHA-256 哈希的前若干位，便于线下核对。
- 支持密钥别名管理。
- 私钥必须加密存储，不能明文落盘。

私钥保护建议使用 .NET 原生 PKCS#8 加密导出能力：

```csharp
string pem = rsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters);
```

PBKDF2 迭代次数建议不低于 600,000。

## UI 功能范围

推荐主要页面：

- 消息加密
- 消息解密
- 文件加密
- 文件解密
- 密钥管理
- 设置与安全说明

文件功能需要支持：

- 文件选择
- 拖放导入
- 输出路径选择
- 进度条
- 错误提示
- 解密失败时明确提示认证失败或格式错误

剪贴板功能需要支持：

- 复制公钥
- 粘贴公钥
- 复制加密消息
- 粘贴加密消息

## 安全注意事项

- 每次加密必须生成新的 AES 会话密钥。
- 每次 AES-GCM 加密必须生成新的 nonce。
- 不要复用同一 nonce 和同一 AES key 的组合。
- 解密失败时不要输出部分明文。
- 解密异常需要统一为用户可理解的错误，不要泄漏内部堆栈。
- 不支持前向安全性。静态私钥泄漏后，历史密文可能被解密；如未来需要前向安全，可考虑 X3DH / Double Ratchet 方案。
- 本项目不是即时通讯工具，不应在 UI 或文案中暗示实时聊天能力。
- 不兼容 PGP 时，不要命名为 PGP Tool 或暗示 PGP 格式兼容。

## 开发优先级

建议开发顺序：

1. 密钥生成、导入、导出和指纹显示。
2. 消息混合加密与解密。
3. 消息打包和格式校验。
4. 小文件整文件 AES-GCM 加密。
5. 大文件分块 AES-GCM 加密。
6. 文件格式 Header 与 Chunk 校验。
7. WinUI3 页面、剪贴板、文件拖放和进度反馈。
8. 边界测试、安全审查和大文件测试。

## 测试重点

需要覆盖：

- 空消息、短消息、长消息。
- 非 UTF-8 文本内容。
- 错误公钥、错误私钥、错误密码。
- 密文被篡改。
- nonce、tag、encrypted key 长度异常。
- 文件为空。
- 文件大小刚好等于分块边界。
- 文件大小超过分块边界 1 字节。
- 大文件加密和解密。
- 块重排、块删除、文件截断。
- 原文件名和元数据异常字符。
