
using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AgentService.Functions
{
    class AgentFunctions
    {
        // Function to get the user's favorite city (hardcoded for example)
        public static string GetUserFavoriteCity() => "Seattle, WA";

        // Definition for the GetUserFavoriteCity function, describing its purpose to the agent
        public static FunctionToolDefinition getUserFavoriteCityTool = new("getUserFavoriteCity", "Gets the user's favorite city.");

        // Function to get a city's nickname based on its location
        public static string GetCityNickname(string location) => location switch
        {
            "Seattle, WA" => "The Emerald City",
            // Handle cases where the nickname is not known
            _ => throw new NotImplementedException(),
        };
        // Definition for the GetCityNickname function, including parameter description
        public static FunctionToolDefinition getCityNicknameTool = new(
            name: "getCityNickname",
            description: "Gets the nickname of a city, e.g. 'LA' for 'Los Angeles, CA'.",
            // Define the expected parameters (location string)
            parameters: BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Location = new
                        {
                            Type = "string",
                            Description = "The city and state, e.g. San Francisco, CA",
                        },
                    },
                    Required = new[] { "location" },
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        // Function to get weather at a specific location, with an optional temperature unit
        public static string GetWeatherAtLocation(string location, string temperatureUnit = "f") => location switch
        {
            "Seattle, WA" => temperatureUnit == "f" ? "70f" : "21c",
            // Handle cases where weather data is not available
            _ => throw new NotImplementedException()
        };
        // Definition for the GetWeatherAtLocation function, specifying parameters and enum for unit
        public static FunctionToolDefinition getCurrentWeatherAtLocationTool = new(
            name: "getCurrentWeatherAtLocation",
            description: "Gets the current weather at a provided location.",
            // Define expected parameters (location string, optional unit enum)
            parameters: BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Location = new
                        {
                            Type = "string",
                            Description = "The city and state, e.g. San Francisco, CA",
                        },
                        Unit = new
                        {
                            Type = "string",
                            Enum = new[] { "c", "f" },
                        },
                    },
                    Required = new[] { "location" },
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
