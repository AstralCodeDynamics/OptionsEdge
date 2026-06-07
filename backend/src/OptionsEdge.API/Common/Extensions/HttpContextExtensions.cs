using System.Security.Claims;
using OptionsEdge.API.Infrastructure.Data;

namespace OptionsEdge.API.Common.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext ctx, IConfiguration config)
    {
        var claim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? ctx.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (claim is not null && Guid.TryParse(claim, out var userId))
            return userId;

        if (Guid.TryParse(config["Dev:UserId"], out var devUserId))
            return devUserId;

        return DevDataSeeder.DevUserId;
    }
}
