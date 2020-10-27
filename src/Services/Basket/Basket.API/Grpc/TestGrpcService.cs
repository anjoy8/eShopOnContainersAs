using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API
{
    public class TestGrpcService : TestGrpc.TestGrpcBase
    {
        private readonly ILogger<TestGrpcService> _logger;

        public TestGrpcService(ILogger<TestGrpcService> logger)
        {
            _logger = logger;
        }

        public override async Task<Response> GetBasketById(Request request, ServerCallContext context)
        {
            return await Task.FromResult(new Response
            {
                Id = "88"
            });
        }
    }
}
