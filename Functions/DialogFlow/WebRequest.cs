using Google.Cloud.Dialogflow.Cx.V3;
using Google.Cloud.Functions.Framework;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;


namespace DialogFlow;

public class Function : IHttpFunction {
    private readonly ILogger _log;

    public Function(ILogger<Function> log) =>
            _log = log;

    public async Task HandleAsync(HttpContext context){
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
        JsonParser          jParser        = new (parserSettings);
        WebhookRequest      webhookRequest = jParser.Parse <WebhookRequest>(text);
        SessionInfo         sessionInfo    = webhookRequest.SessionInfo;
        string              tag            = webhookRequest.FulfillmentInfo.Tag;
        sessionInfo.Parameters.TryGetValue(key: "name", value: out var nameParameterValue);

        _log.LogInformation($"webhookrequest : {webhookRequest}");

        if(tag == "comm100-customfields" && !string.IsNullOrEmpty(nameParameterValue?.ToString()))
        {
            try
            {
                RestClientOptions options = new("https://api12.comm100.io")
                    {
                        MaxTimeout = -1
                    };

                RestClient client = new RestClient(options);
                RestRequest restRequest = new RestRequest("/v4/livechat/visitors?siteId=75000104&include=contactIdentity&include=chatAgent&include=department");
                restRequest.AddHeader(name: "Authorization", value: "Basic [redacted]");
                RestResponse comm100JsonResponse = await client.ExecuteAsync(request: restRequest);
                List<JsonElement> visitorList = JsonSerializer.Deserialize<List<JsonElement>>(comm100JsonResponse.Content ?? string.Empty);
                List<JsonElement> filteredList = visitorList.FindAll( visitor => visitor.GetProperty("name").GetString() == nameParameterValue.StringValue);

                if(filteredList.Count == 1)
                {
                    response.SessionInfo = new SessionInfo();
                    List<JsonElement> customFields = filteredList[index: 0].GetProperty("customFields").Deserialize<List<JsonElement>>();

                    foreach(JsonElement fieldObject in customFields)
                    {
                        string key = fieldObject.GetProperty("name").GetString()?.Trim().ToLower().Replace(oldValue: " ", newValue: "-");
                        Value value = Value.ForString(fieldObject.GetProperty("value").GetString());

                        if(!string.IsNullOrEmpty(key))
                        {
                            response.SessionInfo.Parameters.Add(key: key, value: value);
                        }
                    }
                }
            }
            catch(Exception parseException)
            {
                Console.Write(value: parseException);
                _log.LogError($"error : {parseException}");
            }
        }

        // var formatter = new JsonFormatter(new JsonFormatter.Settings(formatDefaultValues: true));
        JsonFormatter formatter = new JsonFormatter(new JsonFormatter.Settings(true));


        httpResponse.ContentType = "application/json; charset=UTF-8";
        httpResponse.Headers.Append(key: "Access-Control-Allow-Methods", value: "GET");
        httpResponse.Headers.Append(key: "Access-Control-Allow-Headers", value: "Content-Type");

        // await httpResponse.WriteAsync(response.ToString());
        await httpResponse.WriteAsync(formatter.Format(response));
        await httpResponse.Body.FlushAsync();
    }
}