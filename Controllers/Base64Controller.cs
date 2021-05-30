using Microsoft.AspNetCore.Mvc;

using Qrame.CoreFX.ExtensionMethod;
using Qrame.Web.TransactServer.Extensions;

using System;
using System.Net;

namespace Qrame.Web.TransactServer.Controllers
{
    [Route("api/[controller]")]
	[ApiController]
	public class Base64Controller : ControllerBase
	{
		private Serilog.ILogger logger { get; }

		public Base64Controller(Serilog.ILogger logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// 매개변수로 전달된 문자열을 디코딩 된 문자열로 변환한 후 Base64로 반환합니다
		/// </summary>
		/// <param name="value"></param>
		/// <returns>Base64 문자열</returns>
		/// <example>
		/// http://localhost:7002/api/Base64/Encode?value={"ProjectID":"QAF","BusinessID":"DSO","TransactionID":"0001","FunctionID":"R01"}
		/// </example>
		[HttpGet("Encode")]
		public string Encode(string value)
		{
			string result = "";

			try
			{
				value = WebUtility.UrlDecode(value);
				result = value.EncodeBase64();
			}
			catch (Exception exception)
			{
				logger.Error("[{LogCategory}] " + exception.ToMessage(), "Base64 /Encode");
			}

			return result;
		}

		// 
		/// <summary>
		/// Base64로 인코드된 문자열을 디코드하여 반환합니다
		/// </summary>
		/// <param name="value">Base64로 인코드된 문자열</param>
		/// <returns>일반 문자열</returns>
		/// <example>
		/// http://localhost:7002/api/Base64/Decode?value=eyJQcm9qZWN0SUQiOiJRQUYiLCJCdXNpbmVzc0lEIjoiRFNPIiwiVHJhbnNhY3Rpb25JRCI6IjAwMDEiLCJGdW5jdGlvbklEIjoiUjAxIn0=
		/// </example>
		[HttpGet("Decode")]
		public string Decode(string value)
		{
			string result = "";

			try
			{
				result = value.DecodeBase64();
			}
			catch (Exception exception)
			{
				result = exception.ToMessage();
				logger.Error("[{LogCategory}] " + result, "Base64/Decode");
			}

			return result;
		}
	}
}