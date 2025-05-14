The client-side application would then need to be updated to:

Receive the authorization code from the URL
Call the token endpoint (/connect/token) to exchange the code for access and refresh tokens
Store these tokens for future API calls

The flow would be:

1. User authenticates with external provider
1. Server creates an OpenIddict authorization code
1. Server redirects to client with the code
1. Client exchanges code for tokens by calling the token endpoint
1. Client stores tokens and uses them for API calls