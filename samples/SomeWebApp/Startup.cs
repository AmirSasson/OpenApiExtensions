using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Microsoft.Azure.Sentinel.ServiceHostingTools.SwaggerExtensions.Helpers;
using AsiSwaggerExtensions.Helpers;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNet.OData.Query;
using Microsoft.OpenApi.Models;

namespace SomeWebApp
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        private readonly SwaggerConfig _swaggerConfig;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var genarateInternalSwagger = Environment.GetCommandLineArgs().Contains("--internal-swagger");
            var genarateExternalSwagger = !genarateInternalSwagger;
            var OdataReusableParameters = new List<string>() { "$filter", "$orderBy", "$skipToken", "$top" };
            _swaggerConfig = new SwaggerConfig
            {
                PolymorphicSchemaModels = new List<Type> { typeof(V1.WeatherForecast), typeof(V2.WeatherForecast) },
                ModelEnumsAsString = true,
                GlobalCommonReusableParameters = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiParameter>()
                {
                    { "SubscriptionIdParameter", ArmReusableParameters.GetSubscriptionIdParameter() },
                    { "ResourceGroupNameParameter", ArmReusableParameters.GetResourceGroupNameParameter() },
                    { "ApiVersionParameter", ArmReusableParameters.GetApiVersionParameter() }
                },
                ResourceProviderReusableParameters = OdataReusableParameters.Concat(new List<string> { "WorkspaceName" }).ToList(),
                HideParametersEnabled = genarateExternalSwagger,
                GenerateExternalSwagger = genarateExternalSwagger,
                XmlCommentFile = Assembly.GetExecutingAssembly().GetName().Name,
                SupportedApiVersions = new[] { "2021-09-01-preview", "2022-01-01-preview" },
                OverrideMappingTypeToSchema = new Dictionary<Type, Microsoft.OpenApi.Models.OpenApiSchema> { { typeof(ODataQueryOptions<>), new OpenApiSchema() } }
            };
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApiVersioning(config =>
            {
                // Specify the default API Version as 1.0
                config.DefaultApiVersion = ApiVersion.Parse("2021-09-01-preview");
                // If the client hasn't specified the API version in the request, use the default API version number 
                config.AssumeDefaultVersionWhenUnspecified = true;
                // Advertise the API versions supported for the particular endpoint
                config.ReportApiVersions = true;
            });

            services.AddOData();

            services
                .AddControllers(options =>
                {
                    // workaround till we migrate to .net5 or greater/upgrade the odata nuget https://github.com/OData/WebApi/issues/1177#issuecomment-358659774

                    foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                    {
                        outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                    }
                    foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                    {
                        inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                    }

                })
                .AddNewtonsoftJson();

            services.AddArmCompliantSwagger(_swaggerConfig);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseApiVersioning();


            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
                // Change generated swagger version to 2.0
                options.SerializeAsV2 = true;
            });

            app.UseSwaggerUI(option =>
            {
                IEnumerable<string> actualDocumentsToGenerate = _swaggerConfig.SupportedApiVersions;
                if (actualDocumentsToGenerate == null || !actualDocumentsToGenerate.Any())
                {
                    actualDocumentsToGenerate = new[] { _swaggerConfig.DefaultVersion };
                }
                actualDocumentsToGenerate.ToList().ForEach(v => option.SwaggerEndpoint($"/swagger/{v}/swagger.json", v));
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.EnableDependencyInjection();
            });
        }
    }
}
