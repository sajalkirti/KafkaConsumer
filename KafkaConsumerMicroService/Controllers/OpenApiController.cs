using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KafkaConsumerMicroService.Controllers
{
    [ApiController]
    public class OpenApiController : ControllerBase
    {
        // Minimal static OpenAPI JSON for the important endpoints. This avoids runtime
        // generator conflicts and is lightweight for demos.
        [HttpGet]
        [Route("openapi")]
        public IActionResult GetOpenApi()
        {
            var doc = new Dictionary<string, object>
            {
                ["openapi"] = "3.0.1",
                ["info"] = new Dictionary<string, object>
                {
                    ["title"] = "KafkaConsumerMicroService API",
                    ["version"] = "1.0"
                },
                ["paths"] = new Dictionary<string, object>
                {
                    ["/api/health/ping"] = new Dictionary<string, object>
                    {
                        ["get"] = new Dictionary<string, object>
                        {
                            ["summary"] = "Health ping",
                            ["responses"] = new Dictionary<string, object>
                            {
                                ["200"] = new Dictionary<string, object>
                                {
                                    ["description"] = "OK"
                                }
                            }
                        }
                    },
                    ["/api/health/valid-records"] = new Dictionary<string, object>
                    {
                        ["get"] = new Dictionary<string, object>
                        {
                            ["summary"] = "Recent valid market records",
                            ["responses"] = new Dictionary<string, object>
                            {
                                ["200"] = new Dictionary<string, object>
                                {
                                    ["description"] = "A list of MarketRecord",
                                    ["content"] = new Dictionary<string, object>
                                    {
                                        ["application/json"] = new Dictionary<string, object>
                                        {
                                            ["schema"] = new Dictionary<string, object>
                                            {
                                                ["type"] = "array",
                                                ["items"] = new Dictionary<string, object>
                                                {
                                                    ["$ref"] = "#/components/schemas/MarketRecord"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/api/health/invalid-records"] = new Dictionary<string, object>
                    {
                        ["get"] = new Dictionary<string, object>
                        {
                            ["summary"] = "Recent invalid market records",
                            ["responses"] = new Dictionary<string, object>
                            {
                                ["200"] = new Dictionary<string, object>
                                {
                                    ["description"] = "A list of MarketRecord",
                                    ["content"] = new Dictionary<string, object>
                                    {
                                        ["application/json"] = new Dictionary<string, object>
                                        {
                                            ["schema"] = new Dictionary<string, object>
                                            {
                                                ["type"] = "array",
                                                ["items"] = new Dictionary<string, object>
                                                {
                                                    ["$ref"] = "#/components/schemas/MarketRecord"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["components"] = new Dictionary<string, object>
                {
                    ["schemas"] = new Dictionary<string, object>
                    {
                        ["MarketRecord"] = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["id"] = new Dictionary<string, object> { ["type"] = "string", ["format"] = "uuid" },
                                ["sourceSystem"] = new Dictionary<string, object> { ["type"] = "string" },
                                ["accountNumber"] = new Dictionary<string, object> { ["type"] = "integer", ["format"] = "int64" },
                                ["pnlAmount"] = new Dictionary<string, object> { ["type"] = "number", ["format"] = "double" },
                                ["isValid"] = new Dictionary<string, object> { ["type"] = "boolean" },
                                ["receivedAt"] = new Dictionary<string, object> { ["type"] = "string", ["format"] = "date-time" },
                                ["rawPayload"] = new Dictionary<string, object> { ["type"] = "string" },
                                ["metadata"] = new Dictionary<string, object> { ["type"] = "string" }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            return Content(json, "application/json");
        }

        [HttpGet]
        [Route("openapi/ui")]
        public IActionResult GetUi()
        {
            var html = @"<!doctype html>
<html>
  <head>
    <meta charset='utf-8'/>
    <title>API Docs</title>
    <script src='https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js'></script>
  </head>
  <body>
    <redoc spec-url='/openapi'></redoc>
  </body>
</html>";

            return Content(html, "text/html");
        }
    }
}
