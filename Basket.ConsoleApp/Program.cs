using Basket.API;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

namespace Basket.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5103");
            var client = new TestGrpc.TestGrpcClient(channel);

            var response = client.GetBasketById(new Request { Id = "Hello World!" });

            Console.WriteLine("TestGrpc response id: " + response.Id);
            Console.ReadKey();

        }
    }
}
