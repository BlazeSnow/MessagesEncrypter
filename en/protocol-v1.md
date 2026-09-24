# MessagesEncrypter Message Format v1

This document records the standard Base64 JSON encrypted-package format of MessagesEncrypter.

This document is the formal format definition for `ver = 1`. Field semantics for the same `ver` must remain compatible.

`ver = 1` only defines the RSA message encryption format.

## Outer Encoding

Encrypted packages are designed for copy-and-paste transport and are wrapped in Base64.

Decoding steps:

1. Apply `Trim()` to the user input.
2. Base64-decode to obtain UTF-8 JSON.
3. Parse the JSON fields as defined in this document.

## JSON Structure

Field names are lowercase short names to reduce the copy/transfer size.

```json
{
  "ver": 1,
  "ek": "<base64>",
  "nonce": "<base64>",
  "tag": "<base64>",
  "ct": "<base64>"
}
```

## Common Fields

| Field   | Type   | Required | Description                                                  |
| ------- | ------ | -------- | ------------------------------------------------------------ |
| `ver`   | number | Yes      | Message format version. Currently fixed to `1`.              |
| `ek`    | string | Yes      | The wrapped AES-256-GCM session key.                         |
| `nonce` | string | Yes      | AES-GCM nonce, Base64-encoded, 12 bytes raw.                 |
| `tag`   | string | Yes      | AES-GCM authentication tag, Base64-encoded, 16 bytes raw.    |
| `ct`    | string | Yes      | AES-GCM ciphertext, Base64-encoded.                          |

## Algorithms

- Wrap a randomly generated AES-256-GCM session key with the recipient's RSA public key using OAEP-SHA256.
- Encrypt the UTF-8 plaintext with AES-256-GCM.

Field constraints:

| Field | Description                                                                                    |
| ----- | ---------------------------------------------------------------------------------------------- |
| `ek`  | Required. Contains the 32-byte AES session key encrypted with RSA-OAEP-SHA256, Base64-encoded. |

## AES-GCM

All algorithms ultimately encrypt the body with AES-256-GCM.

| Parameter          | Value    |
| ------------------ | -------- |
| Key length         | 32 bytes |
| Nonce length       | 12 bytes |
| Tag length         | 16 bytes |
| Plaintext encoding | UTF-8    |
| AAD                | None     |

`ver = 1` does not use AAD yet. When fields such as `ver`, `ek`, or `nonce` are tampered with, decryption typically fails at field validation, RSA-OAEP unwrapping, or AES-GCM authentication.

## Example

### RSA

```json
{
  "ver": 1,
  "ek": "BASE64_RSA_ENCRYPTED_AES_KEY",
  "nonce": "BASE64_12_BYTE_NONCE",
  "tag": "BASE64_16_BYTE_TAG",
  "ct": "BASE64_CIPHERTEXT"
}
```

## Decryption Validation Requirements

Decryption must perform the following checks:

1. `ver` must equal `1`.
2. The package must provide `ek`, `nonce`, `tag`, and `ct`.
3. `ek`, `nonce`, `tag`, and `ct` must be valid Base64.
4. `nonce` must decode to exactly 12 bytes.
5. `tag` must decode to exactly 16 bytes.
6. Unknown fields must be ignored and must not fail decryption.
7. AES-GCM authentication failure must uniformly be treated as decryption failure.

## Revision History

| Date       | Version  | Description                              |
| ---------- | -------- | ---------------------------------------- |
| 2026-05-31 | v1 final | Finalized the RSA Base64 JSON structure. |
