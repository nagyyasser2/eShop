namespace eShop.Api.Extensions;
using eShop.Api.Middlewares;
using eShop.Api.Filters;
using Hangfire;


public static class AppExtensions
{
    public static WebApplication UseApplicationMiddleware(
        this WebApplication app, IWebHostEnvironment environment)
    {
        app.UseCors("AllowAll");
        app.UseRouting();

        if (environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "eShop API V1");
                options.RoutePrefix = string.Empty;
            });
        }

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()]
        });

        return app;
    }

    public static WebApplication UseAuthenticationMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}