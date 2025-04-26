using System.Reflection;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using TestNest.Admin.API.Exceptions;
using TestNest.Admin.API.Middleware;
using TestNest.Admin.Application;
using TestNest.Admin.Infrastructure;
using TestNest.Admin.Infrastructure.Persistence;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Employee;
using TestNest.Admin.SharedLibrary.Dtos.Requests.Establishment;
using TestNest.Admin.SharedLibrary.Exceptions.Common;

namespace TestNest.Admin.API;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        _ = builder.Services.AddControllers()
            .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver());

        _ = builder.Services.AddEndpointsApiExplorer();

        var openApiPatchSchema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { "op", new OpenApiSchema { Type = "string", Description = "Operation type (replace, add, remove, etc.)" } },
                    { "path", new OpenApiSchema { Type = "string", Description = "Path to the property to update" } },
                    { "value", new OpenApiSchema { Type = "object", Description = "New value for the property" } }
                }
            }
        };

        _ = builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Palawan Nest", Version = "v1" });
            c.MapType<JsonPatchDocument<EmployeePatchRequest>>(() => openApiPatchSchema);
            c.MapType<JsonPatchDocument<EstablishmentPatchRequest>>(() => openApiPatchSchema);
            c.MapType<JsonPatchDocument<EstablishmentAddressPatchRequest>>(() => openApiPatchSchema);
            c.MapType<JsonPatchDocument<EstablishmentContactPatchRequest>>(() => openApiPatchSchema);
            c.MapType<JsonPatchDocument<EstablishmentMemberPatchRequest>>(() => openApiPatchSchema);
            c.MapType<JsonPatchDocument<EstablishmentPhonePatchRequest>>(() => openApiPatchSchema);

            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });

        _ = builder.Services.AddPersistenceServices(builder.Configuration);
        _ = builder.Services.AddScoped<IErrorResponseService, ErrorResponseService>();
        _ = builder.Services.AddApplicationServices(builder.Configuration);

        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        _ = builder.Services.AddDbContext<ApplicationDbContext>(options =>
             options.UseSqlServer(
                 builder.Configuration.GetConnectionString("DefaultConnection"),
                 sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()
             )
         );

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            _ = app.UseSwagger();
            _ = app.UseSwaggerUI();
        }

        _ = app.UseHttpsRedirection();
        _ = app.UseMiddleware<ExceptionHandlingMiddleware>();
        _ = app.UseAuthorization();
        _ = app.MapControllers();
        app.Run();
    }
}
