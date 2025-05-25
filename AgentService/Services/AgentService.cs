using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

using AgentService.Functions;

namespace AgentService.Services
{
    public class AgentService
    {
        private readonly ILogger<AgentService> _logger;
        private readonly IConfiguration _configuration;

        public AgentService(ILogger<AgentService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            Console.WriteLine("AgentService initialized with configuration: " + _configuration["AzureAI"]);
        }

        private PersistentAgentsClient GetPersistentAgentsClient()
        {
            return new PersistentAgentsClient(_configuration["AzureAI:ProjectEndpoint"], new DefaultAzureCredential());
        }

        // Helper function to execute the correct local C# function based on the tool call request from the agent
        private ToolOutput GetResolvedToolOutput(RequiredToolCall toolCall)
        {
            // Check if the required call is a function call
            if (toolCall is RequiredFunctionToolCall functionToolCall)
            {
                // Execute GetUserFavoriteCity if its name matches
                if (functionToolCall.Name == AgentFunctions.getUserFavoriteCityTool.Name)
                {
                    return new ToolOutput(toolCall, AgentFunctions.GetUserFavoriteCity());
                }
                // Parse the arguments provided by the agent for other functions
                using JsonDocument argumentsJson = JsonDocument.Parse(functionToolCall.Arguments);
                // Execute GetCityNickname if its name matches
                if (functionToolCall.Name == AgentFunctions.getCityNicknameTool.Name)
                {
                    // Extract the 'location' argument
                    string locationArgument = argumentsJson.RootElement.GetProperty("location").GetString();
                    return new ToolOutput(toolCall, AgentFunctions.GetCityNickname(locationArgument));
                }
                // Execute GetWeatherAtLocation if its name matches
                if (functionToolCall.Name == AgentFunctions.getCurrentWeatherAtLocationTool.Name)
                {
                    // Extract the 'location' argument
                    string locationArgument = argumentsJson.RootElement.GetProperty("location").GetString();
                    // Check if the optional 'unit' argument was provided
                    if (argumentsJson.RootElement.TryGetProperty("unit", out JsonElement unitElement))
                    {
                        string unitArgument = unitElement.GetString();
                        return new ToolOutput(toolCall, AgentFunctions.GetWeatherAtLocation(locationArgument, unitArgument));
                    }
                    // Call without the unit if it wasn't provided
                    return new ToolOutput(toolCall, AgentFunctions.GetWeatherAtLocation(locationArgument));
                }
            }
            // Return null if the tool call type isn't handled
            return null;
        }


        public void GetThreadCompletion()
        {
            PersistentAgentsClient client = GetPersistentAgentsClient();

            // Create the agent instance
            PersistentAgent agent = client.Administration.CreateAgent(
                model: _configuration["AzureAI:ModelDeploymentName"],
                name: "SDK Test Agent - Functions",
                instructions: "You are a weather bot. Use the provided functions to help answer questions. "
                    + "Customize your responses to the user's preferences as much as possible and use friendly "
                    + "nicknames for cities whenever possible.",
                tools: [
                    AgentFunctions.getUserFavoriteCityTool,
                    AgentFunctions.getCityNicknameTool,
                    AgentFunctions.getCurrentWeatherAtLocationTool
                    ]);

            // Create a new conversation thread for the agent
            PersistentAgentThread thread = client.Threads.CreateThread();
            Console.WriteLine($"Created thread with ID: {thread.Id}");

            // Add the initial user message to the thread
            client.Messages.CreateMessage(
                thread.Id,
                MessageRole.User,
                "What's the weather like in my favorite city?");

            // Start a run for the agent to process the messages in the thread
            ThreadRun run = client.Runs.CreateRun(thread.Id, agent.Id);

            // Loop to check the run status and handle required actions
            do
            {
                // Wait briefly before checking the status again
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                // Get the latest status of the run
                run = client.Runs.GetRun(thread.Id, run.Id);

                // Check if the agent requires a function call to proceed
                if (run.Status == RunStatus.RequiresAction
                    && run.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction)
                {
                    // Prepare a list to hold the outputs of the tool calls
                    List<ToolOutput> toolOutputs = [];
                    // Iterate through each required tool call
                    foreach (RequiredToolCall toolCall in submitToolOutputsAction.ToolCalls)
                    {
                        // Execute the function and get the output using the helper method
                        toolOutputs.Add(GetResolvedToolOutput(toolCall));
                    }
                    // Submit the collected tool outputs back to the run
                    run = client.Runs.SubmitToolOutputsToRun(run, toolOutputs);
                }
            }
            // Continue looping while the run is in progress or requires action
            while (run.Status == RunStatus.Queued
                || run.Status == RunStatus.InProgress
                || run.Status == RunStatus.RequiresAction);

            Console.WriteLine($"Run completed with status: {run.Status}");

            // if failed, show why
            if (run.Status == RunStatus.Failed)
            {
                Console.WriteLine($"Run failed with error: {run.LastError.Message}");
                return;
            }



            // Retrieve all messages from the completed thread, oldest first
            Pageable<PersistentThreadMessage> messages = client.Messages.GetMessages(
                threadId: thread.Id,
                order: ListSortOrder.Ascending
            );

            // Iterate through each message in the thread
            foreach (PersistentThreadMessage threadMessage in messages)
            {
                // Iterate through content items in the message (usually just one text item)
                foreach (MessageContent content in threadMessage.ContentItems)
                {
                    // Process based on content type
                    switch (content)
                    {

                        // If it's a text message
                        case MessageTextContent textItem:
                            // Print the role (user/agent) and the text content
                            Console.WriteLine($"[{threadMessage.Role}]: {textItem.Text}");
                            break;
                            // Add handling for other content types if necessary (e.g., images)
                        default:
                            // Print a message for unsupported content types
                            Console.WriteLine($"[{threadMessage.Role}]: Unsupported content type: {content.GetType().Name}");
                            break;
                    }
                }
            }
            
            // Delete the conversation thread
            client.Threads.DeleteThread(threadId: thread.Id);
            // Delete the agent definition
            client.Administration.DeleteAgent(agentId: agent.Id);

        }
    }
}

