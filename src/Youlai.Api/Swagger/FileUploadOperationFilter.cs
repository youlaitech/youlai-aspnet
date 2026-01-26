using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Youlai.Api.Swagger;

/// <summary>
/// 处理 IFormFile 的 Swagger 表单参数生成
/// </summary>
internal sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasFormFile = context.ApiDescription.ParameterDescriptions
            .Any(p => p.Type == typeof(IFormFile));

        if (!hasFormFile)
        {
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties =
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                            },
                        },
                        Required = new HashSet<string> { "file" },
                    },
                },
            },
        };
    }
}
