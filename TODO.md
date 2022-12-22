Readme:
- Replace Authorizer with ChannelAuthorizer


Implementation:
- There is a weird implementation regarding IAuthorizerAsync and IAuthorizer
- Prevent Authorizer and ChannelAuthorizer from accepting each other. Don't make them inherit from each other.

