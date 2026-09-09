# Unlock and navigation regression — 1.4.4

The reported pre-password navigation reproduced a code path in which the shell
started with NavigationIsAllowed=true. Opening Accounts without authenticating
attempted vault decryption using an empty session credential and could enter the
legacy “saved password invalid / change password” dialog loop. That message did
not establish that the user's actual vault password was wrong.

The fix starts navigation disabled and enables it after credential verification.
The desktop frame also rejects protected page routes while locked. Locking clears
session credentials and displayed accounts and cancels pending operations.
Vault loading checks navigation state, rejects an absent session credential, and
checks cancellation before publishing decrypted accounts. Authentication failures
return to password entry, with no password-change request or vault rewrite.

Verification used synthetic accounts and encrypted vaults only:

- Actual UI frame navigation could not display Accounts, Settings, or an unknown
  page before unlock; navigation worked when enabled.
- Relocking cleared accounts, canceled pending work, and blocked Accounts again.
- Empty and incorrect credentials were rejected; a subsequent correct credential
  decrypted the same vault successfully.
- Production workflow, generated account controls, crypto, tamper rejection,
  wrong-password, and legacy migration checks passed.

The user's running installed app and vault were not modified during verification.
A real-vault unlock on the user's machine still needs confirmation after installing
the fixed build. Native Windows execution and hardware biometrics were not tested.
