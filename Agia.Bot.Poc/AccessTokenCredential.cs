using Azure.Core;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;

public class AccessTokenCredential : TokenCredential
{
    private readonly string _accessToken;

    private readonly DateTimeOffset _expiresOn;

    public AccessTokenCredential(string accessToken)
    {
        _accessToken = accessToken;
        _expiresOn = DateTimeOffset.MaxValue;
    }

    public AccessTokenCredential(JsonWebToken jwtToken)
    {
        _accessToken = jwtToken.Token.RawData;

        var exp = jwtToken.Token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        
        _expiresOn = exp != null 
                        ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)) 
                        : DateTimeOffset.MaxValue;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(_accessToken, _expiresOn);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(new AccessToken(_accessToken, _expiresOn));
    }
}