using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Services;

public interface ICurrentUserService
{
    UserContext GetUserContext();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserContext GetUserContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
        {
            return new UserContext
            {
                UserId = string.Empty,
                UserName = "Anonymous",
                Role = string.Empty,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            };
        }

        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userName = user.Identity.Name ?? user.FindFirstValue(ClaimTypes.Email) ?? "User";
        var role = user.FindFirstValue(ClaimTypes.Role) ?? AppRoles.SalesExecutive;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        return new UserContext
        {
            UserId = userId,
            UserName = userName,
            Role = role,
            IpAddress = ipAddress
        };
    }
}
