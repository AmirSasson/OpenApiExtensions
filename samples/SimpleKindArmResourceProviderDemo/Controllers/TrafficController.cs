using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleKindArmResourceProviderDemo.WebModels.Traffic;

namespace SimpleKindArmResourceProviderDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrafficController : ControllerBase
    {
        private static readonly Dictionary<string, TrafficResource> Db = new Dictionary<string, TrafficResource>();
        private readonly ILogger<TrafficController> _logger;

        public TrafficController(ILogger<TrafficController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<TrafficResource> Get()
        {
            return Db.Values;
        }

        [HttpPut]
        public IActionResult Put(TrafficResource traffic)
        {
            Db[traffic.Id] = traffic;
            return Ok();
        }
    }
}
