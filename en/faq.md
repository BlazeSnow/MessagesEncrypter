# Frequently Asked Questions

## Is MessagesEncrypter an instant messaging app?

No. The software has no server and never sends any messages. It only converts plaintext into an encrypted package locally (or restores plaintext from an encrypted package) — you deliver the package to the other party through your own channels (chat apps, email, etc.).

## Is it compatible with PGP / GPG?

No. MessagesEncrypter uses a custom ciphertext format (Base64-wrapped JSON) and neither reads nor produces PGP formats. The full format specification is available on the [Message Format](/en/protocol-v1) page, and third-party developers can implement interoperability from the documentation.

## What if I forget my private key password?

Private keys are stored locally encrypted with the password, and the software does not store the password unless you explicitly choose to remember it. If the password is forgotten, the corresponding private key can no longer decrypt anything and cannot be recovered. You will need to generate a new key pair and ask contacts to use your new public key.

## Where are my keys and passwords stored?

- Keys are stored in `keys.db` (a SQLite database) inside the app's local data folder, protected by a `keys.db.sig` integrity signature;
- When you check **Remember password**, the private key password is stored in Windows Credential Manager, one entry per key fingerprint.

All data stays on your machine — nothing is uploaded to any server.

## The app warns about a key store integrity check failure at startup?

At startup the app verifies that the key store file matches its signature. A mismatch means the store may have been modified externally — commonly caused by manual edits to the database file, third-party sync or backup tools overwriting it, or disk errors.

You can choose **Exit app** in the dialog. If you are sure the change came from yourself, you may choose **Ignore and re-sign** (a dangerous operation that resets the signing key). Afterwards, please verify the key list still matches what you remember.

## How do I verify a recipient's public key has not been swapped?

Check the key fingerprint on the **Recipient Public Keys** or **My Private Keys** page and confirm it with the other party through a trusted channel (in person, a voice call, etc.) before using it. The fingerprint is derived from the key material — any change to the key changes the fingerprint.

## Why does encrypting the same plaintext produce different ciphertext every time?

That is expected. Every encryption generates a fresh AES session key and nonce, so the same plaintext never produces the same ciphertext — this is part of the security.

## Why does decryption fail?

Common causes:

- The encrypted package was copied incompletely or modified — tampered ciphertext always fails verification, and nothing is output in that case;
- The wrong private key was selected — only the private key matching the recipient public key used for encryption can decrypt;
- The private key password is wrong.

Make sure the encrypted package is copied in full (no extra characters at either end) and the correct private key is selected, then try again.

## Can I still decrypt old messages after deleting a private key?

No. Decryption requires the matching private key, and deletion cannot be undone. Export a backup before deleting (the exported key file is still an encrypted PEM and requires the password to use).

## How do I migrate to a new computer?

Export the private key (`.pem` file) from the **My Private Keys** page on the old computer and import it on the new one; the matching public key can be exported (`.pub` file) and shared with your contacts again.

## Does it support file encryption?

Not yet. The current version focuses on encrypting and decrypting text messages.

## Which systems are supported?

Windows 10 (version 17763) and above, on x86, x64, and ARM64, distributed through the Microsoft Store.
