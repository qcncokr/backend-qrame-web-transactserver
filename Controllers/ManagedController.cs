using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Qrame.Web.TransactServer.Extensions;

using Serilog;

using System;

namespace Qrame.Web.TransactServer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[EnableCors()]
	public class ManagedController : ControllerBase
	{
		private IConfiguration configuration { get; }
		private IWebHostEnvironment environment { get; }

		public ManagedController(IWebHostEnvironment environment, IConfiguration configuration)
		{
			this.configuration = configuration;
			this.environment = environment;
		}

		/// <summary>
		/// 거래 계약 정보를 리셋
		/// </summary>
		/// <returns>리셋 작업 여부</returns>
		/// <example>
		/// http://localhost:7002/api/Managed/ResetContract
		/// </example>
		[HttpGet("ResetContract")]
		public ActionResult ResetContract()
		{
			ActionResult result = NotFound();
			string authorizationKey = Request.Headers["AuthorizationKey"];
			if (string.IsNullOrEmpty(authorizationKey) == true || StaticConfig.AuthorizationKey != authorizationKey)
			{
				result = BadRequest();
			}
			else
			{
				try
				{
					TransactionMapper.LoadBusinessContract(Log.Logger, configuration);

					result = Ok();
				}
				catch (Exception exception)
				{
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}
	}
}