using Microsoft.AspNetCore.Authorization;
using sasipca_API.Enumerators;
using System.Data;
using System.Linq;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Attributes
{
    /// <summary>
    /// Atributo personalizado para autorizar com base no Enum UserRole.
    /// Ex: [AuthorizeRole(UserRole.Admin)]
    /// </summary>
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(params UserRole[] roles)
        {
            // O .NET espera uma string separada por vírgulas na propriedade "Roles"
            // Ex: "Admin,Beneficiary"
            Roles = string.Join(",", roles.Select(r => r.ToString()));
        }
    }
}