namespace Backend.API.Extensions
{
    public static class HttpsRedirectionExtensions
    {
        public static IApplicationBuilder UseHttpsRedirectionWithHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (context.Request.IsHttps)
                {
                    await next();
                    return;
                }

                // Сохраняем заголовки авторизации
                var authHeader = context.Request.Headers["Authorization"].ToString();

                var newUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
                context.Response.Redirect(newUrl, permanent: false);

                // К сожалению, при редиректе заголовки теряются клиентом
                // Лучше просто отключить редирект для API
            });
        }
    }
}
