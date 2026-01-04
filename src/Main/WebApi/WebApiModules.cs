using Infrastructure.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Main.WebApi;

public static class WebApiModules
{
    public static IServiceCollection AddWebApiModules(
        this IServiceCollection services
    )
    {
        return services
            .AddEndpointsApiExplorer()
            .AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, _) =>
                {
                    document.Info = GetOpenApiInfo();
                    return Task.CompletedTask;
                });
            })
            .AddProblemDetails()
            .AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithExposedHeaders("*")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin();
                });
            }
        );
    }

    public static WebApplication UseWebApiModules(this WebApplication app)
    {
        app.UseCors();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "v1");
        }); 

        app.UseHttpsRedirection();

        app.MapRoutes();
        app.MapOpenApi();

        return app;
    }

    private static OpenApiInfo GetOpenApiInfo()
    {
         return new OpenApiInfo
          {
              Version = "v1",
              Title = "Movies API",
              Description = "A simple API to manage movies and movie events.",
              Contact = new OpenApiContact
              {
                  Name = "Matse De Deyn | Lander Debeir | Pratik Lohani | Youri Haentjens",
                  Email = "matse.de.deyn@student.howest.be"
              }
          };
    }
}
