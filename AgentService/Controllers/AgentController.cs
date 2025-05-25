using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AgentService.Services;

namespace AgentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly Services.AgentService _agentService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(AgentService.Services.AgentService agentService, ILogger<AgentController> logger)
        {
            _agentService = agentService;
            _logger = logger;
        }

        /// <summary>
        /// Runs the agent to process a weather question
        /// </summary>
        /// <returns>A success message</returns>
        [HttpGet("run")]
        public IActionResult RunAgent()
        {
            try
            {
                _logger.LogInformation("Starting agent execution");
                _agentService.GetThreadCompletion();
                return Ok(new { Message = "Agent execution completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing agent");
                return StatusCode(500, new { Error = "Failed to execute agent", Message = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>A simple message indicating the API is working</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }
    }
}
