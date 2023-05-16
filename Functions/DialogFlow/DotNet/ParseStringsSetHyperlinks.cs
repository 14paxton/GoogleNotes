using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Comm100API
{
	public class Function : IHttpFunction
	{

		readonly ILogger _logger;

		public Function(ILogger<Function> logger)
		{
			_logger = logger;
		}

		public async Task HandleAsync(HttpContext context)
		{
			HttpRequest request = context.Request;

			// If there's a body, parse it as JSON and check for "message" field.
			using TextReader reader = new StreamReader(request.Body);
			string           text   = await reader.ReadToEndAsync();
			InteractionResponse interactionResponse = new InteractionResponse
			{
				output = new InteractionResponse.Output
				{
					id = Guid.NewGuid().ToString(),
					content = new List<InteractionResponse.Content>()
				}
			};

			_logger.LogDebug($"debug: {text}");
			_logger.LogInformation($"info: {text}");
			_logger.LogTrace($"trace: {text}");

			// const string MYSTRING = "Here are some linked FAQs that might answer that:  \nhttps://dev.veridiancu.org/faq/8107/what-is-an-hsa-health-savings-account and here this \nhttps://dev.veridiancu.org/faq/8108/how-do-i-qualify-for-a-health-savings-account  and here this \nhttps://dev.veridiancu.org/faq/8106/can-i-open-a-cd-on-my-hsa";
			const string MYSTRING = "Here are some linked FAQs that might answer that:  \nhttps://dev.veridiancu.org/faq/8107/what-is-an-hsa-health-savings-account  \nhttps://dev.veridiancu.org/faq/8108/how-do-i-qualify-for-a-health-savings-account  \nhttps://dev.veridiancu.org/faq/8106/can-i-open-a-cd-on-my-hsa";

			//Regex for all https: urls some may have a new line or space before
			//use pattern to find urls and add special char sequence to split on
			List<string> messageStringList = await BuildListOfUrlSections(MYSTRING);
			//using index check if previous text string was a url, if not that needs to be the wording above the button
			foreach ((string msg, int index) in messageStringList.Select((msg, index) => (msg, index)))
			{
				if (!msg.Contains("https:")) continue;

				interactionResponse.output.content.Add(await BuildResponseWithLinks(msg, index, messageStringList));
			}

			string response = JsonConvert.SerializeObject(interactionResponse, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			_logger.LogInformation($@"comm100 chatBotSessionResponse: {response}");
			_logger.LogTrace($@"comm100 chatBotSessionResponse: {response}");

			await context.Response.WriteAsync(response);
		}

		async Task<InteractionResponse.Content> BuildResponseWithLinks(string msg, int index, List<string> messageStringList)
		{

			List<string> urlComponents  = new List<string>(msg.Split('/'));
			string       redirectTarget = urlComponents.Last();
			bool         containsDotCom = redirectTarget.Contains(".com");
			bool         containsDotOrg = redirectTarget.Contains(".org");
			bool         containsHyphen = redirectTarget.Contains('-');
			int          previousValue  = index - 1;
			string       textAboveBtn   = messageStringList[previousValue].Contains("https:") ? "\u0020" : messageStringList[previousValue];

			string btnText = !containsHyphen && (containsDotCom || containsDotOrg)
				? "Click Here"
				: redirectTarget.Replace('-', ' ');

			return new InteractionResponse.Content
			{
				type = "text",
				content = new InteractionResponse.Content
				{
					message = @$" {textAboveBtn} ",
					links = new List<InteractionResponse.Link>
					{
						new InteractionResponse.Link
						{
							buttonText = @$"{btnText}",
							url = msg,
							type = "webpage",
							openStyle = "full",
							openIn = "newWindow"
						}
					}
				}

			};

		}

		async Task<List<string>> BuildListOfUrlSections(string MYSTRING)
		{
			Regex urlPattern = new Regex(@"(?:(?:\\n|\n|\s\w*)https:\/\/.*?(?:\s|$))", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
			string result = urlPattern.Replace(MYSTRING, m => {
				                                             Match urlMatch = Regex.Match(m.Value, @"(https:\/\/)(.*?)(\s|$)");

				                                             return @$"~|~{urlMatch.Value}~|~";
			                                             });

			//split into list keying on the chars added
			List<string> messageStringList = new List<string>(result.Split(@"~|~", StringSplitOptions.RemoveEmptyEntries));

			return messageStringList;
		}
	}
}
