# MessagesEncrypter Message Format v2

本文档记录 `ver = 2` 的初步方向，尚未定稿。

`ver = 2` 计划用于支持更多密钥封装算法，重点考虑 X25519。

## 初步结构

```json
{
  "ver": 2,
  "alg": "x25519",
  "epk": "<base64>",
  "nonce": "<base64>",
  "tag": "<base64>",
  "ct": "<base64>"
}
```

## 初步设想

- `alg` 用于区分算法，例如 `rsa`、`x25519`。
- X25519 每条消息携带发送方临时公钥 `epk`。
- X25519 使用 shared secret + HKDF-SHA256 派生 AES-256-GCM 会话密钥。
- AES-GCM 仍使用 12 字节 nonce 和 16 字节 tag。
- HKDF `salt`、`info` 与 AES-GCM AAD 规则尚未定稿。

## 状态

此文档仅用于记录后续设计方向，不作为当前软件兼容承诺。
