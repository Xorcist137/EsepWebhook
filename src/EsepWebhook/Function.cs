using System.IO;
using System;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private readonly HttpClient client = new HttpClient();

    public string FunctionHandler(object input, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Received input: {input}");

            // Parse the GitHub webhook payload
            dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());

            // Verify we have the expected data
            if (json.issue == null || json.issue.html_url == null)
            {
                context.Logger.LogError("Invalid GitHub webhook payload structure");
                return "Invalid payload structure";
            }

            // Construct the Slack message
            string payload = JsonConvert.SerializeObject(new
            {
                text = $"Issue Created: {json.issue.html_url}"
            });

            // Get the Slack webhook URL from environment variables
            string? slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogError("SLACK_URL environment variable not set");
                return "Slack URL not configured";
            }

            // Send to Slack
            var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());

            string result = reader.ReadToEnd();
            context.Logger.LogInformation($"Slack response: {result}");

            return result;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing webhook: {ex}");
            return $"Error: {ex.Message}";
        }
    }
}