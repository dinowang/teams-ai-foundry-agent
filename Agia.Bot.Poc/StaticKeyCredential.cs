using Azure.Core;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
public class StaticKeyCredential : TokenCredential
{
    private readonly string _key;

    public StaticKeyCredential(string key)
    {
        _key = key;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(_key, DateTimeOffset.MaxValue);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(new AccessToken(_key, DateTimeOffset.MaxValue));
    }
}