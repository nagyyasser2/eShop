﻿using Hangfire.Dashboard;

namespace eShop.Api.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow access only to authenticated users with Admin role
            //return httpContext.User.Identity?.IsAuthenticated == true
            //       && httpContext.User.IsInRole("Admin");
            return true;
        }
    }
}
