using Google.Cloud.Dialogflow.Cx.V3;
using Google.Cloud.Functions.Framework;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Match = System.Text.RegularExpressions.Match;

namespace Comm100API
{
	public class Function : IHttpFunction
	{

		readonly Func<string, ILogger, Task<string>> _getHyperLinksFromText = async (text, _log) => {
			                                                                      InteractionResponse interactionResponse = new InteractionResponse
			                                                                      {
				                                                                      output = new InteractionResponse.Output
				                                                                      {
					                                                                      id = Guid.NewGuid().ToString(),
					                                                                      content = new List<InteractionResponse.Content>()
				                                                                      }
			                                                                      };

			                                                                      _log.LogDebug($"debug: {text}");
			                                                                      _log.LogInformation($"info: {text}");
			                                                                      _log.LogTrace($"trace: {text}");

			                                                                      // const string MYSTRING = "Here are some linked FAQs that might answer that:  \nhttps://dev.veridiancu.org/faq/8107/what-is-an-hsa-health-savings-account and here this \nhttps://dev.veridiancu.org/faq/8108/how-do-i-qualify-for-a-health-savings-account  and here this \nhttps://dev.veridiancu.org/faq/8106/can-i-open-a-cd-on-my-hsa";
			                                                                      const string MYSTRING = "Here are some linked FAQs that might answer that:  \nhttps://dev.veridiancu.org/faq/8107/what-is-an-hsa-health-savings-account  \nhttps://dev.veridiancu.org/faq/8108/how-do-i-qualify-for-a-health-savings-account  \nhttps://dev.veridiancu.org/faq/8106/can-i-open-a-cd-on-my-hsa";

			                                                                      //Regex for all https: urls some may have a new line or space before
			                                                                      //use pattern to find urls and add special char sequence to split on
			                                                                      List<string> messageStringList = await BuildListOfUrlSections(MYSTRING);

			                                                                      //using list if string has an http it is a url, if it is plan string it is dictating the url group
			                                                                      // every url under a line of text is added to that chat message until the next line of text
			                                                                      IList<InteractionResponse.Content> formattedLinksList = new List<InteractionResponse.Content>();
			                                                                      foreach ((string msg, int index) in messageStringList.Select((msg, index) => (msg, index)))
				                                                                      if (!string.IsNullOrWhiteSpace(msg))
				                                                                      {
					                                                                      if (!msg.Contains("https:"))
					                                                                      {


						                                                                      formattedLinksList.Add(new InteractionResponse.Content
						                                                                      {
							                                                                      type = "text",
							                                                                      content = new InteractionResponse.Content
							                                                                      {
								                                                                      message = $@" {msg} ",
								                                                                      links = new List<InteractionResponse.Link>()
							                                                                      }
						                                                                      });


						                                                                      continue;
					                                                                      }

					                                                                      formattedLinksList.Last().content.links.Add(await BuildResponseWithLinks(msg, index, messageStringList));
				                                                                      }

			                                                                      interactionResponse.output.content.AddRange(formattedLinksList);

			                                                                      _log.LogInformation($@"comm100 chatBotSessionResponse: {interactionResponse}");
			                                                                      _log.LogTrace($@"comm100 chatBotSessionResponse: {interactionResponse}");


			                                                                      return await Task.FromResult(JsonConvert.SerializeObject(interactionResponse, new JsonSerializerSettings
			                                                                      {
				                                                                      NullValueHandling = NullValueHandling.Ignore
			                                                                      }));
		                                                                      };

		readonly Func<string, ILogger, Task<string>> _getSessionInfo = async (text, _log) => {
			                                                               JsonParser.Settings parserSettings = JsonParser.Settings.Default.WithIgnoreUnknownFields(true);
			                                                               JsonParser          jParser        = new JsonParser(parserSettings);
			                                                               WebhookRequest      webhookRequest = jParser.Parse<WebhookRequest>(text);
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

		readonly ILogger _log;

		public Function(ILogger<Function> log)
		{
			_log = log;
		}

		public async Task HandleAsync(HttpContext context)
		{
			HttpRequest      httpRequest  = context.Request;
			HttpResponse     httpResponse = context.Response;
			using TextReader reader       = new StreamReader(httpRequest.Body);
			string           text         = await reader.ReadToEndAsync();
			JsonElement      json         = JsonSerializer.Deserialize<JsonElement>(text);
			string           response     = null;
			string tag = json.TryGetProperty("fulfillmentInfo", out JsonElement messageElement)
				? messageElement.TryGetProperty("tag", out JsonElement tagJsonElement)
					? tagJsonElement.GetString()
					: null
				: "ParseTextToHyperLinks";

			IDictionary<string, Func<string, ILogger, Task<string>>> webhookFunctions = await BuildFunctionMap();

			_log.LogInformation($"http request body- WebhookRequest : {text}");
			_log.LogInformation($"http request tag : {tag}");


			try
			{
				if (webhookFunctions != null)
					response = webhookFunctions.TryGetValue(tag ?? "ParseTextToHyperLinks", out Func<string, ILogger, Task<string>> function)
						? await function(text, _log)
						: "Bad Tag";

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

		async Task<Dictionary<string, Func<string, ILogger, Task<string>>>> BuildFunctionMap()
		{
			return await Task.FromResult(new Dictionary<string, Func<string, ILogger, Task<string>>>
			{
				{
					"GetSessionInfo", _getSessionInfo
				},
				{
					"ParseTextToHyperLinks", _getHyperLinksFromText
				}
			});
		}

		static async Task<InteractionResponse.Link> BuildResponseWithLinks(string msg, int index, IReadOnlyList<string> messageStringList)
		{
			List<string> urlComponents  = new List<string>(msg.Split('/'));
			string       redirectTarget = urlComponents.Last();
			bool         containsDotCom = redirectTarget.Contains(".com");
			bool         containsDotOrg = redirectTarget.Contains(".org");
			bool         containsHyphen = redirectTarget.Contains('-');

			string btnText = !containsHyphen && (containsDotCom || containsDotOrg)
				? "Click Here"
				: redirectTarget.Replace('-', ' ');

			return await Task.FromResult(new InteractionResponse.Link
			{
				buttonText = $@"{char.ToUpper(btnText[0])}{btnText[1..]}",
				url = msg,
				type = "webpage",
				openStyle = "full",
				openIn = "newWindow"
			});
		}

		static async Task<List<string>> BuildListOfUrlSections(string MYSTRING)
		{
			Regex urlPattern = new Regex(@"(?:(?:\\n|\n|\s\w*)https:\/\/.*?(?:\s|$))", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
			string result = urlPattern.Replace(MYSTRING, m => {
				                                             Match urlMatch = Regex.Match(m.Value, @"(https:\/\/)(.*?)(\s|$)");

				                                             return @$"~|~{urlMatch.Value}~|~";
			                                             });

			//split into list keying on the chars added
			List<string> messageStringList = new List<string>(result.Split(@"~|~", StringSplitOptions.RemoveEmptyEntries));

			return await Task.FromResult(messageStringList);
		}
	}
}