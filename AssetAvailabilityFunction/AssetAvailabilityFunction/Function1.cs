using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AssetAvailabilityFunction
{
    // 1. The response object matching your desired JSON output
    public class AvailabilityResponse
    {
        public string AssetId { get; set; }
        public bool Available { get; set; }
        public string Reason { get; set; }
        public string NextMaintenance { get; set; }
    }

    // 2. A simple mock model to represent our assets
    public class Asset
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string NextMaintenanceDate { get; set; }
    }

    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        // 3. Mock data (acting as our database)
        private static readonly Dictionary<string, Asset> MockAssets = new()
        {
            { "EX-01", new Asset { Id = "EX-01", Type = "Excavator", NextMaintenanceDate = "2026-11-01" } },
            { "EX-02", new Asset { Id = "EX-02", Type = "Excavator", NextMaintenanceDate = "2026-10-25" } },
            { "HT-05", new Asset { Id = "HT-05", Type = "Haul Truck", NextMaintenanceDate = "2026-12-15" } },
            { "HT-06", new Asset { Id = "HT-06", Type = "Haul Truck", NextMaintenanceDate = "2026-10-18" } }
        };

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        // 4. The HTTP Trigger with a custom RESTful route
        [Function("GetAssetAvailability")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "assets/{id}/availability")] HttpRequest req,
            string id) // The '{id}' from the route is automatically bound here
        {
            _logger.LogInformation($"Received availability request for asset: {id}");

            // 5. Get and validate the 'date' query parameter
            string dateQuery = req.Query["date"];
            if (string.IsNullOrEmpty(dateQuery) || !DateTime.TryParse(dateQuery, out DateTime requestedDate))
            {
                return new BadRequestObjectResult("Please provide a valid 'date' query parameter in YYYY-MM-DD format.");
            }

            // 6. Check if the asset exists
            if (!MockAssets.TryGetValue(id, out Asset asset))
            {
                return new NotFoundObjectResult($"Asset with ID '{id}' not found.");
            }

            // 7. --- BUSINESS LOGIC ---
            // Simulate a scheduling rule: An asset is unavailable if the requested date 
            // falls within 7 days *before* its scheduled maintenance date.
            DateTime nextMaintDate = DateTime.Parse(asset.NextMaintenanceDate);
            bool isAvailable = true;
            string reason = "No scheduled maintenance";

            if (requestedDate >= nextMaintDate.AddDays(-7) && requestedDate <= nextMaintDate)
            {
                isAvailable = false;
                reason = "Scheduled maintenance window";
            }

            // 8. Build and return the JSON response
            var response = new AvailabilityResponse
            {
                AssetId = asset.Id,
                Available = isAvailable,
                Reason = reason,
                NextMaintenance = asset.NextMaintenanceDate
            };

            return new OkObjectResult(response);
        }
    }
}