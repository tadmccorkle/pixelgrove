using System;
using System.Security.Claims;

namespace Csm.PixelGrove.Auth;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetAppUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        userId = Guid.Empty;

        var claim = principal.FindFirst(c => c is
        {
            Type: ClaimTypes.NameIdentifier,
            Issuer: ClaimsIdentity.DefaultIssuer,
        });

        return claim is not null && Guid.TryParse(claim.Value, out userId);
    }
}
