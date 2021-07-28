using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Qrame.Core.Library.Logger;
using Qrame.Web.TransactServer.Extensions;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Qrame.Web.TransactServer
{
    public class Startup
	{
		private string startTime = null;
		private int processID = 0;
		private bool useProxyForward = false;
		private bool useResponseComression = false;
		private IConfiguration configuration { get; }
		private IWebHostEnvironment environment { get; }
		static readonly ServerEventListener serverEventListener = new ServerEventListener();

		public Startup(IWebHostEnvironment environment, IConfiguration configuration)
		{
			Process currentProcess = Process.GetCurrentProcess();
			processID = currentProcess.Id;
			startTime = currentProcess.StartTime.ToString();

			this.configuration = configuration;
			this.environment = environment;
			this.useProxyForward = bool.Parse(configuration.GetSection("AppSettings")["UseForwardProxy"]);
			this.useResponseComression = bool.Parse(configuration.GetSection("AppSettings")["UseResponseComression"]);
		}

		public void ConfigureServices(IServiceCollection services)
		{
			var appSettings = configuration.GetSection("AppSettings");
			StaticConfig.IsQueryIDHashing = bool.Parse(appSettings["IsQueryIDHashing"].ToString());
			StaticConfig.UseApiAuthorize = bool.Parse(appSettings["UseApiAuthorize"].ToString());
			StaticConfig.ApplicationName = environment.ApplicationName;
			StaticConfig.ContentRootPath = environment.ContentRootPath;
			StaticConfig.EnvironmentName = environment.EnvironmentName;
			StaticConfig.WebRootPath = environment.WebRootPath;
			StaticConfig.BusinessContractBasePath = appSettings["BusinessContractBasePath"].ToString();
			StaticConfig.AvailableEnvironment = appSettings["AvailableEnvironment"].ToString();
			StaticConfig.RunningEnvironment = appSettings["RunningEnvironment"].ToString();
			StaticConfig.HostName = appSettings["HostName"].ToString();
			StaticConfig.SystemCode = appSettings["SystemCode"].ToString();
			StaticConfig.MessageDataType = appSettings["MessageDataType"].ToString();
			StaticConfig.IsTransactionLogging = bool.Parse(appSettings["IsTransactionLogging"].ToString());
			StaticConfig.TransactionLogFilePath = appSettings["TransactionLogFilePath"].ToString();
			StaticConfig.IsExceptionDetailText = bool.Parse(appSettings["IsExceptionDetailText"].ToString());
			StaticConfig.IsSwaggerUI = bool.Parse(appSettings["IsSwaggerUI"].ToString());

			string withOrigins = appSettings["WithOrigins"].ToString();
			if (string.IsNullOrEmpty(withOrigins) == false)
			{
				foreach (string item in withOrigins.Split(","))
				{
					StaticConfig.WithOrigins.Add(item.Trim());
				}
			}

			StaticConfig.IsCodeDataCache = bool.Parse(appSettings["IsCodeDataCache"].ToString());
			StaticConfig.CodeDataCacheTimeout = appSettings["CodeDataCacheTimeout"] == null ? 20 : int.Parse(appSettings["CodeDataCacheTimeout"].ToString());
			StaticConfig.AuthorizationKey = StaticConfig.SystemCode + StaticConfig.RunningEnvironment + StaticConfig.HostName;
			StaticConfig.IsConfigure = true;

			if (useResponseComression == true)
			{
				services.AddResponseCompression(options =>
				{
					options.EnableForHttps = bool.Parse(configuration.GetSection("AppSettings")["ComressionEnableForHttps"]);
					options.Providers.Add<BrotliCompressionProvider>();
					options.Providers.Add<GzipCompressionProvider>();

					List<string> mimeTypes = new List<string>();
					var comressionMimeTypes = configuration.GetSection("AppSettings").GetSection("ComressionMimeTypes").AsEnumerable();
					foreach (var comressionMimeType in comressionMimeTypes)
					{
						mimeTypes.Add(comressionMimeType.Value);
					}

					options.MimeTypes = mimeTypes;
				});
			}

			if (useProxyForward == true)
			{
				services.Configure<ForwardedHeadersOptions>(options =>
				{
					var forwards = configuration.GetSection("AppSettings").GetSection("ForwardProxyIP").AsEnumerable();
					foreach (var item in forwards)
					{
						if (string.IsNullOrEmpty(item.Value) == false)
						{
							options.KnownProxies.Add(IPAddress.Parse(item.Value));
						}
					}

					bool useSameIPProxy = bool.Parse(configuration.GetSection("AppSettings")["UseSameIPProxy"]);
					if (useSameIPProxy == true)
					{
						IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
						foreach (IPAddress ipAddress in host.AddressList)
						{
							if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
							{
								options.KnownProxies.Add(ipAddress);
							}
						}
					}
				});
			}

			var routeUrls = appSettings.GetSection("RouteUrl");
			foreach (var item in routeUrls.GetChildren())
			{
				StaticConfig.RouteUrl.Add(item.Key, item.Value);
			}

			TransactionMapper.LoadBusinessContract(Log.Logger, configuration);

			services.AddCors(options =>
			{
				options.AddDefaultPolicy(
				builder => builder
					.AllowAnyHeader()
					.AllowAnyMethod()
					.WithOrigins(StaticConfig.WithOrigins.ToArray())
					.SetIsOriginAllowedToAllowWildcardSubdomains()
				);
			});

			services.AddMvc().AddMvcOptions(option =>
			{
				option.EnableEndpointRouting = false;

				option.InputFormatters.Add(new RawRequestBodyFormatter(Log.Logger));
			})
			.AddJsonOptions(option =>
			{
				option.JsonSerializerOptions.PropertyNamingPolicy = null;
			});

			LoggerFactory loggerFactory = new LoggerFactory();
			loggerFactory.AddProvider(new FileLoggerProvider(StaticConfig.TransactionLogFilePath, new FileLoggerOptions()
			{
				Append = true,
				FileSizeLimitBytes = 104857600, // 100 MB
				MaxRollingFiles = 30
			}));

			services.AddSingleton(loggerFactory);
			services.AddSingleton(Log.Logger);
			services.AddSingleton(configuration);
			services.AddControllers();

			if (StaticConfig.IsSwaggerUI == true)
			{
				services.AddSwaggerGen(option =>
				{
					option.SwaggerDoc("v1", new OpenApiInfo
					{
						Version = "v1",
						Title = "Qrame BP Transaction Web API"
					});
					option.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
					option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
				});
				services.AddSwaggerGenNewtonsoftSupport();
			}
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (StaticConfig.IsSwaggerUI == true)
			{
				app.UseSwagger();
				app.UseSwaggerUI(option =>
				{
					option.SwaggerEndpoint("/swagger/v1/swagger.json", "Qrame BP API V1");
					option.RoutePrefix = "docs";
				});
			}

			if (useResponseComression == true)
			{
				app.UseResponseCompression();
			}

			if (useProxyForward == true)
			{
				app.UseForwardedHeaders(new ForwardedHeadersOptions
				{
					ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
				});
			}

			if (env.IsDevelopment() == true || StaticConfig.IsExceptionDetailText == true)
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseStaticFiles();
			app.UseCors();
			// app.UseSerilogRequestLogging();
			app.UseRouting();
			app.UseAuthorization();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/diagnostics", async context =>
				{
					var result = new
					{
						Environment = new
						{
							ProcessID = processID,
							ProcessCPUUsage = await ServerEventListener.GetCpuUsageForProcess(),
							ProcessRAMUsage = ServerEventListener.GetRamUsageForProcess(),
							StartTime = startTime,
							SystemCode = StaticConfig.SystemCode,
							ApplicationName = StaticConfig.ApplicationName,
							Is64Bit = Environment.Is64BitOperatingSystem,
							MachineName = Environment.MachineName,
							HostName = StaticConfig.HostName,
							RunningEnvironment = StaticConfig.RunningEnvironment,
							ApiAuthorize = StaticConfig.UseApiAuthorize,
							IsTransactionLogging = StaticConfig.IsTransactionLogging
						},
						System = serverEventListener.SystemRuntime,
						Hosting = serverEventListener.AspNetCoreHosting,
						// Kestrel = serverEventListener.AspNetCoreServerKestrel
					};
					context.Response.Headers["Content-Type"] = "application/json";
					await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
				});
			});
			app.UseMvcWithDefaultRoute();

			try
			{
				if (env.IsProduction() == true || env.IsStaging() == true)
				{
					File.WriteAllText("appstartup-update.txt", DateTime.Now.ToString());
				}
			}
			catch
			{
			}
		}
	}
}
