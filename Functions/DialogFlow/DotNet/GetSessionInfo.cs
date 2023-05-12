using Google.Cloud.Dialogflow.Cx.V3;
using Google.Cloud.Functions.Framework;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DialogFlow
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
            HttpRequest httpRequest = context.Request;
            HttpResponse httpResponse = context.Response;

            WebhookResponse response = new()
            {
                FulfillmentResponse = new WebhookResponse.Types.FulfillmentResponse()
            };

            using TextReader reader = new StreamReader(httpRequest.Body);
            string text = await reader.ReadToEndAsync();
            _log.LogInformation($"http request body- WebhookRequest : {text}");

            JsonParser.Settings parserSettings = JsonParser.Settings.Default.WithIgnoreUnknownFields(true);
            JsonParser jParser = new(parserSettings);
            WebhookRequest webhookRequest = jParser.Parse<WebhookRequest>(text);
            SessionInfo sessionInfo = webhookRequest.SessionInfo;
            SessionName sessionData = sessionInfo.SessionAsSessionName;

            _log.LogInformation($"webhookrequest : {webhookRequest}");
            _log.LogInformation($"webhookrequest : {sessionInfo}");

            try
            {
                Value sessionValue = Value.ForString(sessionData?.ToString());
                Value idValue = Value.ForString(sessionData?.SessionId);
                response.SessionInfo = new SessionInfo();
                response.SessionInfo.Session = sessionData.ToString();
                response.SessionInfo.Parameters.Add("session-url", sessionValue);
                response.SessionInfo.Parameters.Add("session-id", idValue);
            }
            catch (Exception parseException)
            {
                Console.Write(value: parseException);
                _log.LogError($"error : {parseException}");
            }

            // var formatter = new JsonFormatter(new JsonFormatter.Settings(formatDefaultValues: true));
            JsonFormatter formatter = new(new JsonFormatter.Settings(true));


            httpResponse.ContentType = "application/json; charset=UTF-8";
            httpResponse.Headers.Append("Access-Control-Allow-Methods", "GET");
            httpResponse.Headers.Append("Access-Control-Allow-Headers", "Content-Type");

            // await httpResponse.WriteAsync(response.ToString());
            await httpResponse.WriteAsync(formatter.Format(response));
            await httpResponse.Body.FlushAsync();
        }
    }
}