using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace sasipca_API.Middleware
{
    /// <summary>
    /// Método auxiliar para obter o ID do utilizador autenticado a partir do contexto HTTP.
    /// Pode ser utilizado em todos os controladores.
    /// Apenas vai buscar o userId do token de acesso, a Endpoints com [Authorize]
    /// </summary>
    public class HttpClaim : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var endpoint = context.ActionDescriptor.EndpointMetadata;
            bool requiresAuth = endpoint.Any(meta => meta is AuthorizeAttribute);

            if (!requiresAuth) return; // Se Endpoint não requer autenticação, não faz nada.

            var httpContext = context.HttpContext;
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Não foi possível identificar o utilizador autenticado." });
                return;
            }

            // Adiciona o userId ao HttpContext para que possa ser usado nos controladores
            httpContext.Items["UserId"] = userId;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nada a fazer depois da ação
        }
    }
}
