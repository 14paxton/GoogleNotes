using Google.Cloud.Dialogflow.Cx.V3;
using Google.Cloud.Functions.Framework;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Comm100API
{
	public class Function : IHttpFunction
	{
		readonly ILogger _log;

		public Function(ILogger<Function> log)
		{
			_log = log;
		}

		public async Task HandleAsync(HttpContext context)
		{
			HttpRequest                                                      httpRequest      = context.Request;
			HttpResponse                                                     httpResponse     = context.Response;
			using TextReader                                                 reader           = new StreamReader(httpRequest.Body);
			string                                                           text             = await reader.ReadToEndAsync();
			JsonParser.Settings                                              parserSettings   = JsonParser.Settings.Default.WithIgnoreUnknownFields(true);
			JsonParser                                                       jParser          = new JsonParser(parserSettings);
			WebhookRequest                                                   webhookRequest   = jParser.Parse<WebhookRequest>(text);
			string                                                           response         = null;
			string                                                           tag              = webhookRequest.FulfillmentInfo.Tag;
			IDictionary<string, Func<WebhookRequest, ILogger, Task<string>>> webhookFunctions = await BuildFunctionMap();

			_log.LogInformation($"http request body- WebhookRequest : {text}");
			_log.LogInformation($"http request tag : {tag}");


			try
			{
				if (webhookFunctions != null)
				{
					response = webhookFunctions.TryGetValue(tag, out Func<WebhookRequest, ILogger, Task<string>> function) ? await function(webhookRequest, _log) : "Bad Tag";
				}

			}
			catch (Exception parseException)
			{
				Console.Write(parseException);
				_log.LogError($@"error : {parseException}");

				throw;
			}

			httpResponse.ContentType = "application/json; charset=UTF-8";
			httpResponse.Headers.Append("Access-Control-Allow-Methods", "GET");
			httpResponse.Headers.Append("Access-Control-Allow-Headers", "Content-Type");

			// await httpResponse.WriteAsync(response.ToString());
			await httpResponse.WriteAsync(response ?? string.Empty);
			await httpResponse.Body.FlushAsync();
		}

		async Task<Dictionary<string, Func<WebhookRequest, ILogger, Task<string>>>> BuildFunctionMap()
		{
			return await Task.FromResult(new Dictionary<string, Func<WebhookRequest, ILogger, Task<string>>>
			{
				{
					"GetSessionInfo", _getSessionInfo
				}
			});
		}

		readonly Func<WebhookRequest, ILogger, Task<string>> _getSessionInfo = async (webhookRequest, _log) => {

			                                                                       WebhookResponse response = new WebhookResponse
			                                                                       {
				                                                                       FulfillmentResponse = new WebhookResponse.Types.FulfillmentResponse()
			                                                                       };

			                                                                       JsonFormatter formatter   = new JsonFormatter(new JsonFormatter.Settings(true));
			                                                                       SessionInfo   sessionInfo = webhookRequest.SessionInfo;
			                                                                       string        sessionURI  = sessionInfo.Session;
			                                                                       SessionName   sessionData = sessionInfo.SessionAsSessionName;

			                                                                       _log.LogInformation($@"webhookrequest - session : {sessionInfo}");

			                                                                       Value sessionValue = Value.ForString(sessionData?.ToString());
			                                                                       Value uri          = Value.ForString(sessionURI);
			                                                                       Value idValue      = Value.ForString(sessionData?.SessionId);
			                                                                       response.SessionInfo = new SessionInfo();
			                                                                       response.SessionInfo.Parameters.Add("session-url", sessionValue);
			                                                                       response.SessionInfo.Parameters.Add("session-uri", uri);
			                                                                       response.SessionInfo.Parameters.Add("session-id", idValue);

			                                                                       return await Task.FromResult(formatter.Format(response));

		                                                                       };
	}
}
