# User Guide

## Message Encryption

- Select the recipient's public key in the **Recipient** dropdown, enter the message, and click **Encrypt** to generate the encrypted package;
- Click **Copy ciphertext** to copy the package to the clipboard and send it to the recipient through any channel (chat apps, email, etc.);
- Every encryption generates a fresh AES-256-GCM session key, so the same plaintext never produces the same ciphertext;
- Blank messages cannot be encrypted;
- The selected public key is remembered and restored the next time you open the app.

## Message Decryption

- Select your private key in the **My Private Keys** dropdown, paste the encrypted package, and click **Decrypt**;
- If the private key has a password, enter it — check **Remember password** to save it to Windows Credential Manager;
- An incomplete or tampered package always fails to decrypt, and no partial content is ever output;
- The selected private key is remembered and restored the next time you open the app.

## Recipient Public Key Management

On the **Recipient Public Keys** page:

- **Import**: paste the public key text or import from a `.pub` / `.pem` / `.txt` file; the alias can be auto-filled from the file name;
- **Copy / Export**: copy the public key text or export it as a `.pub` file — File Explorer opens with the exported file selected;
- **Rename**: change the key alias;
- **Delete**: deleted after confirmation.

Importing the same public key again shows a duplicate-fingerprint warning. The list is sorted by alias.

## My Private Key Management

On the **My Private Keys** page:

- **Generate**: choose the RSA key size (2048, 3072, 4096, 8192 — 4096 by default) and set the private key password;
- **Import**: encrypted and unencrypted private keys are both supported; imports are uniformly converted to encrypted PKCS#8 PEM storage, and the matching public key is derived automatically;
- **Copy / Export public key**: get shareable public key text or a `.pub` file;
- **Copy / Export private key**: the exported private key remains encrypted (`.pem`) and requires the private key password to use;
- **Change private key password**: enter the old password, the new password, and confirm the new one — internally the key is decrypted with the old password and re-encrypted with the new one;
- **Delete**: deleted after confirmation, together with any saved password for the key.

Private keys are always stored encrypted locally — never in plaintext.

## Private Key Passwords

- **Remember password** is available when generating, importing, decrypting, and changing the password; passwords are stored in Windows Credential Manager, one per key fingerprint;
- A forgotten private key password means the key's past messages can never be decrypted, and it cannot be recovered;
- After changing the private key password you can choose to remember the new one; if not chosen, the previously saved old password is removed.

## Key Fingerprints

- The fingerprint is derived from the public key material (first 16 bytes of SHA-256) — any change to the key changes the fingerprint;
- Use it to verify public keys offline and prevent key substitution;
- The key list shows both the key type (e.g. RSA4096) and the fingerprint.

## Settings

- **Export location**: defaults to the current user's Downloads folder and can be changed; after exporting a key, File Explorer opens with the file selected;
- **Display language**: follow the system, Simplified Chinese, or English — a restart is required after switching;
- **App version**: read from the installed package.

## Key Store Integrity

- Keys are stored in `keys.db` (a SQLite database) inside the app's local data folder, protected by a `keys.db.sig` integrity signature;
- The signature is verified at every startup; on mismatch a dialog offers **Exit app** or **Ignore and re-sign** (a dangerous operation);
- See the [FAQ](/en/faq) for details.
