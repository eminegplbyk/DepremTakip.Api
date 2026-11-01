using DepremTakip.Api.Helpers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DepremTakip.Api.Swagger;

// Swagger'da "Province" kutusunu yazı kutusu yerine 81 ilin listelendiği açılır menü yapar.
public class ProvinceDropdownFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.In == ParameterLocation.Query &&
            string.Equals(parameter.Name, "Province", StringComparison.OrdinalIgnoreCase) &&
            parameter.Schema != null)
        {
            parameter.Schema.Enum = Provinces.All
                .Select(p => (IOpenApiAny)new OpenApiString(p))
                .ToList();
        }
    }
}
