using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace app_curso_claude.Filters
{
    /// <summary>
    /// Answers 404 while the given feature flag is off. The flag is read from the configuration on every request.
    /// </summary>
    /// <param name="flag">Configuration key of the flag, for example "Features:Contact".</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireFeatureAttribute(string flag) : Attribute, IAuthorizationFilter
    {
        // An authorization filter runs before antiforgery validation,
        // so a POST to a disabled feature also gets 404 instead of 400.
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            if (!configuration.GetValue<bool>(flag))
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
