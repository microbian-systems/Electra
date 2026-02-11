# Electra Authentication

This library provides authentication services for Microbians.io, including:
- Cookie-based authentication for Web
- JWT-based authentication for Mobile Apps
- Social Login (Google, Microsoft, etc.)
- Passkey (FIDO2/WebAuthn) support

## Views
The library includes default Razor views embedded as resources.
- Login: `/login`
- Register: `/register`
- Forgot Password: `/forgot-password`
- Passkey Login: `/login-passkey`
- Passkey Registration: `/register-passkey`

## Customization
Views can be overridden in the consuming application by placing a view file in the same path structure:
- `Views/Auth/Login.cshtml`
- `Views/Auth/Register.cshtml`
etc.

## Passkey Configuration
Passkeys require HTTPS and a valid Relying Party ID.
Configure in `appsettings.json`:
```json
"Passkey": {
  "RelyingPartyId": "localhost",
  "RelyingPartyName": "Microbians.io"
}
```
