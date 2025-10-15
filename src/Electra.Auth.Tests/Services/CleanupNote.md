# Cleanup Note

The following files were removed to simplify the test suite:
- DefaultAuthenticationCeremonyHandleServiceTests.cs
- DefaultRegistrationCeremonyHandleServiceTests.cs  
- DefaultCookieCredentialStorageTests.cs

The main tests are now in:
- ElectraAuthIntegrationTests.cs (comprehensive integration tests)
- DefaultUserServiceTests.cs (simplified service tests)
- AccountControllerTest.cs (basic controller tests)
- IdentityTests.cs (existing identity tests)

Focus is on testing essential registration and login functionality for both traditional logins and passkeys.