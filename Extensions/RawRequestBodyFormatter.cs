using MessagePack;

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Qrame.Core.Library.MessageContract.Message;

using Serilog;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Qrame.Web.TransactServer.Extensions
{
    public class RawRequestBodyFormatter : InputFormatter
	{
		private ILogger logger { get; }

		public RawRequestBodyFormatter(ILogger logger)
		{
			this.logger = logger;

			SupportedMediaTypes.Add(new MediaTypeHeaderValue("qrame/plain-transact"));
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("qrame/json-transact"));
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("qrame/stream-transact"));
		}

		public override bool CanRead(InputFormatterContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			var contentType = context.HttpContext.Request.ContentType;
			if (string.IsNullOrEmpty(contentType) == true)
			{
				return false;
			}
			else if (contentType.IndexOf("qrame/plain-transact") > -1 || contentType.IndexOf("qrame/json-transact") > -1 || contentType.IndexOf("qrame/stream-transact") > -1)
			{
				return true;
			}

			return false;
		}


		public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
		{
			var request = context.HttpContext.Request;
			var contentType = context.HttpContext.Request.ContentType;

			if (string.IsNullOrEmpty(contentType) == false)
			{
				TransactionRequest transactionRequest = null;

				try
				{
					if (contentType.IndexOf("qrame/plain-transact") > -1 || contentType.IndexOf("qrame/json-transact") > -1)
					{
						using (var reader = new StreamReader(request.Body))
						{
							var content = await reader.ReadToEndAsync();
							transactionRequest = JsonConvert.DeserializeObject<TransactionRequest>(content);
							return await InputFormatterResult.SuccessAsync(transactionRequest);
						}
					}
					else if (contentType.IndexOf("qrame/stream-transact") > -1)
					{
						using (var ms = new MemoryStream(2048))
						{
							await request.Body.CopyToAsync(ms);
							var content = ms.ToArray();
							return await InputFormatterResult.SuccessAsync(MessagePackSerializer.Deserialize<TransactionRequest>(content));
						}
					}
				}
				catch (Exception exception)
				{
					logger.Error("[{LogCategory}] " + exception.ToMessage(), "ReadRequestBodyAsync");
				}
			}

			return await InputFormatterResult.FailureAsync();
		}
	}

	public class AuthorizationHeaderParameterOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (operation.Parameters == null)
			{
				operation.Parameters = new List<OpenApiParameter>();
			}

			if (context.ApiDescription.RelativePath == "api/Base64" || context.ApiDescription.RelativePath == "api/Transaction")
			{
			}
			else
			{
				operation.Parameters.Add(new OpenApiParameter
				{
					Name = "AuthorizationKey",
					In = ParameterLocation.Header,
					Description = "Qrame BP Access TokenID (SystemCode + RunningEnvironment + HostName)",
					Schema = new OpenApiSchema
					{
						Type = "String"
					}
				});
			}
		}
	}
}
