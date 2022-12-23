Readme:
- Replace Authorizer with ChannelAuthorizer


Implementation:
- Signin on reconnection
- Bind to user events
- Watchlist interface + Bind to watchlist events

Later:
- There is a weird implementation regarding IAuthorizerAsync and IAuthorizer
- Prevent Authorizer and ChannelAuthorizer from accepting each other. Don't make them inherit from each other.

