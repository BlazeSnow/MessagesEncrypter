# MessagesEncrypter Message Format v1

本文档记录 MessagesEncrypter 的标准 Base64 JSON 密文包格式。

本格式尚未对外发布，可在正式发布前修订。正式发布后，同一 `ver` 的字段语义应保持兼容。

## 外层编码

密文包面向复制和粘贴传输，外层使用 Base64 编码。

解码流程：

1. 对用户输入进行 `Trim()`。
2. 按 Base64 解码得到 UTF-8 JSON。
3. 按本文档定义解析 JSON 字段。

## JSON 结构

字段名使用小写短字段，减少复制传输体积。

```json
{
  "ver": 1,
  "alg": "rsa",
  "ek": "<base64>",
  "nonce": "<base64>",
  "tag": "<base64>",
  "ct": "<base64>"
}
```

## 公共字段

| 字段 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `ver` | number | 是 | 消息格式版本。当前固定为 `1`。 |
| `alg` | string | 是 | 密钥封装算法短标识。 |
| `ek` | string | 视算法而定 | 被封装的 AES-256-GCM 会话密钥。 |
| `epk` | string | 视算法而定 | 临时公钥。 |
| `nonce` | string | 是 | AES-GCM nonce，Base64 编码，原始长度 12 字节。 |
| `tag` | string | 是 | AES-GCM authentication tag，Base64 编码，原始长度 16 字节。 |
| `ct` | string | 是 | AES-GCM ciphertext，Base64 编码。 |

## 算法标识

| `alg` | 实际算法套件 |
| --- | --- |
| `rsa` | RSA-OAEP-SHA256 + AES-256-GCM |
| `x25519` | X25519-HKDF-SHA256 + AES-256-GCM |

### rsa

`alg` 值：

```text
rsa
```

用途：

- 使用接收方 RSA 公钥通过 OAEP-SHA256 封装随机生成的 AES-256-GCM 会话密钥。
- 使用 AES-256-GCM 加密 UTF-8 明文。

字段约束：

| 字段 | 说明 |
| --- | --- |
| `ek` | 必须存在。内容为 RSA-OAEP-SHA256 加密后的 32 字节 AES 会话密钥，Base64 编码。 |
| `epk` | 不得出现。 |

### x25519

`alg` 值：

```text
x25519
```

用途：

- 发送方为每条消息生成一对临时 X25519 密钥。
- 发送方使用临时私钥和接收方 X25519 公钥计算 shared secret。
- 接收方使用自己的 X25519 私钥和密文包中的临时公钥计算同一个 shared secret。
- 双方使用 HKDF-SHA256 从 shared secret 派生 32 字节 AES-256-GCM 会话密钥。
- 使用 AES-256-GCM 加密 UTF-8 明文。

字段约束：

| 字段 | 说明 |
| --- | --- |
| `ek` | 不得出现。X25519 不直接传输被加密的会话密钥。 |
| `epk` | 必须存在。内容为发送方临时 X25519 公钥，Base64 编码。 |

HKDF 参数需在实现时固定并记录：

| 参数 | 值 |
| --- | --- |
| Hash | SHA-256 |
| Output length | 32 bytes |
| Salt | 待定 |
| Info | 待定 |

## AES-GCM

所有算法最终都使用 AES-256-GCM 加密正文。

| 参数 | 值 |
| --- | --- |
| Key length | 32 bytes |
| Nonce length | 12 bytes |
| Tag length | 16 bytes |
| Plaintext encoding | UTF-8 |
| AAD | 待定 |

如果启用 AAD，建议至少认证 `ver`、`alg`、`ek`、`epk`、`nonce` 等未加密头部字段，防止算法和密钥封装字段被替换。

## 示例

### RSA

```json
{
  "ver": 1,
  "alg": "rsa",
  "ek": "BASE64_RSA_ENCRYPTED_AES_KEY",
  "nonce": "BASE64_12_BYTE_NONCE",
  "tag": "BASE64_16_BYTE_TAG",
  "ct": "BASE64_CIPHERTEXT"
}
```

### X25519

```json
{
  "ver": 1,
  "alg": "x25519",
  "epk": "BASE64_EPHEMERAL_X25519_PUBLIC_KEY",
  "nonce": "BASE64_12_BYTE_NONCE",
  "tag": "BASE64_16_BYTE_TAG",
  "ct": "BASE64_CIPHERTEXT"
}
```

## 解密校验要求

解密时必须进行以下校验：

1. `ver` 必须等于 `1`。
2. `alg` 必须是已支持的算法标识。
3. `nonce` 解码后必须为 12 字节。
4. `tag` 解码后必须为 16 字节。
5. RSA 密文包必须提供 `ek`，且不得出现 `epk`。
6. X25519 密文包必须提供 `epk`，且不得出现 `ek`。
7. AES-GCM 认证失败时，必须统一视为解密失败。

## 修订记录

| 日期 | 版本 | 说明 |
| --- | --- | --- |
| 2026-05-31 | v1 draft | 记录统一 Base64 JSON 结构，并预留 RSA 与 X25519 算法字段。 |
