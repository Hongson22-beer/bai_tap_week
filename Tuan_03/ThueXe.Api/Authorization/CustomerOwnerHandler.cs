using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ThueXe.Api.Authorization;

public class CustomerOwnerHandler : AuthorizationHandler<CustomerOwnerRequirement, Guid>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CustomerOwnerRequirement requirement,
        Guid resourceCustomerId)
    {
        // 1. Nếu là Admin hoặc Staff -> Cấp quyền truy cập toàn bộ
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Staff"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Trích xuất UserId từ claims của JWT Token
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? context.User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var currentUserId))
        {
            // 3. Nếu UserId trùng với CustomerId của tài nguyên -> Cấp quyền (Chính chủ)
            if (currentUserId == resourceCustomerId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}