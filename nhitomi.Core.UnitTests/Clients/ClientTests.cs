using System.Linq;
using System.Threading.Tasks;
using nhitomi.Core.Clients;
using nhitomi.Core.Clients.Hitomi;
using nhitomi.Core.Clients.nhentai;
using NUnit.Framework;

namespace nhitomi.Core.UnitTests.Clients
{
    public class ClientTests
    {
        [Test]
        public async Task nhentaiClient()
        {
            var client = new nhentaiClient(
                TestUtils.HttpClient,
                TestUtils.Serializer,
                TestUtils.Logger<nhentaiClient>());

            await RunTestAsync(client);
        }

        [Test]
        public async Task nhentaiEnumerate()
        {
            var client = new nhentaiClient(
                TestUtils.HttpClient,
                TestUtils.Serializer,
                TestUtils.Logger<nhentaiClient>());

            await EnumerateAsync(client);
        }

        [Test]
        public async Task HitomiClient()
        {
            var client = new HitomiClient(
                TestUtils.HttpClient,
                TestUtils.Serializer,
                TestUtils.Logger<HitomiClient>());

            await RunTestAsync(client);
        }

        [Test]
        public async Task HitomiEnumerate()
        {
            var client = new HitomiClient(
                TestUtils.HttpClient,
                TestUtils.Serializer,
                TestUtils.Logger<HitomiClient>());

            await EnumerateAsync(client);
        }

        static async Task RunTestAsync(IDoujinClient client)
        {
            var tester = new ClientTester();

            if (!await tester.TestAsync(client))
                tester.ThrowExceptions();
        }

        static async Task EnumerateAsync(IDoujinClient client)
        {
            var ids = (await client.EnumerateAsync()).ToArray();

            Assert.That(ids, Is.Not.Zero);

            var d = await client.GetAsync(ids[ids.Length - 1]);

            Assert.That(d, Is.Not.Null);
        }
    }
}