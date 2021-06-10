using ChoETL;

using MessagePack;

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Qrame.CoreFX.Cryptography;
using Qrame.CoreFX.Cryptography.CryptoProvider;
using Qrame.CoreFX.ExtensionMethod;
using Qrame.CoreFX.Helper;
using Qrame.CoreFX.Messages;
using Qrame.Core.Library.MessageContract;
using Qrame.Core.Library.MessageContract.Contract;
using Qrame.Core.Library.MessageContract.DataObject;
using Qrame.Core.Library.MessageContract.Enumeration;
using Qrame.Core.Library.MessageContract.Message;
using Qrame.Web.TransactServer.Entities;
using Qrame.Web.TransactServer.Extensions;

using RestSharp;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Qrame.Core.Library;

namespace Qrame.Web.TransactServer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[EnableCors()]
	public class TransactionController : ControllerBase
	{
		private IMemoryCache memoryCache;
		LoggerFactory loggerFactory = null;
		private Serilog.ILogger logger { get; }

		public TransactionController(LoggerFactory loggerFactory, IMemoryCache memoryCache, Serilog.ILogger logger)
		{
			this.memoryCache = memoryCache;
			this.loggerFactory = loggerFactory;
			this.logger = logger;
		}

		/// <summary>
		/// 거래 정보가 존재하는지 확인합니다
		/// </summary>
		/// <param name="projectID">프로젝트ID</param>
		/// <param name="businessID">업무ID</param>
		/// <param name="transactionID">거래ID</param>
		/// <returns>조건에 해당하는 거래 갯수</returns>
		[HttpGet("Has")]
		public ActionResult Has(string projectID, string businessID, string transactionID)
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
					var value = TransactionMapper.HasCount(projectID, businessID, transactionID);
					result = Content(JsonConvert.SerializeObject(value), "application/json");
				}
				catch (Exception exception)
				{
					logger.Warning("[{LogCategory}] " + exception.ToMessage(), "Transaction/Has");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 거래 정보를 등록합니다
		/// </summary>
		/// <param name="businessContractFilePath">거래 정보 파일 상대경로입니다</param>
		/// <returns>정상적으로 등록되면 true를 아니면 false를 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Transaction/Add?businessContractFilePath=QAF\DSO\QAFDSO0001.json
		/// </example>
		[HttpGet("Add")]
		public ActionResult Add(string businessContractFilePath)
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
					var value = TransactionMapper.Add(businessContractFilePath);
					result = Content(JsonConvert.SerializeObject(value), "application/json");
				}
				catch (Exception exception)
				{
					logger.Warning("[{LogCategory}] " + exception.ToMessage(), "Transaction/Add");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 거래 정보를 삭제합니다
		/// </summary>
		/// <param name="businessContractFilePath">거래 정보 파일 상대경로입니다</param>
		/// <returns>정상적으로 삭제되면 true를 아니면 false를 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Transaction/Remove?businessContractFilePath=QAF\DSO\QAFDSO0001.json
		/// </example>
		[HttpGet("Remove")]
		public ActionResult Remove(string businessContractFilePath)
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
					var value = TransactionMapper.Remove(businessContractFilePath);
					result = Content(JsonConvert.SerializeObject(value), "application/json");
				}
				catch (Exception exception)
				{
					logger.Warning("[{LogCategory}] " + exception.ToMessage(), "Transaction/Remove");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 거래 정보를 갱신합니다
		/// </summary>
		/// <param name="businessContractFilePath">거래 정보 파일 상대경로입니다</param>
		/// <returns>정상적으로 삭제되면 true를 아니면 false를 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Transaction/Refresh?businessContractFilePath=QAF\MWA\CA020.json
		/// </example>
		[HttpGet("Refresh")]
		public ActionResult Refresh(string businessContractFilePath)
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
					TransactionMapper.Remove(businessContractFilePath);
					var value = TransactionMapper.Add(businessContractFilePath);
					result = Content(JsonConvert.SerializeObject(value), "application/json");
				}
				catch (Exception exception)
				{
					logger.Warning("[{LogCategory}] " + exception.ToMessage(), "Transaction/Refresh");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 코드캐시정보를 초기화합니다
		/// </summary>
		/// <param name="cacheKey">코드캐시 키입니다. 빈 값일 경우 전체 코드캐시 정보가 삭제됩니다</param>
		/// <returns>정상적으로 초기화되면 true를 아니면 false를 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Transaction/CacheClear?cacheKey=
		/// </example>
		[HttpGet("CacheClear")]
		public ActionResult CacheClear(string cacheKey)
		{
			ActionResult result = NotFound();
			try
			{
				result = Content(JsonConvert.SerializeObject(true), "application/json");
				if (string.IsNullOrEmpty(cacheKey) == true)
				{
					List<string> items = GetMemoryCacheKeys();
					foreach (string item in items)
					{
						memoryCache.Remove(item);
					}
				}
				else if (memoryCache.Get(cacheKey) != null)
				{
					memoryCache.Remove(cacheKey);
				}
				else
				{
					result = Content(JsonConvert.SerializeObject(false), "application/json");
				}
			}
			catch (Exception exception)
			{
				logger.Warning("[{LogCategory}] " + exception.ToMessage(), "Transaction/CacheClear");
				result = StatusCode(500, exception.ToMessage());
			}

			return result;
		}

		/// <summary>
		/// 캐시 데이터로 운영중인 캐시 키 목록을 조회합니다
		/// </summary>
		/// <returns>코드캐시 키 목록입니다</returns>
		/// <example>
		/// http://localhost:7002/api/Transaction/CacheKeys
		/// </example>
		[HttpGet("CacheKeys")]
		public ActionResult CacheKeys()
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
					List<string> items = GetMemoryCacheKeys();
					result = Content(JsonConvert.SerializeObject(items), "application/json");
				}
				catch (Exception exception)
				{
					logger.Warning("[{LogCategory}] " + exception.ToMessage(), "Transaction/Has");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		private List<string> GetMemoryCacheKeys()
		{
			var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
			var collection = field.GetValue(memoryCache) as ICollection;
			var items = new List<string>();
			if (collection != null)
			{
				foreach (var item in collection)
				{
					var methodInfo = item.GetType().GetProperty("Key");
					var val = methodInfo.GetValue(item);
					items.Add(val.ToString());
				}
			}

			return items;
		}

		/// <summary>
		/// 지정된 조건에 해당하는 거래 정보를 확인합니다
		/// </summary>
		/// <param name="base64Json">Base64로 인코드된 지정된 조건 문자열</param>
		/// <returns>거래 정보를 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Base64/Encode?value={"ApplicationID":"EWP","ProjectID":"ZZD","TransactionID":"TST010"}
		/// http://localhost:7002/api/Transaction/Get?base64Json=eyJQcm9qZWN0SUQiOiJRQUYiLCJCdXNpbmVzc0lEIjoiRFNPIiwiVHJhbnNhY3Rpb25JRCI6IjAwMDEiLCJGdW5jdGlvbklEIjoiUjAxMDAifQ==
		/// </example>
		[HttpGet("Get")]
		public ActionResult Get(string base64Json)
		{
			var definition = new
			{
				ApplicationID = "",
				ProjectID = "",
				TransactionID = ""
			};

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
					string json = base64Json.DecodeBase64();
					var model = JsonConvert.DeserializeAnonymousType(json, definition);

					BusinessContract businessContract = TransactionMapper.GetBusinessContracts().Select(p => p.Value).Where(p =>
						p.ApplicationID == model.ApplicationID &&
						p.ProjectID == model.ProjectID &&
						p.TransactionID == model.TransactionID).FirstOrDefault();

					if (businessContract != null)
					{
						var value = JsonConvert.SerializeObject(businessContract);
						result = Content(JsonConvert.SerializeObject(value), "application/json");
					}
				}
				catch (Exception exception)
				{
					logger.Error("[{LogCategory}] " + exception.ToMessage(), "Transaction/Get");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 지정된 조건에 해당하는 거래 정보 목록을 확인합니다
		/// </summary>
		/// <param name="base64Json">Base64로 인코드된 지정된 조건 문자열</param>
		/// <returns>거래 정보 목록을 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Base64/Encode?value={"ApplicationID":"EWP","ProjectID":"ZZD","TransactionID":"TST010"}
		/// http://localhost:7002/api/Transaction/Retrieve?base64Json=eyJQcm9qZWN0SUQiOiJRQUYiLCJCdXNpbmVzc0lEIjoiIiwiVHJhbnNhY3Rpb25JRCI6IiIsIkZ1bmN0aW9uSUQiOiIifQ==
		/// </example>
		[HttpGet("Retrieve")]
		public ActionResult Retrieve(string base64Json)
		{
			var definition = new
			{
				ApplicationID = "",
				ProjectID = "",
				TransactionID = ""
			};

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
					string json = base64Json.DecodeBase64();
					var model = JsonConvert.DeserializeAnonymousType(json, definition);

					if (string.IsNullOrEmpty(model.ApplicationID) == true || string.IsNullOrEmpty(model.ProjectID) == true)
					{
						return Content("필수 항목 확인", "text/html");
					}

					List<BusinessContract> businessContracts = null;

					var queryResults = TransactionMapper.GetBusinessContracts().Select(p => p.Value).Where(p =>
							p.ApplicationID == model.ApplicationID);

					if (string.IsNullOrEmpty(model.ProjectID) == false)
					{
						queryResults = queryResults.Where(p =>
							p.ProjectID == model.ProjectID);
					}

					if (string.IsNullOrEmpty(model.TransactionID) == false)
					{
						queryResults = queryResults.Where(p =>
							p.TransactionID == model.TransactionID);
					}

					businessContracts = queryResults.ToList();
					if (businessContracts != null)
					{
						var value = JsonConvert.SerializeObject(businessContracts);
						result = Content(JsonConvert.SerializeObject(value), "application/json");
					}
				}
				catch (Exception exception)
				{
					logger.Error("[{LogCategory}] " + exception.ToMessage(), "Transaction/Retrieve");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 거래 정보 거래 로그 설정을 변경합니다
		/// </summary>
		/// <returns>거래 로그 설정을 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Base64/Encode?value={"ApplicationID":"EWP","ProjectID":"ZZD","TransactionID":"TST010","ServiceID":"G01","TransactionLog":true}
		/// http://localhost:7002/api/Transaction/Log?base64Json=eyJQcm9qZWN0SUQiOiJTVlUiLCJCdXNpbmVzc0lEIjoiWlpEIiwiVHJhbnNhY3Rpb25JRCI6IlRTVDAxMCIsIkZ1bmN0aW9uSUQiOiJHMDEwMCIsIlRyYW5zYWN0aW9uTG9nIjp0cnVlfQ==
		/// </example>
		[HttpGet("Log")]
		public ActionResult Log(string base64Json)
		{
			var definition = new
			{
				ApplicationID = "",
				ProjectID = "",
				TransactionID = "",
				ServiceID = "",
				TransactionLog = false
			};

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
					string json = base64Json.DecodeBase64();
					var model = JsonConvert.DeserializeAnonymousType(json, definition);

					BusinessContract businessContract = TransactionMapper.GetBusinessContracts().Select(p => p.Value).Where(p =>
						p.ApplicationID == model.ApplicationID &&
						p.ProjectID == model.ProjectID &&
						p.TransactionID == model.TransactionID).FirstOrDefault();

					if (businessContract != null)
					{
						TransactionInfo transactionInfo = businessContract.Services.Select(p => p).Where(p =>
							p.ServiceID == model.ServiceID).FirstOrDefault();

						if (transactionInfo != null)
						{
							transactionInfo.TransactionLog = model.TransactionLog;
							var value = model.TransactionLog;
							result = Content(JsonConvert.SerializeObject(value), "application/json");
						}
						else
						{
							result = Content(JsonConvert.SerializeObject(false), "application/json");
						}
					}
					else
					{
						result = Content(JsonConvert.SerializeObject(false), "application/json");
					}
				}
				catch (Exception exception)
				{
					logger.Error("[{LogCategory}] " + exception.ToMessage(), "Transaction/Log");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 전체 거래 정보 목록을 확인합니다
		/// </summary>
		/// <returns>거래 정보 목록을 반환합니다</returns>
		/// <example>
		/// http://localhost:7002/api/Transaction/Meta
		/// </example>
		[HttpGet("Meta")]
		public ActionResult Meta()
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
					var businessContracts = TransactionMapper.GetBusinessContracts();

					if (businessContracts != null)
					{
						result = Content(JsonConvert.SerializeObject(businessContracts), "application/json");
					}
				}
				catch (Exception exception)
				{
					logger.Warning("[{LogCategory}] " + exception.ToMessage(), "Transaction/Meta");
					result = StatusCode(500, exception.ToMessage());
				}
			}

			return result;
		}

		/// <summary>
		/// 지정된 거래 명령 메시지 조건들을 기준으로, 프로그램 기능을 실행하여 데이터 집합 또는 JSON 결과를 반환합니다
		/// </summary>
		/// <param name="request">거래 명령 메시지에 필요한 구성정보입니다</param>
		/// <returns>데이터 집합 또는 JSON 결과를 저장할 응답 객체입니다</returns>
		[HttpPost]
		public ActionResult Execute(TransactionRequest request)
		{
			// 주요 구간 거래 명령 입력 횟수 및 명령 시간 기록
			TransactionResponse response = new TransactionResponse();
			response.Acknowledge = AcknowledgeType.Failure;

			try
			{
				#region 입력 기본값 구성

				if (request.TH.DAT_FMT == "")
				{
					request.TH.DAT_FMT = "J";
				}

				#endregion

				#region 기본 응답 정보 구성

				response.SH.FST_TMS_SYS_CD = StaticConfig.SystemCode;
				response.SH.TMS_SYS_CD = StaticConfig.SystemCode;
				response.SH.TMS_SYS_NODE_ID = StaticConfig.HostName;
				response.SH.RSP_RST_DSCD = "N";
				response.SH.MSG_OCC_SYS_CD = "S01";

				response.MDO = new MDO()
				{
					TRN_RET_DSCD = "ERM",
					MSG_CD = "Q00000" + nameof(MessageCode.T400),
					MAIN_MSG_TXT = MessageCode.T400,
					ADI_MSG = new List<ADI_MSG>()
				};

				var contentType = HttpContext.Request.ContentType;
				if (string.IsNullOrEmpty(contentType) == false && (contentType.IndexOf("qrame/plain-transact") > -1 || contentType.IndexOf("qrame/json-transact") > -1 || contentType.IndexOf("qrame/stream-transact") > -1) == false)
				{
					response.ExceptionText = $"'{contentType}' 입력 타입 확인 필요";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				if (request.TH.DAT_FMT == "T")
				{
					if (request.DAT.REQ_INPUT == null)
					{
						request.DAT.REQ_INPUT = new List<List<REQ_INPUT>>();
					}

					request.DAT.REQ_INPUT.Clear();

					foreach (string REQ_INPUTDATA in request.DAT.REQ_INPUTDATA)
					{
						var reqJArray = ToJson(DecryptInputData(REQ_INPUTDATA, request.TH.CRYPTO_DSCD));
						var reqInputs = JsonConvert.DeserializeObject<List<REQ_INPUT>>(reqJArray.ToString());

						foreach (var reqInput in reqInputs)
						{
							if (string.IsNullOrEmpty(reqInput.REQ_FIELD_ID) == true)
							{
								reqInput.REQ_FIELD_ID = "DEFAULT";
								reqInput.REQ_FIELD_DAT = "";
							}
						}
						request.DAT.REQ_INPUT.Add(reqInputs);
					}
				}
				else if (request.TH.DAT_FMT == "J")
				{
					if (request.TH.CRYPTO_DSCD == "C")
					{
						if (request.DAT.REQ_INPUT == null)
						{
							request.DAT.REQ_INPUT = new List<List<REQ_INPUT>>();
						}

						request.DAT.REQ_INPUT.Clear();

						foreach (string REQ_INPUTDATA in request.DAT.REQ_INPUTDATA)
						{
							request.DAT.REQ_INPUT.Add(JsonConvert.DeserializeObject<List<REQ_INPUT>>(DecryptInputData(REQ_INPUTDATA, request.TH.CRYPTO_DSCD)));
						}
					}
				}
				else
				{
					response.ExceptionText = $"데이터 포맷 '{request.TH.DAT_FMT }' 확인 필요";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				CopyHeader(request, response);

				string cacheKey = null;
				if (StaticConfig.IsCodeDataCache == true && request.TH.TRN_CD == "SMP110" && request.TH.TRN_SCRN_CD != "index")
				{
					if (request.DAT != null && request.DAT.REQ_INPUT != null && request.DAT.REQ_INPUT.Count > 0)
					{
						var inputs = request.DAT.REQ_INPUT[0];
						List<string> cacheKeys = new List<string>();
						for (int i = 0; i < inputs.Count; i++)
						{
							REQ_INPUT input = inputs[i];
							cacheKeys.Add(input.REQ_FIELD_ID + ":" + (input.REQ_FIELD_DAT == null ? "null" : input.REQ_FIELD_DAT.ToString()));
						}

						cacheKey = cacheKeys.ToJoin(";");

						TransactionResponse transactionResponse = new TransactionResponse();
						if (memoryCache.TryGetValue(cacheKey, out transactionResponse) == true)
						{
							transactionResponse.SH.TLM_RSP_DTM = DateTime.Now.ToString("yyyyMMddhhmmddsss");
							transactionResponse.ResponseID = string.Concat(StaticConfig.SystemCode, StaticConfig.HostName, request.SH.ENV_INF_DSCD, DateTime.Now.ToString("yyyyMMddhhmmddsss"));

							CopyHeader(request, transactionResponse);

							return Content(JsonConvert.SerializeObject(transactionResponse), "application/json");
						}
					}
					else
					{
						response.ExceptionText = $"PGM_ID '{request.TH.PGM_ID}', BIZ_ID '{request.TH.BIZ_ID}', TRN_CD '{request.TH.TRN_CD}' 코드 데이터 거래 Transaction 입력 전문 확인 필요";
						logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
						return Content(JsonConvert.SerializeObject(response), "application/json");
					}
				}

				BusinessContract businessContract = null;

				#endregion

				#region 입력 확인

				if (request.SH == null || request.TH == null || request.TCI == null || request.DAT == null)
				{
					response.ExceptionText = "잘못된 입력 전문";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH == null ? "empty_globalid" : request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				#endregion

				#region 입력 정보 검증

				if (request.Version == "001" || string.IsNullOrEmpty(request.SH.ENV_INF_DSCD) == true)
				{
					if (request.SH.TLM_ENCY_DSCD == "Y")
					{
						// SH/TH를 제외한 구간복호화 처리

						// 개별 필드 복호화
						foreach (List<REQ_INPUT> items in request.DAT.REQ_INPUT)
						{
							foreach (REQ_INPUT item in items)
							{
								// item.REQ_FIELD_DAT 값 복호화 처리
							}
						}
					}

					// 환경정보구분코드가 허용 범위인지 확인
					if (StaticConfig.AvailableEnvironment.IndexOf(request.SH.ENV_INF_DSCD) == -1)
					{
						response.ExceptionText = $"'{request.SH.ENV_INF_DSCD}' 환경정보구분코드가 허용 안됨";
						logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
						return Content(JsonConvert.SerializeObject(response), "application/json");
					}
				}
				else
				{
					response.ExceptionText = $"입력 전문 '{request.Version}' 버전 및 '{request.SH.ENV_INF_DSCD}' 환경정보구분코드 확인 필요";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				#endregion

				#region 거래 Transaction 입력 전문 확인

				response.SH.MSG_OCC_SYS_CD = "S02";
				businessContract = TransactionMapper.Get(request.TH.PGM_ID, request.TH.BIZ_ID, request.TH.TRN_CD);
				if (businessContract == null)
				{
					response.ExceptionText = $"PGM_ID '{request.TH.PGM_ID}', BIZ_ID '{request.TH.BIZ_ID}', TRN_CD '{request.TH.TRN_CD}' 거래 Transaction 입력 전문 확인 필요";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				#endregion

				#region 거래 매핑 정보 확인

				TransactionInfo transactionInfo = null;
				var services = from item in businessContract.Services
							   where item.ServiceID == request.TH.FUNC_CD
							   select item;

				if (services.Count() == 1)
				{
					transactionInfo = services.ToList()[0].DeepClone();
				}
				else if (services.Count() > 1)
				{
					response.ExceptionText = $"FUNC_CD '{request.TH.FUNC_CD}' 거래 매핑 중복 정보 확인 필요";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				if (transactionInfo == null)
				{
					response.ExceptionText = $"FUNC_CD '{request.TH.FUNC_CD}' 거래 매핑 정보 확인 필요";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				bool isAccessScreenID = false;
				if (transactionInfo.AccessScreenID == null)
				{
					if (businessContract.TransactionID == request.TH.TRN_SCRN_CD)
					{
						isAccessScreenID = true;
					}
				}
				else
				{
					if (transactionInfo.AccessScreenID.IndexOf(request.TH.TRN_SCRN_CD) > -1)
					{
						isAccessScreenID = true;
					}
					else if (businessContract.TransactionID == request.TH.TRN_SCRN_CD)
					{
						isAccessScreenID = true;
					}
				}

				if (isAccessScreenID == false)
				{
					if (StaticConfig.PublicTransactions.Count > 0)
					{
						PublicTransaction publicTransaction = StaticConfig.PublicTransactions.FirstOrDefault(p => p.ApplicationID == request.TH.PGM_ID && p.ProjectID == request.TH.BIZ_ID && p.TransactionID == request.TH.TRN_CD);
						if (publicTransaction != null)
						{
							isAccessScreenID = true;
						}
					}

					if (isAccessScreenID == false)
					{
						response.ExceptionText = $"TRN_SCRN_CD '{request.TH.TRN_SCRN_CD}' 요청 가능화면 거래 매핑 정보 확인 필요";
						logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
						return Content(JsonConvert.SerializeObject(response), "application/json");
					}
				}

				if (StaticConfig.IsTransactionLogging == true || transactionInfo.TransactionLog == true)
				{
					var transactionLogger = loggerFactory.CreateLogger($"{request.TH.PGM_ID}|{request.TH.BIZ_ID}|{request.TH.TRN_CD}|{request.TH.FUNC_CD}");
					transactionLogger.LogWarning($"Re GlobalID: {request.SH.GLBL_ID}, JSON: {JsonConvert.SerializeObject(request)}");
				}

				#endregion

				#region 거래 입력 정보 생성

				BearerToken bearerToken = null;
				string token = request.AccessTokenID;
				try
				{
					if (string.IsNullOrEmpty(token) == true)
					{
						if (StaticConfig.UseApiAuthorize == true && transactionInfo.Authorize == true)
						{
							response.ExceptionText = $"'{businessContract.ApplicationID}' 어플리케이션 또는 '{businessContract.ProjectID}' 프로젝트 권한 확인 필요";
							logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
							return Content(JsonConvert.SerializeObject(response), "application/json");
						}
					}
					else
					{
						if (token.IndexOf(".") == -1)
						{
							response.ExceptionText = "Bearer-Token 기본 무결성 확인 필요";
							logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
							return Content(JsonConvert.SerializeObject(response), "application/json");
						}

						string[] tokenArray = token.Split(".");
						string userID = tokenArray[0].DecodeBase64();

						if (userID != request.TH.OPR_NO)
						{
							response.ExceptionText = "Bearer-Token 사용자 무결성 확인 필요";
							logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
							return Content(JsonConvert.SerializeObject(response), "application/json");
						}

						token = tokenArray[1];
						bearerToken = JsonConvert.DeserializeObject<BearerToken>(token.DecryptAES(request.TH.OPR_NO.PadRight(32, ' ')));

						if (bearerToken == null)
						{
							response.ExceptionText = "Bearer-Token 정보 무결성 확인 필요";
							logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
							return Content(JsonConvert.SerializeObject(response), "application/json");
						}

						if (transactionInfo.Authorize == true)
						{
							if (bearerToken.Claim.AllowApplications.IndexOf(businessContract.ApplicationID) == -1)
							{
								response.ExceptionText = $"{businessContract.ApplicationID} 어플리케이션 권한 확인 필요";
								logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
								return Content(JsonConvert.SerializeObject(response), "application/json");
							}

							if (bearerToken.Claim.AllowProjects.IndexOf(businessContract.ProjectID) == -1)
							{
								response.ExceptionText = $"{businessContract.ProjectID} 프로젝트 권한 확인 필요";
								logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
								return Content(JsonConvert.SerializeObject(response), "application/json");
							}
						}
					}
				}
				catch (Exception exception)
				{
					response.ExceptionText = $"인증 또는 권한 확인 오류 - {exception.ToMessage()}";
					logger.Error("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				// 마스킹 해제 정보 확인
				if (request.TCI.Count > 0)
				{
					foreach (TCI item in request.TCI)
					{

					}
				}

				// 거래 Inputs/Outpus 정보 확인
				if (request.DTI != null && request.DTI.Length > 0)
				{
					if (transactionInfo.Inputs.Count == 0)
					{
						string[] dti = request.DTI.Split("|");
						string[] inputs = dti[0].Split(",");
						foreach (string item in inputs)
						{
							if (string.IsNullOrEmpty(item) == false)
							{
								transactionInfo.Inputs.Add(new ModelInputContract()
								{
									ModelID = "Dynamic",
									Fields = new List<string>(),
									TestValues = new List<TestValue>(),
									DefaultValues = new List<DefaultValue>(),
									Type = item,
									BaseFieldMappings = new List<BaseFieldMapping>(),
									ParameterHandling = item == "Row" ? "Rejected" : "ByPassing"
								});
							}
						}
					}

					if (transactionInfo.Outputs.Count == 0)
					{
						string[] dti = request.DTI.Split("|");
						string[] outputs = dti[1].Split(",");
						foreach (string item in outputs)
						{
							if (string.IsNullOrEmpty(item) == false)
							{
								transactionInfo.Outputs.Add(new ModelOutputContract()
								{
									ModelID = "Dynamic",
									Fields = new List<string>(),
									Type = item
								});
							}
						}
					}
				}

				TransactionObject transactionObject = new TransactionObject();
				transactionObject.LoadOptions = request.LoadOptions;
				if (transactionObject.LoadOptions != null && transactionObject.LoadOptions.Count > 0)
				{
				}

				transactionObject.RequestID = string.Concat(StaticConfig.SystemCode, StaticConfig.HostName, request.SH.ENV_INF_DSCD, request.TH.TRN_SCRN_CD, DateTime.Now.ToString("yyyyMMddhhmmddsss"));
				transactionObject.GlobalID = request.SH.GLBL_ID;
				transactionObject.TransactionID = string.Concat(businessContract.ApplicationID, "|", businessContract.TransactionProjectID, "|", request.TH.TRN_CD);
				transactionObject.ServiceID = request.TH.FUNC_CD;
				transactionObject.TransactionScope = transactionInfo.TransactionScope;
				transactionObject.ReturnType = transactionInfo.ReturnType;
				transactionObject.InputsItemCount = request.DAT.REQ_INPUT_CNT;

				List<Model> businessModels = businessContract.Models;
				List<ModelInputContract> inputContracts = transactionInfo.Inputs;
				List<ModelOutputContract> outputContracts = transactionInfo.Outputs;
				List<List<REQ_INPUT>> requestInputs = request.DAT.REQ_INPUT;

				// 입력 항목이 계약과 동일한지 확인
				if (inputContracts.Count > 0 && inputContracts.Count != request.DAT.REQ_INPUT_CNT.Count)
				{
					response.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력 항목이 계약과 동일한지 확인 필요";
					logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				// 입력 항목ID가 계약에 적합한지 확인
				int inputOffset = 0;
				Dictionary<string, List<List<REQ_INPUT>>> requestInputItems = new Dictionary<string, List<List<REQ_INPUT>>>();
				for (int i = 0; i < inputContracts.Count; i++)
				{
					ModelInputContract inputContract = inputContracts[i];
					Model model = businessModels.GetBusinessModel(inputContract.ModelID);

					if (model == null && inputContract.ModelID != "Unknown" && inputContract.ModelID != "Dynamic")
					{
						response.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 '{inputContract.ModelID}' 입력 모델 ID가 계약에 있는지 확인";
						logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
						return Content(JsonConvert.SerializeObject(response), "application/json");
					}

					int inputCount = request.DAT.REQ_INPUT_CNT[i];
					if (inputContract.Type == "Row" && inputCount != 1)
					{
						response.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력 항목이 계약과 동일한지 확인 필요";
						logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
						return Content(JsonConvert.SerializeObject(response), "application/json");
					}

					if (inputContract.ParameterHandling == "Rejected" && inputCount == 0)
					{
						response.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 필요한 입력 항목이 필요";
						logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
						return Content(JsonConvert.SerializeObject(response), "application/json");
					}

					if (inputContract.ParameterHandling == "ByPassing" && inputCount == 0)
					{
						continue;
					}

					List<REQ_INPUT> requestInput = null;
					if (inputContract.ParameterHandling == "DefaultValue" && inputCount == 0)
					{
						if (inputContract.DefaultValues == null)
						{
							response.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 필요한 기본값 입력 항목 확인 필요";
							logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
							return Content(JsonConvert.SerializeObject(response), "application/json");
						}

						request.DAT.REQ_INPUT_CNT[i] = 1;
						transactionObject.InputsItemCount[i] = 1;
						inputCount = 1;
						requestInput = new List<REQ_INPUT>();

						int fieldIndex = 0;
						foreach (string REQ_FIELD_ID in inputContract.Fields)
						{
							DefaultValue defaultValue = inputContract.DefaultValues[fieldIndex];
							DbColumn column = null;

							if (model == null)
							{
								column = new DbColumn()
								{
									Name = REQ_FIELD_ID,
									Length = -1,
									DataType = "String",
									Default = "",
									Require = false
								};
							}
							else
							{
								column = model.Columns.FirstOrDefault(p => p.Name == REQ_FIELD_ID);
							}

							REQ_INPUT tempReqInput = new REQ_INPUT();
							tempReqInput.REQ_FIELD_ID = REQ_FIELD_ID;

							SetInputDefaultValue(defaultValue, column, tempReqInput);

							requestInput.Add(tempReqInput);

							fieldIndex = fieldIndex + 1;
						}

						requestInputs.Add(requestInput);
					}
					else
					{
						requestInput = requestInputs[inputOffset];
					}

					if (inputContract.ModelID != "Unknown" && inputContract.ModelID != "Dynamic")
					{
						foreach (var item in requestInput)
						{
							if (inputContract.Fields.Contains(item.REQ_FIELD_ID) == false)
							{
								if (item.REQ_FIELD_ID == "Flag")
								{
								}
								else
								{
									response.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 '{item.REQ_FIELD_ID}' 항목 ID가 계약에 있는지 확인";
									logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
									return Content(JsonConvert.SerializeObject(response), "application/json");
								}
							}
						}
					}

					requestInputItems.Add(inputContract.ModelID + i.ToString(), requestInputs.Skip(inputOffset).Take(inputCount).ToList());
					inputOffset = inputOffset + inputCount;
				}

				List<List<TransactField>> transactInputs = new List<List<TransactField>>();

				int index = 0;
				foreach (var requestInputItem in requestInputItems)
				{
					string modelID = requestInputItem.Key;
					List<List<REQ_INPUT>> inputItems = requestInputItem.Value;

					// 입력 정보 생성
					ModelInputContract inputContract = inputContracts[index];
					Model model = businessModels.GetBusinessModel(inputContract.ModelID);

					if (model == null && inputContract.ModelID != "Unknown" && inputContract.ModelID != "Dynamic")
					{
						response.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 '{inputContract.ModelID}' 입력 모델 ID가 계약에 있는지 확인";
						logger.Warning("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
						return Content(JsonConvert.SerializeObject(response), "application/json");
					}

					for (int i = 0; i < inputItems.Count; i++)
					{
						List<TransactField> transactInput = new List<TransactField>();
						List<REQ_INPUT> requestInput = inputItems[i];

						foreach (var item in requestInput)
						{
							DbColumn column = null;

							if (model == null)
							{
								column = new DbColumn()
								{
									Name = item.REQ_FIELD_ID,
									Length = -1,
									DataType = "String",
									Default = "",
									Require = false
								};
							}
							else
							{
								column = model.Columns.FirstOrDefault(p => p.Name == item.REQ_FIELD_ID);
							}

							if (column == null)
							{
								response.ExceptionText = $"'{model.Name}' 입력 모델에 '{item.REQ_FIELD_ID}' 항목 확인 필요";
								return Content(JsonConvert.SerializeObject(response), "application/json");
							}
							else
							{
								TransactField transactField = new TransactField();
								transactField.FieldID = item.REQ_FIELD_ID;
								transactField.Length = column.Length;
								transactField.DataType = column.DataType.ToString();

								if (item.REQ_FIELD_DAT == null)
								{
									if (column.Require == true)
									{
										transactField.Value = column.Default;
									}
									else
									{
										transactField.Value = null;
									}
								}
								else
								{
									if (item.REQ_FIELD_DAT.ToString() == "[DbNull]")
									{
										transactField.Value = null;
									}
									else
									{
										transactField.Value = item.REQ_FIELD_DAT;
									}
								}
								transactField.Value = item.REQ_FIELD_DAT == null ? (column.Require == true ? column.Default : null) : item.REQ_FIELD_DAT;

								transactInput.Add(transactField);
							}
						}

						JObject bearerFields = bearerToken == null ? null : bearerToken.Addtional as JObject;
						if (bearerFields != null)
						{
							foreach (var item in bearerFields)
							{
								string REQ_FIELD_ID = "$" + item.Key;
								JToken jToken = item.Value;
								if (jToken == null)
								{
									response.ExceptionText = $"{REQ_FIELD_ID} Bearer 필드 확인 필요";
									return Content(JsonConvert.SerializeObject(response), "application/json");
								}

								DbColumn column = null;

								column = new DbColumn()
								{
									Name = REQ_FIELD_ID,
									Length = -1,
									DataType = "String",
									Default = "",
									Require = false
								};

								TransactField transactField = new TransactField();
								transactField.FieldID = REQ_FIELD_ID;
								transactField.Length = column.Length;
								transactField.DataType = column.DataType.ToString();

								object REQ_FIELD_DAT = null;
								if (jToken is JValue)
								{
									REQ_FIELD_DAT = jToken.ToObject<string>();
								}
								else if (jToken is JObject)
								{
									REQ_FIELD_DAT = jToken.ToString();
								}
								else if (jToken is JArray)
								{
									REQ_FIELD_DAT = jToken.ToArray();
								}

								if (REQ_FIELD_DAT == null)
								{
									if (column.Require == true)
									{
										transactField.Value = column.Default;
									}
									else
									{
										transactField.Value = null;
									}
								}
								else
								{
									if (REQ_FIELD_DAT.ToString() == "[DbNull]")
									{
										transactField.Value = null;
									}
									else
									{
										transactField.Value = REQ_FIELD_DAT;
									}
								}
								transactField.Value = REQ_FIELD_DAT == null ? (column.Require == true ? column.Default : null) : REQ_FIELD_DAT;

								transactInput.Add(transactField);
							}
						}

						transactInputs.Add(transactInput);
					}

					index = index + 1;
				}

				transactionObject.Inputs = transactInputs;

				#endregion

				#region 명령 구분 확인(Console, DataTransaction, ApiServer, FileServer)

				request.TH.CMD_TYPE = transactionInfo.TransactionType;
				ApplicationResponse applicationResponse = null;

				switch (transactionInfo.TransactionType)
				{
					case "C":
					case "T":
					case "D":
					case "A":
					case "F":
						applicationResponse = DataTransaction(request, response, transactionInfo, transactionObject, businessModels, inputContracts, outputContracts);
						break;
					case "S":
						applicationResponse = SequentialDataTransaction(request, response, transactionInfo, transactionObject, businessModels, inputContracts, outputContracts);
						if (string.IsNullOrEmpty(applicationResponse.ExceptionText) == true)
						{
							applicationResponse = SequentialResultContractValidation(applicationResponse, request, response, transactionInfo, transactionObject, businessModels, outputContracts);
						}
						break;
					case "R":
						applicationResponse = new ApplicationResponse();
						applicationResponse.ExceptionText = "TransactionType 확인 필요";
						break;
					default:
						applicationResponse = new ApplicationResponse();
						applicationResponse.ExceptionText = "TransactionType 확인 필요";
						break;
				}

				if (string.IsNullOrEmpty(applicationResponse.ExceptionText) == false)
				{
					response.ExceptionText = applicationResponse.ExceptionText;
					return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				#endregion

				#region 거래 명령 실행 및 결과 반환

				switch (transactionInfo.ReturnType)
				{
					case "DataSet":
						return File(applicationResponse.ResultObject as byte[], "application/octet-stream");
					case "Scalar":
						return Content(applicationResponse.ResultObject == null ? "" : applicationResponse.ResultObject.ToString(), "text/html");
					case "NonQuery":
						return Content(applicationResponse.ResultInteger.ToString(), "text/html");
					case "Xml":
						return Content(applicationResponse.ResultObject == null ? "" : applicationResponse.ResultObject.ToString(), "application/xml");
					case "DynamicJson":
						return Content(applicationResponse.ResultJson == null ? "" : applicationResponse.ResultJson.ToString(), "application/json");
					default:
						response.SH = request.SH;
						response.TH = request.TH;

						response.MDO.TRN_RET_DSCD = "NRM";
						response.MDO.MSG_CD = nameof(MessageCode.T200);
						response.MDO.MAIN_MSG_TXT = MessageCode.T200;

						response.TCO = new List<TCO>();
						response.TMO = new TMO();
						response.TMO.OUP_SCRN_DSCD = request.TH.TRN_SCRN_CD;

						if (response.TH.LQTY_DAT_PRC_DIS == "Y")
						{
							foreach (RES_OUTPUT item in response.DAT.RES_OUTPUT)
							{
								// item.RES_DAT
								// 대량 거래일때 데이터 압축 처리
							}
						}

						// 거래 명령 응답 시간 기록
						// 거래 명령 결과 확인
						// 거래 명령 성공/ 실패 응답 횟수 기록
						// 거래 Transaction 응답 전문 생성
						// 응답 반환

						response.SH.RSP_RST_DSCD = "Y";
						response.SH.TLM_RSP_DTM = DateTime.Now.ToString("yyyyMMddhhmmddsss");
						response.ResponseID = string.Concat(StaticConfig.SystemCode, StaticConfig.HostName, request.SH.ENV_INF_DSCD, DateTime.Now.ToString("yyyyMMddhhmmddsss"));
						response.Acknowledge = AcknowledgeType.Success;
						response.TH.SMLT_TRN_DSCD = transactionInfo.ReturnType;

						if (response.TH.DAT_FMT == "T")
						{
							List<string> resultMeta = applicationResponse.ResultMeta;
							int i = 0;
							foreach (RES_OUTPUT RES_OUTPUT in response.DAT.RES_OUTPUT)
							{
								JToken RES_DAT = RES_OUTPUT.RES_DAT as JToken;
								if (RES_DAT != null)
								{
									if (RES_DAT is JObject)
									{
										var names = RES_DAT.ToObject<JObject>().Properties().Select(p => p.Name).ToList();
										foreach (var item in names)
										{
											var jtoken = RES_DAT[item];
											string data = jtoken.ToString();
											if (data.StartsWith('"') == true)
											{
												RES_DAT[item] = "\"" + data;
											}

											if (data.EndsWith('"') == true)
											{
												RES_DAT[item] = RES_DAT[item].ToString() + "\"";
											}
										}
									}
									else if (RES_DAT is JArray)
									{
										var jtokens = RES_DAT.ToObject<JArray>().ToList();
										foreach (var jtoken in jtokens)
										{
											var names = jtoken.ToObject<JObject>().Properties().Select(p => p.Name).ToList();
											foreach (var item in names)
											{
												var jtoken1 = jtoken[item];
												string data = jtoken1.ToString();
												if (data.ToString().StartsWith('"') == true)
												{
													jtoken[item] = "\"" + data.ToString();
												}

												if (data.ToString().EndsWith('"') == true)
												{
													jtoken[item] = jtoken[item].ToString() + "\"";
												}
											}
										}
									}

									string meta = resultMeta[i];
									var jsonReader = new StringReader(RES_DAT.ToString());
									using (ChoJSONReader choJSONReader = new ChoJSONReader(jsonReader))
									{
										var stringBuilder = new StringBuilder();
										using (var choCSVWriter = new ChoCSVWriter(stringBuilder, new ChoCSVRecordConfiguration()
										{
											Delimiter = "｜",
											EOLDelimiter = "↵"
										}).WithFirstLineHeader().QuoteAllFields(false))
										{
											choCSVWriter.Write(choJSONReader);
										}

										if (request.TH.CRYPTO_DSCD == "C")
										{
											RES_OUTPUT.RES_DAT = LZStringHelper.CompressToBase64(meta + "＾" + stringBuilder.ToString().Replace("\"\"", "\""));
										}
										else
										{
											RES_OUTPUT.RES_DAT = meta + "＾" + stringBuilder.ToString().Replace("\"\"", "\"");
										}
									}
								}

								i = i + 1;
							}
						}
						else
						{
							List<string> resultMeta = applicationResponse.ResultMeta;
							int i = 0;
							foreach (RES_OUTPUT RES_OUTPUT in response.DAT.RES_OUTPUT)
							{
								JToken RES_DAT = RES_OUTPUT.RES_DAT as JToken;
								if (RES_DAT != null)
								{
									if (request.TH.CRYPTO_DSCD == "C")
									{
										RES_OUTPUT.RES_DAT = LZStringHelper.CompressToBase64(JsonConvert.SerializeObject(RES_DAT));
									}
								}

								i = i + 1;
							}
						}

						if (StaticConfig.IsTransactionLogging == true || transactionInfo.TransactionLog == true)
						{
							var transactionLogger = loggerFactory.CreateLogger($"{request.TH.PGM_ID}|{request.TH.BIZ_ID}|{request.TH.TRN_CD}|{request.TH.FUNC_CD}");
							transactionLogger.LogWarning($"Response GlobalID: {request.SH.GLBL_ID}, JSON: {JsonConvert.SerializeObject(response)}");
						}

						if (request.TH.TRN_CD == "SMP110" && request.TH.TRN_SCRN_CD != "index" && string.IsNullOrEmpty(cacheKey) == false)
						{
							if (memoryCache.Get(cacheKey) == null)
							{
								memoryCache.Set(cacheKey, response, TimeSpan.FromMinutes(StaticConfig.CodeDataCacheTimeout));
							}
						}

						return Content(JsonConvert.SerializeObject(response), "application/json");
				}

				#endregion
			}
			catch (Exception exception)
			{
				response.ExceptionText = exception.ToMessage();
				logger.Error("[{LogCategory}] [{GlobalID}] " + response.ExceptionText, "Transaction/Execute", request.SH.GLBL_ID);
			}

			if (StaticConfig.IsTransactionLogging == true)
			{
				var transactionLogger = loggerFactory.CreateLogger($"{request.TH.PGM_ID}|{request.TH.BIZ_ID}|{request.TH.TRN_CD}|{request.TH.FUNC_CD}");
				transactionLogger.LogWarning($"Response GlobalID: {request.SH.GLBL_ID}, JSON: {JsonConvert.SerializeObject(response)}");
			}

			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		private static string DecryptInputData(string inputData, string decrptCode)
		{
			string result = "";
			switch (decrptCode)
			{
				case "C":
					result = LZStringHelper.DecompressFromBase64(inputData);
					break;
				default:
					result = inputData;
					break;
			}

			return result;
		}

		private ApplicationResponse SequentialResultContractValidation(ApplicationResponse applicationResponse, TransactionRequest request, TransactionResponse response, TransactionInfo transactionInfo, TransactionObject transactionObject, List<Model> businessModels, List<ModelOutputContract> outputContracts)
		{
			List<FieldData> outputs = JsonConvert.DeserializeObject<List<FieldData>>(applicationResponse.ResultJson);

			if (outputContracts.Count > 0)
			{
				if (outputContracts.Where(p => p.Type == "Dynamic").Count() > 0)
				{
				}
				else
				{
					int additionCount = outputContracts.Where(p => p.Type == "Addition").Count();
					if ((outputContracts.Count - additionCount + (additionCount > 0 ? 1 : 0)) != outputs.Count)
					{
						applicationResponse.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 출력 모델 개수 확인 필요, 계약 건수 - '{outputContracts.Count}', 응답 건수 - '{outputs.Count}'";
						return applicationResponse;
					}

					var lastIndex = outputs.Count - 1;
					for (int i = 0; i < outputs.Count; i++)
					{
						FieldData output = outputs[i];
						ModelOutputContract outputContract = outputContracts[i];
						Model model = businessModels.GetBusinessModel(outputContract.ModelID);

						if (model == null && outputContract.ModelID != "Unknown" && outputContract.ModelID != "Dynamic")
						{
							applicationResponse.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 '{outputContract.ModelID}' 출력 모델 ID가 계약에 있는지 확인";
							return applicationResponse;
						}

						RES_OUTPUT responseData = new RES_OUTPUT();
						responseData.RES_FIELD_ID = output.FieldID;

						if (additionCount > 0 && i == lastIndex)
						{
							continue;
						}

						dynamic tempParseJson = null;
						if (model == null)
						{
							if (outputContract.ModelID == "Unknown")
							{
								if (outputContract.Type == "Form")
								{
									tempParseJson = JObject.Parse(output.Value.ToString());
									JObject jObject = (JObject)tempParseJson;
									foreach (JProperty property in jObject.Properties())
									{
										if (outputContract.Fields.Contains(property.Name) == false)
										{
											applicationResponse.ExceptionText = $"{outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
											return applicationResponse;
										}
									}
								}
								else if (outputContract.Type == "Grid" || outputContract.Type == "DataSet")
								{
									tempParseJson = JArray.Parse(output.Value.ToString());
									if (tempParseJson.Count > 0)
									{
										JObject jObject = (JObject)tempParseJson.First;
										foreach (JProperty property in jObject.Properties())
										{
											if (outputContract.Fields.Contains(property.Name) == false)
											{
												applicationResponse.ExceptionText = $"{outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
												return applicationResponse;
											}
										}
									}
								}
								else if (outputContract.Type == "Chart")
								{
									tempParseJson = JToken.Parse(output.Value.ToString());
								}
								else if (outputContract.Type == "Dynamic")
								{
									tempParseJson = JToken.Parse(output.Value.ToString());
								}
							}
							else if (outputContract.ModelID == "Dynamic")
							{
								if (outputContract.Type == "Form")
								{
									tempParseJson = JObject.Parse(output.Value.ToString());
								}
								else if (outputContract.Type == "Grid")
								{
									tempParseJson = JArray.Parse(output.Value.ToString());
								}
								else if (outputContract.Type == "Chart")
								{
									tempParseJson = JToken.Parse(output.Value.ToString());
								}
								else if (outputContract.Type == "DataSet")
								{
									tempParseJson = JToken.Parse(output.Value.ToString());
								}
								else if (outputContract.Type == "Dynamic")
								{
									tempParseJson = JToken.Parse(output.Value.ToString());
								}
							}
						}
						else
						{
							if (outputContract.Type == "Form")
							{
								tempParseJson = JObject.Parse(output.Value.ToString());
								JObject jObject = (JObject)tempParseJson;
								foreach (JProperty property in jObject.Properties())
								{
									if (model.Columns.IsContain(property.Name) == false)
									{
										applicationResponse.ExceptionText = $"'{model.Name}' {outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
										return applicationResponse;
									}
								}
							}
							else if (outputContract.Type == "Grid" || outputContract.Type == "DataSet")
							{
								tempParseJson = JArray.Parse(output.Value.ToString());
								if (tempParseJson.Count > 0)
								{
									JObject jObject = (JObject)tempParseJson.First;
									foreach (JProperty property in jObject.Properties())
									{
										if (model.Columns.IsContain(property.Name) == false)
										{
											applicationResponse.ExceptionText = $"'{model.Name}' {outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
											return applicationResponse;
										}
									}
								}
							}
							else if (outputContract.Type == "Chart")
							{
								tempParseJson = JToken.Parse(output.Value.ToString());
							}
							else if (outputContract.Type == "Dynamic")
							{
								tempParseJson = JToken.Parse(output.Value.ToString());
							}
						}
					}
				}
			}

			return applicationResponse;
		}

		private ApplicationResponse SequentialDataTransaction(TransactionRequest request, TransactionResponse response, TransactionInfo transactionInfo, TransactionObject transactionObject, List<Model> businessModels, List<ModelInputContract> inputContracts, List<ModelOutputContract> outputContracts)
		{
			ApplicationResponse applicationResponse = null;
			foreach (SequentialOption sequentialOption in transactionInfo.SequentialOptions)
			{
				List<ModelInputContract> sequentialinputContracts = new List<ModelInputContract>();
				foreach (int inputIdex in sequentialOption.ServiceInputFields)
				{
					sequentialinputContracts.Add(inputContracts[inputIdex]);
				}

				List<ModelOutputContract> sequentialOutputContracts = new List<ModelOutputContract>();
				foreach (ModelOutputContract modelOutputContract in sequentialOption.ServiceOutputs)
				{
					sequentialOutputContracts.Add(modelOutputContract);
				}

				applicationResponse = SequentialRequestDataTransaction(request, transactionObject, sequentialOption, sequentialinputContracts, sequentialOutputContracts);

				if (string.IsNullOrEmpty(applicationResponse.ExceptionText) == false)
				{
					return applicationResponse;
				}

				string transactionID = string.IsNullOrEmpty(sequentialOption.TransactionID) == true ? request.TH.TRN_CD : sequentialOption.TransactionID;
				string serviceID = string.IsNullOrEmpty(sequentialOption.ServiceID) == true ? transactionObject.ServiceID : sequentialOption.ServiceID;

				response.DAT = new OUTPUT_DAT();
				response.DAT.RES_TX_MAP_ID = request.DAT.REQ_TX_MAP_ID;
				response.DAT.RES_OUTPUT = new List<RES_OUTPUT>();

				if (transactionInfo.ReturnType == "Json")
				{
					List<FieldData> outputs = JsonConvert.DeserializeObject<List<FieldData>>(applicationResponse.ResultJson);

					if (sequentialOption.ResultHandling == "ResultSet")
					{
						#region ResultSet

						if (sequentialOutputContracts.Count > 0)
						{
							if (sequentialOutputContracts.Where(p => p.Type == "Dynamic").Count() > 0)
							{
								for (int i = 0; i < outputs.Count; i++)
								{
									FieldData output = outputs[i];
									dynamic outputJson = JToken.Parse(output.Value.ToString());
									RES_OUTPUT responseData = new RES_OUTPUT();
									responseData.RES_FIELD_ID = output.FieldID;
									responseData.RES_DAT = outputJson;
									response.DAT.RES_OUTPUT.Add(responseData);
								}
							}
							else
							{
								int additionCount = sequentialOutputContracts.Where(p => p.Type == "Addition").Count();
								if ((sequentialOutputContracts.Count - additionCount + (additionCount > 0 ? 1 : 0)) != outputs.Count)
								{
									applicationResponse.ExceptionText = $"'{transactionID}|{serviceID}' 거래 입력에 출력 모델 개수 확인 필요, 계약 건수 - '{sequentialOutputContracts.Count}', 응답 건수 - '{outputs.Count}'";
									return applicationResponse;
								}

								var lastIndex = outputs.Count - 1;
								for (int i = 0; i < outputs.Count; i++)
								{
									FieldData output = outputs[i];
									ModelOutputContract outputContract = sequentialOutputContracts[i];
									Model model = businessModels.GetBusinessModel(outputContract.ModelID);

									if (model == null && outputContract.ModelID != "Unknown" && outputContract.ModelID != "Dynamic")
									{
										applicationResponse.ExceptionText = $"'{transactionID}|{serviceID}' 거래 입력에 '{outputContract.ModelID}' 출력 모델 ID가 계약에 있는지 확인";
										return applicationResponse;
									}

									dynamic outputJson = null;
									RES_OUTPUT responseData = new RES_OUTPUT();
									responseData.RES_FIELD_ID = output.FieldID;

									if (additionCount > 0 && i == lastIndex)
									{
										try
										{
											JArray messagesJson = JArray.Parse(output.Value.ToString());
											for (int j = 0; j < messagesJson.Count; j++)
											{
												ADI_MSG adiMessage = new ADI_MSG();
												adiMessage.ADI_MSG_CD = messagesJson[j]["MSG_CD"].ToString();
												adiMessage.ADI_MSG_TXT = messagesJson[j]["MSG_TXT"].ToString();
												response.MDO.ADI_MSG.Add(adiMessage);
											}
										}
										catch (Exception exception)
										{
											ADI_MSG adiMessage = new ADI_MSG();
											adiMessage.ADI_MSG_CD = "E001";
											adiMessage.ADI_MSG_TXT = exception.ToMessage();
											logger.Warning("[{LogCategory}] [{GlobalID}] " + adiMessage.ADI_MSG_TXT, "Transaction/ADI_MSG", request.SH.GLBL_ID);
											response.MDO.ADI_MSG.Add(adiMessage);
										}
										continue;
									}

									if (model == null)
									{
										if (outputContract.ModelID == "Unknown")
										{
											if (outputContract.Type == "Form")
											{
												outputJson = JObject.Parse(output.Value.ToString());
												JObject jObject = (JObject)outputJson;
												foreach (JProperty property in jObject.Properties())
												{
													if (outputContract.Fields.Contains(property.Name) == false)
													{
														applicationResponse.ExceptionText = $"{outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
														return applicationResponse;
													}
												}
											}
											else if (outputContract.Type == "Grid" || outputContract.Type == "DataSet")
											{
												outputJson = JArray.Parse(output.Value.ToString());
												if (outputJson.Count > 0)
												{
													JObject jObject = (JObject)outputJson.First;
													foreach (JProperty property in jObject.Properties())
													{
														if (outputContract.Fields.Contains(property.Name) == false)
														{
															applicationResponse.ExceptionText = $"{outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
															return applicationResponse;
														}
													}
												}
											}
											else if (outputContract.Type == "Chart")
											{
												outputJson = JToken.Parse(output.Value.ToString());
											}
											else if (outputContract.Type == "Dynamic")
											{
												outputJson = JToken.Parse(output.Value.ToString());
											}
										}
										else if (outputContract.ModelID == "Dynamic")
										{
											if (outputContract.Type == "Form")
											{
												outputJson = JObject.Parse(output.Value.ToString());
											}
											else if (outputContract.Type == "Grid")
											{
												outputJson = JArray.Parse(output.Value.ToString());
											}
											else if (outputContract.Type == "Chart")
											{
												outputJson = JToken.Parse(output.Value.ToString());
											}
											else if (outputContract.Type == "DataSet")
											{
												outputJson = JToken.Parse(output.Value.ToString());
											}
											else if (outputContract.Type == "Dynamic")
											{
												outputJson = JToken.Parse(output.Value.ToString());
											}
										}
									}
									else
									{
										if (outputContract.Type == "Form")
										{
											outputJson = JObject.Parse(output.Value.ToString());
											JObject jObject = (JObject)outputJson;
											foreach (JProperty property in jObject.Properties())
											{
												if (model.Columns.IsContain(property.Name) == false)
												{
													applicationResponse.ExceptionText = $"'{model.Name}' {outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
													return applicationResponse;
												}
											}
										}
										else if (outputContract.Type == "Grid" || outputContract.Type == "DataSet")
										{
											outputJson = JArray.Parse(output.Value.ToString());
											if (outputJson.Count > 0)
											{
												JObject jObject = (JObject)outputJson.First;
												foreach (JProperty property in jObject.Properties())
												{
													if (model.Columns.IsContain(property.Name) == false)
													{
														applicationResponse.ExceptionText = $"'{model.Name}' {outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
														return applicationResponse;
													}
												}
											}
										}
										else if (outputContract.Type == "Chart")
										{
											outputJson = JToken.Parse(output.Value.ToString());
										}
										else if (outputContract.Type == "Dynamic")
										{
											outputJson = JToken.Parse(output.Value.ToString());
										}
									}

									responseData.RES_DAT = outputJson;
									response.DAT.RES_OUTPUT.AddUnique(sequentialOption.ResultOutputFields[i], responseData);
								}
							}
						}

						#endregion
					}
					else if (sequentialOption.ResultHandling == "FieldMapping")
					{
						#region FieldMapping

						if (outputs.Count() > 0)
						{
							foreach (int inputIdex in sequentialOption.TargetInputFields)
							{
								ModelInputContract modelInputContract = inputContracts[inputIdex];
								MappingTransactionInputsValue(transactionObject, inputIdex, modelInputContract, JObject.Parse(outputs[0].Value.ToString()));
							}
						}

						#endregion
					}
				}
				else
				{
					applicationResponse.ExceptionText = $"'{transactionID}|{serviceID}' 순차 처리 되는 거래 응답은 Json만 지원";
					return applicationResponse;
				}
			}

			return applicationResponse;
		}

		private ApplicationResponse DataTransaction(TransactionRequest request, TransactionResponse response, TransactionInfo transactionInfo, TransactionObject transactionObject, List<Model> businessModels, List<ModelInputContract> inputContracts, List<ModelOutputContract> outputContracts)
		{
			ApplicationResponse applicationResponse = RequestDataTransaction(request, transactionObject, inputContracts, outputContracts);

			if (string.IsNullOrEmpty(applicationResponse.ExceptionText) == false)
			{
				return applicationResponse;
			}

			response.DAT = new OUTPUT_DAT();
			response.DAT.RES_TX_MAP_ID = request.DAT.REQ_TX_MAP_ID;
			response.DAT.RES_OUTPUT = new List<RES_OUTPUT>();

			switch (transactionInfo.ReturnType)
			{
				case "CodeHelp":
					ResponseCodeObject responseCodeObject = JsonConvert.DeserializeObject<ResponseCodeObject>(applicationResponse.ResultJson);

					REQ_INPUT input = request.DAT.REQ_INPUT[0].Where(p => p.REQ_FIELD_ID == "CodeHelpID").FirstOrDefault();

					response.DAT.RES_OUTPUT.Add(new RES_OUTPUT()
					{
						RES_FIELD_ID = input == null ? "CODEHELP" : input.REQ_FIELD_DAT.ToString(),
						RES_DAT = responseCodeObject
					});

					break;
				case "SchemeOnly":
					JObject resultJson = JObject.Parse(applicationResponse.ResultJson);
					foreach (JProperty property in resultJson.Properties())
					{
						response.DAT.RES_OUTPUT.Add(new RES_OUTPUT()
						{
							RES_FIELD_ID = property.Name,
							RES_DAT = property.Value.ToString(Formatting.None)
						});
					}

					break;
				case "SQLText":
					JObject sqlJson = JObject.Parse(applicationResponse.ResultJson);
					RES_OUTPUT sqlData = new RES_OUTPUT();
					sqlData.RES_FIELD_ID = "SQLText";
					sqlData.RES_DAT = sqlJson;
					response.DAT.RES_OUTPUT.Add(sqlData);

					break;
				case "Json":
					List<FieldData> outputs = JsonConvert.DeserializeObject<List<FieldData>>(applicationResponse.ResultJson);

					if (outputContracts.Count > 0)
					{
						if (outputContracts.Where(p => p.Type == "Dynamic").Count() > 0)
						{
							for (int i = 0; i < outputs.Count; i++)
							{
								FieldData output = outputs[i];
								dynamic outputJson = JToken.Parse(output.Value.ToString());
								RES_OUTPUT responseData = new RES_OUTPUT();
								responseData.RES_FIELD_ID = output.FieldID;
								responseData.RES_DAT = outputJson;
								response.DAT.RES_OUTPUT.Add(responseData);
							}
						}
						else
						{
							int additionCount = outputContracts.Where(p => p.Type == "Addition").Count();
							if ((outputContracts.Count - additionCount + (additionCount > 0 ? 1 : 0)) != outputs.Count)
							{
								applicationResponse.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 출력 모델 개수 확인 필요, 계약 건수 - '{outputContracts.Count}', 응답 건수 - '{outputs.Count}'";
								return applicationResponse;
							}

							var lastIndex = outputs.Count - 1;
							for (int i = 0; i < outputs.Count; i++)
							{
								FieldData output = outputs[i];
								ModelOutputContract outputContract = outputContracts[i];
								Model model = businessModels.GetBusinessModel(outputContract.ModelID);

								if (model == null && outputContract.ModelID != "Unknown" && outputContract.ModelID != "Dynamic")
								{
									applicationResponse.ExceptionText = $"'{transactionObject.TransactionID}|{request.TH.FUNC_CD}' 거래 입력에 '{outputContract.ModelID}' 출력 모델 ID가 계약에 있는지 확인";
									return applicationResponse;
								}

								dynamic outputJson = null;
								RES_OUTPUT responseData = new RES_OUTPUT();
								responseData.RES_FIELD_ID = output.FieldID;

								if (additionCount > 0 && i == lastIndex)
								{
									try
									{
										JArray messagesJson = JArray.Parse(output.Value.ToString());
										for (int j = 0; j < messagesJson.Count; j++)
										{
											ADI_MSG adiMessage = new ADI_MSG();
											adiMessage.ADI_MSG_CD = messagesJson[j]["MSG_CD"].ToString();
											adiMessage.ADI_MSG_TXT = messagesJson[j]["MSG_TXT"].ToString();
											response.MDO.ADI_MSG.Add(adiMessage);
										}
									}
									catch (Exception exception)
									{
										ADI_MSG adiMessage = new ADI_MSG();
										adiMessage.ADI_MSG_CD = "E001";
										adiMessage.ADI_MSG_TXT = exception.ToMessage();
										logger.Warning("[{LogCategory}] [{GlobalID}] " + adiMessage.ADI_MSG_TXT, "Transaction/ADI_MSG", request.SH.GLBL_ID);
										response.MDO.ADI_MSG.Add(adiMessage);
									}
									continue;
								}

								if (model == null)
								{
									if (outputContract.ModelID == "Unknown")
									{
										if (outputContract.Type == "Form")
										{
											outputJson = JObject.Parse(output.Value.ToString());
											JObject jObject = (JObject)outputJson;
											foreach (JProperty property in jObject.Properties())
											{
												if (outputContract.Fields.Contains(property.Name) == false)
												{
													applicationResponse.ExceptionText = $"{outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
													return applicationResponse;
												}
											}
										}
										else if (outputContract.Type == "Grid" || outputContract.Type == "DataSet")
										{
											outputJson = JArray.Parse(output.Value.ToString());
											if (outputJson.Count > 0)
											{
												JObject jObject = (JObject)outputJson.First;
												foreach (JProperty property in jObject.Properties())
												{
													if (outputContract.Fields.Contains(property.Name) == false)
													{
														applicationResponse.ExceptionText = $"{outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
														return applicationResponse;
													}
												}
											}
										}
										else if (outputContract.Type == "Chart")
										{
											outputJson = JToken.Parse(output.Value.ToString());
										}
										else if (outputContract.Type == "Dynamic")
										{
											outputJson = JToken.Parse(output.Value.ToString());
										}
									}
									else if (outputContract.ModelID == "Dynamic")
									{
										if (outputContract.Type == "Form")
										{
											outputJson = JObject.Parse(output.Value.ToString());
										}
										else if (outputContract.Type == "Grid")
										{
											outputJson = JArray.Parse(output.Value.ToString());
										}
										else if (outputContract.Type == "Chart")
										{
											outputJson = JToken.Parse(output.Value.ToString());
										}
										else if (outputContract.Type == "DataSet")
										{
											outputJson = JToken.Parse(output.Value.ToString());
										}
										else if (outputContract.Type == "Dynamic")
										{
											outputJson = JToken.Parse(output.Value.ToString());
										}
									}
								}
								else
								{
									if (outputContract.Type == "Form")
									{
										outputJson = JObject.Parse(output.Value.ToString());
										JObject jObject = (JObject)outputJson;
										foreach (JProperty property in jObject.Properties())
										{
											if (model.Columns.IsContain(property.Name) == false)
											{
												applicationResponse.ExceptionText = $"'{model.Name}' {outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
												return applicationResponse;
											}
										}
									}
									else if (outputContract.Type == "Grid" || outputContract.Type == "DataSet")
									{
										outputJson = JArray.Parse(output.Value.ToString());
										if (outputJson.Count > 0)
										{
											JObject jObject = (JObject)outputJson.First;
											foreach (JProperty property in jObject.Properties())
											{
												if (model.Columns.IsContain(property.Name) == false)
												{
													applicationResponse.ExceptionText = $"'{model.Name}' {outputContract.Type} 출력 모델에 '{property.Name}' 항목 확인 필요";
													return applicationResponse;
												}
											}
										}
									}
									else if (outputContract.Type == "Chart")
									{
										outputJson = JToken.Parse(output.Value.ToString());
									}
									else if (outputContract.Type == "Dynamic")
									{
										outputJson = JToken.Parse(output.Value.ToString());
									}
								}

								responseData.RES_DAT = outputJson;
								response.DAT.RES_OUTPUT.Add(responseData);
							}
						}
					}
					break;
			}

			return applicationResponse;
		}

		private static void SetInputDefaultValue(DefaultValue defaultValue, DbColumn column, REQ_INPUT tempReqInput)
		{
			switch (column.DataType)
			{
				case "String":
					tempReqInput.REQ_FIELD_DAT = defaultValue.String;
					break;
				case "Int32":
					tempReqInput.REQ_FIELD_DAT = defaultValue.Integer;
					break;
				case "Boolean":
					tempReqInput.REQ_FIELD_DAT = defaultValue.Boolean;
					break;
				case "DateTime":
					DateTime dateValue;
					if (DateTime.TryParseExact(defaultValue.String, "o", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue) == true)
					{
						tempReqInput.REQ_FIELD_DAT = dateValue;
					}
					else
					{
						tempReqInput.REQ_FIELD_DAT = DateTime.Now;
					}
					break;
				default:
					tempReqInput.REQ_FIELD_DAT = "";
					break;
			}
		}

		private void MappingTransactionInputsValue(TransactionObject transactionObject, int modelInputIndex, ModelInputContract modelInputContract, JObject formOutput)
		{
			List<List<TransactField>> transactInputs = transactionObject.Inputs;
			int inputCount = 0;
			int inputOffset = 0;
			for (int i = 0; i < transactionObject.InputsItemCount.Count; i++)
			{
				inputCount = transactionObject.InputsItemCount[i];

				if (i <= modelInputIndex)
				{
					break;
				}

				inputOffset = inputOffset + inputCount;
			}

			List<List<TransactField>> inputs = transactInputs.Skip(inputOffset).Take(inputCount).ToList();

			if (modelInputContract.Type == "Row")
			{
				if (inputs.Count > 0)
				{
					List<TransactField> serviceParameters = inputs[0];

					foreach (var item in formOutput)
					{
						TransactField fieldItem = serviceParameters.Where(p => p.FieldID == item.Key).FirstOrDefault();
						if (fieldItem != null)
						{
							fieldItem.Value = ((JValue)item.Value).Value;
						}
					}
				}
			}
			else if (modelInputContract.Type == "List")
			{
				if (inputs.Count > 0)
				{
					List<TransactField> findParameters = inputs[0];

					foreach (var item in formOutput)
					{
						TransactField findItem = findParameters.Where(p => p.FieldID == item.Key).FirstOrDefault();
						if (findItem != null)
						{
							for (int i = 0; i < inputs.Count; i++)
							{
								List<TransactField> serviceParameters = inputs[i];

								TransactField fieldItem = serviceParameters.Where(p => p.FieldID == item.Key).FirstOrDefault();
								if (fieldItem != null)
								{
									fieldItem.Value = ((JValue)item.Value).Value;
								}
							}
						}
					}
				}
			}
		}

		private ApplicationResponse SequentialRequestDataTransaction(TransactionRequest request, TransactionObject transactionObject, SequentialOption sequentialOption, List<ModelInputContract> inputContracts, List<ModelOutputContract> outputContracts)
		{
			ApplicationResponse responseObject = new ApplicationResponse();
			responseObject.Acknowledge = AcknowledgeType.Failure;

			try
			{
				string businessID = string.IsNullOrEmpty(sequentialOption.TransactionProjectID) == true ? request.TH.BIZ_ID : sequentialOption.TransactionProjectID;
				string transactionID = string.IsNullOrEmpty(sequentialOption.TransactionID) == true ? request.TH.TRN_CD : sequentialOption.TransactionID;
				string serviceID = string.IsNullOrEmpty(sequentialOption.ServiceID) == true ? transactionObject.ServiceID : sequentialOption.ServiceID;

				string messageServerUrl = StaticConfig.RouteUrl.GetValueOrDefault(request.TH.PGM_ID + request.TH.BIZ_ID + request.SH.ENV_INF_DSCD);

				if (string.IsNullOrEmpty(messageServerUrl) == true)
				{
					responseObject.ExceptionText = request.SH.ENV_INF_DSCD + "_MessageServerUrl 환경변수 확인";
					return responseObject;
				}

				DynamicRequest dynamicRequest = new DynamicRequest();
				dynamicRequest.AccessTokenID = request.AccessTokenID;
				dynamicRequest.Action = request.Action;
				dynamicRequest.ClientTag = request.ClientTag;
				dynamicRequest.Environment = request.Environment;
				dynamicRequest.RequestID = request.RequestID;
				dynamicRequest.GlobalID = request.SH.GLBL_ID;
				dynamicRequest.Version = request.Version;
				dynamicRequest.LoadOptions = transactionObject.LoadOptions;
				dynamicRequest.IsTransaction = transactionObject.TransactionScope;
				dynamicRequest.ReturnType = (ExecuteDynamicTypeObject)Enum.Parse(typeof(ExecuteDynamicTypeObject), transactionObject.ReturnType);
				List<Qrame.Core.Library.MessageContract.DataObject.DynamicObject> dynamicObjects = new List<Qrame.Core.Library.MessageContract.DataObject.DynamicObject>();

				List<List<TransactField>> transactInputs = transactionObject.Inputs;

				int inputOffset = 0;
				Dictionary<string, List<List<TransactField>>> requestInputItems = new Dictionary<string, List<List<TransactField>>>();
				for (int i = 0; i < transactionObject.InputsItemCount.Count; i++)
				{
					int inputCount = transactionObject.InputsItemCount[i];
					if (inputCount > 0 && inputContracts.Count > 0)
					{
						ModelInputContract inputContract = inputContracts[i];
						List<List<TransactField>> inputs = transactInputs.Skip(inputOffset).Take(inputCount).ToList();

						for (int j = 0; j < inputs.Count; j++)
						{
							List<TransactField> serviceParameters = inputs[j];

							Qrame.Core.Library.MessageContract.DataObject.DynamicObject dynamicObject = new Qrame.Core.Library.MessageContract.DataObject.DynamicObject();

							dynamicObject.QueryID = string.Concat(transactionObject.TransactionID, "|", transactionObject.ServiceID, i.ToString().PadLeft(2, '0'));
							if (StaticConfig.IsQueryIDHashing == true)
							{
								dynamicObject.QueryID = ShaCrypto.GetHashData(dynamicObject.QueryID, ShaEncryption.MD5);
							}

							List<JsonObjectType> jsonObjectTypes = new List<JsonObjectType>();
							foreach (ModelOutputContract item in outputContracts)
							{
								JsonObjectType jsonObjectType = (JsonObjectType)Enum.Parse(typeof(JsonObjectType), item.Type + "Json");
								jsonObjectTypes.Add(jsonObjectType);

								if (jsonObjectType == JsonObjectType.AdditionJson)
								{
									dynamicObject.JsonObject = jsonObjectType;
								}
							}
							dynamicObject.JsonObjects = jsonObjectTypes;

							List<DynamicParameter> parameters = new List<DynamicParameter>();
							foreach (var item in serviceParameters)
							{
								parameters.Append(item.FieldID, (DbType)Enum.Parse(typeof(DbType), item.DataType), item.Value);
							}

							dynamicObject.Parameters = parameters;
							dynamicObject.BaseFieldMappings = inputContract.BaseFieldMappings;
							dynamicObject.IgnoreResult = inputContract.IgnoreResult;
							dynamicObjects.Add(dynamicObject);
						}
					}
					else
					{
						Qrame.Core.Library.MessageContract.DataObject.DynamicObject dynamicObject = new Qrame.Core.Library.MessageContract.DataObject.DynamicObject();
						dynamicObject.QueryID = string.Concat(transactionObject.TransactionID, "|", transactionObject.ServiceID, i.ToString().PadLeft(2, '0'));
						if (StaticConfig.IsQueryIDHashing == true)
						{
							dynamicObject.QueryID = ShaCrypto.GetHashData(dynamicObject.QueryID, ShaEncryption.MD5);
						}

						List<JsonObjectType> jsonObjectTypes = new List<JsonObjectType>();
						foreach (ModelOutputContract item in outputContracts)
						{
							JsonObjectType jsonObjectType = (JsonObjectType)Enum.Parse(typeof(JsonObjectType), item.Type + "Json");
							jsonObjectTypes.Add(jsonObjectType);

							if (jsonObjectType == JsonObjectType.AdditionJson)
							{
								dynamicObject.JsonObject = jsonObjectType;
							}
						}
						dynamicObject.JsonObjects = jsonObjectTypes;
						dynamicObject.Parameters = new List<DynamicParameter>();
						dynamicObject.BaseFieldMappings = new List<BaseFieldMapping>();
						dynamicObject.IgnoreResult = false;
						dynamicObjects.Add(dynamicObject);
					}

					inputOffset = inputOffset + inputCount;
				}

				dynamicRequest.DynamicObjects = dynamicObjects;
				dynamicRequest.ClientTag = transactionObject.ClientTag;

				string segment = "";
				switch (sequentialOption.TransactionType)
				{
					case "C":
						segment = "/console";
						break;
					case "T":
						segment = "/task";
						break;
					case "D":
						segment = "/dynamic";
						break;
					case "A":
						BusinessContract businessContract = TransactionMapper.Get(request.TH.PGM_ID, businessID, transactionID);
						segment = $"/webapi/{request.TH.PGM_ID}/{businessContract.TransactionProjectID}/{transactionID}";
						break;
					case "F":
						segment = "/function";
						break;
					case "R":
						segment = "/repository";
						break;
				}
				var restClient = new RestClient(messageServerUrl + segment);
				var restRequest = new RestRequest(Method.POST);

				if (StaticConfig.MessageDataType == "json")
				{
					restRequest.AddHeader("Content-Type", "qrame/json-message");
					string json = JsonConvert.SerializeObject(dynamicRequest);
					restRequest.AddParameter("qrame/json-message", json, ParameterType.RequestBody);
				}
				else
				{
					restRequest.AddHeader("Content-Type", "qrame/stream-message");
					byte[] data = MessagePackSerializer.Serialize(dynamicRequest);
					restRequest.AddParameter("qrame/stream-message", data, ParameterType.RequestBody);
				}

				DynamicResponse response = null;
				IRestResponse restResponse = null;
				byte[] fileResponse = null;
				if (dynamicRequest.ReturnType == ExecuteDynamicTypeObject.DataSet)
				{
					fileResponse = restClient.DownloadData(restRequest);
					responseObject.ResultObject = fileResponse;
					responseObject.Acknowledge = AcknowledgeType.Success;
				}
				else
				{
					restResponse = restClient.Execute(restRequest);

					if (restResponse.StatusCode == HttpStatusCode.OK)
					{
						if (dynamicRequest.ReturnType == ExecuteDynamicTypeObject.Xml)
						{
							response = new DynamicResponse();
							response.ResultObject = restResponse.Content;
						}
						else
						{
							response = JsonConvert.DeserializeObject<DynamicResponse>(restResponse.Content);
						}

						responseObject.Acknowledge = response.Acknowledge;

						if (responseObject.Acknowledge == AcknowledgeType.Success)
						{
							switch (dynamicRequest.ReturnType)
							{
								case ExecuteDynamicTypeObject.DataSet:
									responseObject.ResultObject = response.ResultObject as byte[];
									break;
								case ExecuteDynamicTypeObject.Json:
									responseObject.ResultMeta = response.ResultMeta;
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.Scalar:
									responseObject.ResultObject = response.ResultObject;
									break;
								case ExecuteDynamicTypeObject.NonQuery:
									responseObject.ResultInteger = response.ResultInteger;
									break;
								case ExecuteDynamicTypeObject.SQLText:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.SchemeOnly:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.CodeHelp:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.Xml:
									responseObject.ResultObject = response.ResultObject as string;
									break;
								case ExecuteDynamicTypeObject.DynamicJson:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
							}
						}
						else
						{
							responseObject.ExceptionText = response.ExceptionText;
						}
					}
					else
					{
						responseObject.ExceptionText = "AP X-Requested Transfort Error: " + Environment.NewLine + restResponse.ErrorMessage;
					}
				}
			}
			catch (Exception exception)
			{
				responseObject.ExceptionText = exception.ToMessage();
				logger.Error("[{LogCategory}] [{GlobalID}] " + responseObject.ExceptionText, "Transaction/SequentialRequestDataTransaction", request.SH.GLBL_ID);
			}

			return responseObject;
		}

		private ApplicationResponse RequestDataTransaction(TransactionRequest request, TransactionObject transactionObject, List<ModelInputContract> inputContracts, List<ModelOutputContract> outputContracts)
		{
			ApplicationResponse responseObject = new ApplicationResponse();
			responseObject.Acknowledge = AcknowledgeType.Failure;

			try
			{
				string messageServerUrl = StaticConfig.RouteUrl.GetValueOrDefault(request.TH.PGM_ID + request.TH.BIZ_ID + request.SH.ENV_INF_DSCD);

				if (string.IsNullOrEmpty(messageServerUrl) == true)
				{
					responseObject.ExceptionText = request.SH.ENV_INF_DSCD + "_MessageServerUrl 환경변수 확인";
					return responseObject;
				}

				DynamicRequest dynamicRequest = new DynamicRequest();
				dynamicRequest.AccessTokenID = request.AccessTokenID;
				dynamicRequest.Action = request.Action;
				dynamicRequest.ClientTag = request.ClientTag;
				dynamicRequest.Environment = request.Environment;
				dynamicRequest.RequestID = request.RequestID;
				dynamicRequest.GlobalID = request.SH.GLBL_ID;
				dynamicRequest.Version = request.Version;
				dynamicRequest.LoadOptions = transactionObject.LoadOptions;
				dynamicRequest.IsTransaction = transactionObject.TransactionScope;
				dynamicRequest.ReturnType = (ExecuteDynamicTypeObject)Enum.Parse(typeof(ExecuteDynamicTypeObject), transactionObject.ReturnType);
				List<Qrame.Core.Library.MessageContract.DataObject.DynamicObject> dynamicObjects = new List<Qrame.Core.Library.MessageContract.DataObject.DynamicObject>();

				List<List<TransactField>> transactInputs = transactionObject.Inputs;

				int inputOffset = 0;
				Dictionary<string, List<List<TransactField>>> requestInputItems = new Dictionary<string, List<List<TransactField>>>();
				for (int i = 0; i < transactionObject.InputsItemCount.Count; i++)
				{
					int inputCount = transactionObject.InputsItemCount[i];
					if (inputCount > 0 && inputContracts.Count > 0)
					{
						ModelInputContract inputContract = inputContracts[i];
						List<List<TransactField>> inputs = transactInputs.Skip(inputOffset).Take(inputCount).ToList();

						for (int j = 0; j < inputs.Count; j++)
						{
							List<TransactField> serviceParameters = inputs[j];

							Qrame.Core.Library.MessageContract.DataObject.DynamicObject dynamicObject = new Qrame.Core.Library.MessageContract.DataObject.DynamicObject();

							dynamicObject.QueryID = string.Concat(transactionObject.TransactionID, "|", transactionObject.ServiceID, i.ToString().PadLeft(2, '0'));
							if (StaticConfig.IsQueryIDHashing == true)
							{
								dynamicObject.QueryID = ShaCrypto.GetHashData(dynamicObject.QueryID, ShaEncryption.MD5);
							}

							List<JsonObjectType> jsonObjectTypes = new List<JsonObjectType>();
							foreach (ModelOutputContract item in outputContracts)
							{
								JsonObjectType jsonObjectType = (JsonObjectType)Enum.Parse(typeof(JsonObjectType), item.Type + "Json");
								jsonObjectTypes.Add(jsonObjectType);

								if (jsonObjectType == JsonObjectType.AdditionJson)
								{
									dynamicObject.JsonObject = jsonObjectType;
								}
							}
							dynamicObject.JsonObjects = jsonObjectTypes;

							List<DynamicParameter> parameters = new List<DynamicParameter>();
							foreach (var item in serviceParameters)
							{
								parameters.Append(item.FieldID, (DbType)Enum.Parse(typeof(DbType), item.DataType), item.Value);
							}

							dynamicObject.Parameters = parameters;
							dynamicObject.BaseFieldMappings = inputContract.BaseFieldMappings;
							dynamicObject.IgnoreResult = inputContract.IgnoreResult;
							dynamicObjects.Add(dynamicObject);
						}
					}
					else
					{
						Qrame.Core.Library.MessageContract.DataObject.DynamicObject dynamicObject = new Qrame.Core.Library.MessageContract.DataObject.DynamicObject();
						dynamicObject.QueryID = string.Concat(transactionObject.TransactionID, "|", transactionObject.ServiceID, i.ToString().PadLeft(2, '0'));
						if (StaticConfig.IsQueryIDHashing == true)
						{
							dynamicObject.QueryID = ShaCrypto.GetHashData(dynamicObject.QueryID, ShaEncryption.MD5);
						}

						List<JsonObjectType> jsonObjectTypes = new List<JsonObjectType>();
						foreach (ModelOutputContract item in outputContracts)
						{
							JsonObjectType jsonObjectType = (JsonObjectType)Enum.Parse(typeof(JsonObjectType), item.Type + "Json");
							jsonObjectTypes.Add(jsonObjectType);

							if (jsonObjectType == JsonObjectType.AdditionJson)
							{
								dynamicObject.JsonObject = jsonObjectType;
							}
						}
						dynamicObject.JsonObjects = jsonObjectTypes;
						dynamicObject.Parameters = new List<DynamicParameter>();
						dynamicObject.BaseFieldMappings = new List<BaseFieldMapping>();
						dynamicObject.IgnoreResult = false;
						dynamicObjects.Add(dynamicObject);
					}

					inputOffset = inputOffset + inputCount;
				}

				dynamicRequest.DynamicObjects = dynamicObjects;
				dynamicRequest.ClientTag = transactionObject.ClientTag;

				string segment = "";
				switch (request.TH.CMD_TYPE)
				{
					case "C":
						segment = "/console";
						break;
					case "T":
						segment = "/task";
						break;
					case "D":
						segment = "/dynamic";
						break;
					case "A":
						BusinessContract businessContract = TransactionMapper.Get(request.TH.PGM_ID, request.TH.BIZ_ID, request.TH.TRN_CD);
						segment = $"/webapi/{request.TH.PGM_ID}/{businessContract.TransactionProjectID}/{request.TH.TRN_CD}";
						break;
					case "F":
						segment = "/function";
						break;
					case "R":
						segment = "/repository";
						break;
				}
				var restClient = new RestClient(messageServerUrl + segment);
				// restClient.Proxy = BypassWebProxy.Default;
				var restRequest = new RestRequest(Method.POST);

				if (StaticConfig.MessageDataType == "json")
				{
					restRequest.AddHeader("Content-Type", "qrame/json-message");
					string json = JsonConvert.SerializeObject(dynamicRequest);
					restRequest.AddParameter("qrame/json-message", json, ParameterType.RequestBody);
				}
				else
				{
					restRequest.AddHeader("Content-Type", "qrame/stream-message");
					byte[] data = MessagePackSerializer.Serialize(dynamicRequest);
					restRequest.AddParameter("qrame/stream-message", data, ParameterType.RequestBody);
				}

				DynamicResponse response = null;
				IRestResponse restResponse = null;
				byte[] fileResponse = null;
				if (dynamicRequest.ReturnType == ExecuteDynamicTypeObject.DataSet)
				{
					fileResponse = restClient.DownloadData(restRequest);
					responseObject.ResultObject = fileResponse;
					responseObject.Acknowledge = AcknowledgeType.Success;
				}
				else
				{
					restResponse = restClient.Execute(restRequest);

					if (restResponse.StatusCode == HttpStatusCode.OK)
					{
						if (dynamicRequest.ReturnType == ExecuteDynamicTypeObject.Xml)
						{
							response = new DynamicResponse();
							response.ResultObject = restResponse.Content;
						}
						else
						{
							response = JsonConvert.DeserializeObject<DynamicResponse>(restResponse.Content);
						}

						responseObject.Acknowledge = response.Acknowledge;

						if (responseObject.Acknowledge == AcknowledgeType.Success)
						{
							switch (dynamicRequest.ReturnType)
							{
								case ExecuteDynamicTypeObject.DataSet:
									responseObject.ResultObject = response.ResultObject as byte[];
									break;
								case ExecuteDynamicTypeObject.Json:
									responseObject.ResultMeta = response.ResultMeta;
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.Scalar:
									responseObject.ResultObject = response.ResultObject;
									break;
								case ExecuteDynamicTypeObject.NonQuery:
									responseObject.ResultInteger = response.ResultInteger;
									break;
								case ExecuteDynamicTypeObject.SQLText:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.SchemeOnly:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.CodeHelp:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
								case ExecuteDynamicTypeObject.Xml:
									responseObject.ResultObject = response.ResultObject as string;
									break;
								case ExecuteDynamicTypeObject.DynamicJson:
									responseObject.ResultJson = response.ResultJson.ToString();
									break;
							}
						}
						else
						{
							if (string.IsNullOrEmpty(response.ExceptionText) == true)
							{
								responseObject.ExceptionText = $"GlobalID: {dynamicRequest.GlobalID} 거래 확인 필요";
							}
							else
							{
								responseObject.ExceptionText = response.ExceptionText;
							}
						}
					}
					else
					{
						responseObject.ExceptionText = "AP X-Requested Transfort Error: " + Environment.NewLine + restResponse.ErrorMessage;
					}
				}
			}
			catch (Exception exception)
			{
				responseObject.ExceptionText = exception.ToMessage();
				logger.Error("[{LogCategory}] [{GlobalID}] " + responseObject.ExceptionText, "Transaction/RequestDataTransaction", request.SH.GLBL_ID);
			}

			return responseObject;
		}

		private JArray ToJson(string val)
		{
			JArray result = new JArray();

			char delimeter = '｜';
			char newline = '↵';
			var lines = val.Split(newline);
			var headers = lines[0].Split(delimeter);

			for (int i = 0; i < headers.Length; i++)
			{
				headers[i] = headers[i].Replace(@"(^[\s""]+|[\s""]+$)", "");
			}

			int lineLength = lines.Length;
			for (int i = 1; i < lineLength; i++)
			{
				var row = lines[i].Split(delimeter);
				JObject item = new JObject();
				for (var j = 0; j < headers.Length; j++)
				{
					item[headers[j]] = ToDynamic(row[j]);
				}
				result.Add(item);
			}

			return result;
		}

		private dynamic ToDynamic(string val)
		{
			dynamic result;

			if (val == "true" || val == "True" || val == "TRUE")
			{
				result = true;
			}
			else if (val == "false" || val == "False" || val == "FALSE")
			{
				result = false;
			}
			else if (Regex.IsMatch(val, @"^\s*-?(\d*\.?\d+|\d+\.?\d*)(e[-+]?\d+)?\s*$") == true)
			{
				int intValue = 0;
				bool isParsable = int.TryParse(val, out intValue);
				if (isParsable == true)
				{
					result = intValue;
				}
				else
				{
					float floatValue = 0;
					isParsable = float.TryParse(val, out floatValue);
					if (isParsable == true)
					{
						result = intValue;
					}
					else
					{
						result = 0;
					}
				}
			}
			else if (Regex.IsMatch(val, @"(\d{4}-[01]\d-[0-3]\dT[0-2]\d:[0-5]\d:[0-5]\d\.\d+([+-][0-2]\d:[0-5]\d|Z))|(\d{4}-[01]\d-[0-3]\dT[0-2]\d:[0-5]\d:[0-5]\d([+-][0-2]\d:[0-5]\d|Z))|(\d{4}-[01]\d-[0-3]\dT[0-2]\d:[0-5]\d([+-][0-2]\d:[0-5]\d|Z))") == true)
			{
				result = DateTime.Parse(val);
			}
			else
			{
				result = val;
			}

			return result;
		}

		private void CopyHeader(TransactionRequest request, TransactionResponse response)
		{
			response.SH.TLM_ENCY_DSCD = request.SH.TLM_ENCY_DSCD;
			response.SH.GLBL_ID = request.SH.GLBL_ID;
			response.SH.GLBL_ID_PRG_SRNO = request.SH.GLBL_ID_PRG_SRNO;
			response.SH.CLNT_IPAD = request.SH.CLNT_IPAD;
			response.SH.CLNT_MAC = request.SH.CLNT_MAC;
			response.SH.ENV_INF_DSCD = request.SH.ENV_INF_DSCD;
			response.SH.FST_TMS_SYS_CD = request.SH.FST_TMS_SYS_CD;
			response.SH.FST_TLM_REQ_DTM = request.SH.FST_TLM_REQ_DTM;
			response.SH.LANG_DSCD = request.SH.LANG_DSCD;
			response.SH.TMS_SYS_CD = request.SH.TMS_SYS_CD;
			response.SH.TMS_SYS_NODE_ID = request.SH.TMS_SYS_NODE_ID;
			response.SH.MD_KDCD = request.SH.MD_KDCD;
			response.SH.INTF_ID = request.SH.INTF_ID;
			response.SH.TLM_REQ_DTM = request.SH.TLM_REQ_DTM;
			response.SH.RSP_SYS_CD = request.SH.RSP_SYS_CD;
			response.SH.TLM_RSP_DTM = request.SH.TLM_RSP_DTM;
			response.SH.RSP_RST_DSCD = request.SH.RSP_RST_DSCD;
			response.SH.MSG_OCC_SYS_CD = request.SH.MSG_OCC_SYS_CD;

			response.TH.TRM_BRNO = request.TH.TRM_BRNO;
			response.TH.OPR_NO = request.TH.OPR_NO;
			response.TH.RLPE_SQCN = request.TH.RLPE_SQCN;
			response.TH.TRN_SCRN_CD = request.TH.TRN_SCRN_CD;
			response.TH.PGM_ID = request.TH.PGM_ID;
			response.TH.BIZ_ID = request.TH.BIZ_ID;
			response.TH.TRN_CD = request.TH.TRN_CD;
			response.TH.FUNC_CD = request.TH.FUNC_CD;
			response.TH.DAT_FMT = request.TH.DAT_FMT;
			response.TH.LQTY_DAT_PRC_DIS = request.TH.LQTY_DAT_PRC_DIS;
			response.TH.SMLT_TRN_DSCD = request.TH.SMLT_TRN_DSCD;
			response.TH.EXNK_DSCD = request.TH.EXNK_DSCD;
			response.TH.MSK_NTGT_TRN_YN = request.TH.MSK_NTGT_TRN_YN;

			response.CorrelationID = request.RequestID;
		}
	}
}