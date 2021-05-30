using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

using Qrame.Web.TransactServer.Entities;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Qrame.Web.TransactServer
{
	/// <summary>
	/// 전역 메모리에 올려서 관리할 환경설정
	/// </summary>
	public static class StaticConfig
	{
		public static bool IsConfigure = false;
		public static string RunningEnvironment = "D";
		public static string HostName = "QRAME-BIZ-" + (new Random()).Next(0, 10000).ToString();
		public static string SystemCode = "QAF";
		public static string BusinessContractBasePath = "";
		public static string ApplicationName = "";
		public static string ContentRootPath = "";
		public static string EnvironmentName = "";
		public static string WebRootPath = "";
		public static string AvailableEnvironment = "P,D,S";
		public static string MessageDataType = "json";
		public static Dictionary<string, string> RouteUrl = new Dictionary<string, string>();
		public static bool IsQueryIDHashing = false;
		public static bool IsTransactionLogging = false;
		public static string TransactionLogFilePath = "";
		public static bool UseApiAuthorize = false;
		public static bool IsExceptionDetailText = false;
		public static bool IsSwaggerUI = false;
		public static string WithOrigins = "";
		public static int CodeDataCacheTimeout = 20;
		public static string AuthorizationKey = "";
		public static List<PublicTransaction> PublicTransactions = new List<PublicTransaction>();

		public static StringBuilder BootstrappingVariables(IWebHostEnvironment environment)
		{
			var sb = new StringBuilder();
			var nl = Environment.NewLine;

			sb.Append($"ApplicationName: {environment.ApplicationName}{nl}");
			sb.Append($"ContentRootFileProvider: {environment.ContentRootFileProvider}{nl}");
			sb.Append($"ContentRootPath: {environment.ContentRootPath}{nl}");
			sb.Append($"EnvironmentName: {environment.EnvironmentName}{nl}");
			sb.Append($"WebRootFileProvider: {environment.WebRootFileProvider}{nl}");
			sb.Append($"WebRootPath: {environment.WebRootPath}{nl}");

			return sb;
		}

		public static StringBuilder BootstrappingVariables(IConfigurationRoot configuration)
		{
			var sb = new StringBuilder();
			var nl = Environment.NewLine;
			var rule = string.Concat(nl, new string('-', 40), nl);

			sb.Append($"{nl}");
			sb.Append($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
			sb.Append($"{nl}Configuration{rule}");
			foreach (var pair in configuration.AsEnumerable())
			{
				sb.Append($"{pair.Key}: {pair.Value}{nl}");
			}
			sb.Append(nl);

			sb.Append($"Environment Variables{rule}");
			var vars = Environment.GetEnvironmentVariables();
			foreach (var key in vars.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
			{
				var value = vars[key];
				sb.Append($"{key}: {value}{nl}");
			}

			return sb;
		}
	}
}