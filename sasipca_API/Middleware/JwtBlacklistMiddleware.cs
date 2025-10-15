using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using NeighbourLink_API.Data;
using NeighbourLink_API.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.DependencyInjection;

namespace NeighbourLink_API.Middleware
{
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory; // Usa um escopo para acessar JWTService

        public JwtBlacklistMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token))
            {
                using (var scope = _scopeFactory.CreateScope()) // Criar escopo para resolver serviços scoped
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<NLDbContext>();
                    var jwtService = scope.ServiceProvider.GetRequiredService<JWTService>();

                    // Verifica se o token está na blacklist
                    bool tokenExists = await dbContext.TokenBlacklists.AnyAsync(t => t.Token == token);
                    if (tokenExists)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Token bloqueado. Faça login novamente.");
                        return;
                    }

                    // Verifica se o token está expirado
                    var expiration = jwtService.GetTokenExpiration(token);
                    if (expiration <= DateTime.UtcNow)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Token expirado. Renove seu token.");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
