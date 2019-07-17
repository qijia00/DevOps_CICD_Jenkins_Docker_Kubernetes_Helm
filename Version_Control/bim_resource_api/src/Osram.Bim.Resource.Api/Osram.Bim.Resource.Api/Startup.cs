using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Osram.Bim.Resource.Storage;
using Osram.Bim.Resource.Api.EndpointManagement;
using Osram.Bim.Resource.Api.Exceptions;
using Osram.Bim.Resource.Api.Utility.Config;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Trace;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;







namespace Osram.Bim.Resources.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _appVersion;

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException();
            _logger = _loggerFactory.CreateLogger<Program>();

            var executingAssembly = Assembly.GetExecutingAssembly();
            var attr = executingAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            _appVersion = attr?.InformationalVersion ?? "0.0.0-0";

            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile(env.ContentRootPath + "/config/appsettings.json", false, true)
                .AddJsonFile(env.ContentRootPath + $"/config/appsettings.{env.EnvironmentName}.json", true)
                .AddJsonFile(env.ContentRootPath + $"/config/serviceSettings.json", optional: true)
                .AddJsonFile(env.ContentRootPath + $"/config/serviceSettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            _configuration = configBuilder.Build();

            _logger.LogDebug("Starting up");

            try
            {
                _configuration = ConfigureConsul(configBuilder);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error adding provider from consul: {ex.Message}");
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogDebug("Configuring services");

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddCors();

            services.AddMvc()
                .AddJsonOptions(options => { options.SerializerSettings.Formatting = Formatting.Indented; })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddSingleton(_configuration);

            services.Configure<ConsulConfig>(_configuration.GetSection("consulSettings"));

            ConfigureResourceAdapter(services);

            ConfigureHealthServices(services);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Info
                    {
                        Title = "BIM Resource Api",
                        Version = _appVersion,
                        Description = "OSRAM BIM Resource Api",
                        TermsOfService = "None",
                        Contact = new Contact
                        {
                            Name = ".. ..",
                            Email = string.Empty,
                            Url = "https://bimapi.osram.com/contactus/"
                        },
                        License = new License
                        {
                            Name = "BIM API License",
                            Url = "https://bimapi.osram.com/license"
                        }
                    });

                c.EnableAnnotations();

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            // TODO: Get this from config
            var supportedCultures = new[]
            {
                //new CultureInfo("en-US"),
                //new CultureInfo("en-CA"),
                new CultureInfo("en")
                // new CultureInfo("fr"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            // By default allow any header, any method, any origin, and credentials in all CORS Response Header
            // TODO: This should be reviewed after our security topology is more fleshed out, and probably provided
            // by configuration rather than being hard coded.
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseHttpsRedirection();
            app.UseMiddleware<ResourceExceptionMiddleware>();
            app.UseMvc();

            ConfigureHealth(app);

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, request) => swagger.BasePath = "/resource");
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            //app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "BIM Resource Api - v1.0"); });

            app.UseSwaggerUI(__options =>
            {
                __options.SwaggerEndpoint($"../{__options.RoutePrefix}/v1/swagger.json", $"BIM Resource Api - {_appVersion}");

            });

        }

        private void ConfigureHealthServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IHealthContributor, DiskHealth>();
            serviceCollection.AddSingleton<IHealthContributor, ConsulHealth>();
            serviceCollection.AddHealthActuator(_configuration);

            serviceCollection.AddSingleton<IInfoContributor, Generalnfo>();
            serviceCollection.AddInfoActuator(_configuration);

            // default steeltoe endpoints with no customiation
            serviceCollection.AddTraceActuator(_configuration);
            //serviceCollection.AddEnvActuator(_configuration);
            serviceCollection.AddRefreshActuator(_configuration);
            serviceCollection.AddMappingsActuator(_configuration);
        }

        private void ConfigureHealth(IApplicationBuilder app)
        {
            app.UseHealthActuator(); // /health
            app.UseInfoActuator(); // /info
            app.UseTraceActuator(); // /trace
            //app.UseEnvActuator(); // /env TODO restrict passwords
            app.UseRefreshActuator(); // /refresh
            app.UseMappingsActuator(); // /mappings
        }

        private void ConfigureResourceAdapter(IServiceCollection serviceCollection)
        {
            //var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            //var modelsAssembly = Assembly.Load("Osram.Bim.Models");
            //assemblies.Add(modelsAssembly);

            var resourceStorageSection = _configuration.GetSection("resourceStorage")
                                         ?? throw new ApplicationException("resourceStorage configuration section not found!");

            var storageType = resourceStorageSection.GetValue<string>("type")
                              ?? throw new ApplicationException("no connection type provided");

            var supportedFiles = _configuration.GetSection("supportedFiles").Get<List<string>>();

            if (storageType == typeof(AzureAdapter).FullName)
            {
                var connectionString = resourceStorageSection.GetValue<string>("connectionString")
                                       ?? throw new ApplicationException("no connection string provided!");

                var containerName = resourceStorageSection.GetValue<string>("containerName")
                                    ?? throw new ApplicationException("no connection string provided!");

                serviceCollection.AddSingleton<IResourceAdapter>(new AzureAdapter(connectionString, containerName, supportedFiles));
            }
            else
            {
                throw new ApplicationException($"Unknown resource storage type: {storageType}");
            }
        }

        private IConfigurationRoot ConfigureConsul(IConfigurationBuilder cb)
        {
            IEnumerable<string> consulAddresses = _configuration.GetSection("consulSettings:host").GetChildren().Select(x => x.Value);
            string consulPath = _configuration.GetValue<string>("consulSettings:path");
            int consulTimer = _configuration.GetValue<int>("consulSettings:watchDelay");
            string aclToken = _configuration.GetValue<string>("consulSettings:accessToken");
            if (!consulAddresses.Any() || string.IsNullOrWhiteSpace(consulPath))
                throw new ApplicationException("no consul endpoints provided!");
            if (consulTimer < 10000)
                throw new ApplicationException("consul fail delay timer must be at least ten seconds!");
            cb.AddConsul(consulAddresses, consulPath, consulTimer, aclToken, _logger);
            return cb.Build();
        }
    }
}