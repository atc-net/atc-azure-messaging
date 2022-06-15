using Atc.Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.Tests.ServiceBus
{
    public sealed class ServiceBusSenderProviderTests
    {
        private readonly ServiceBusClient client;
        private readonly ServiceBusSenderProvider sut;

        public ServiceBusSenderProviderTests()
        {
            client = Substitute.For<ServiceBusClient>();
            client
                .CreateSender(default)
                .ReturnsForAnyArgs(Substitute.For<ServiceBusSender>());

            var factory = Substitute.For<IServiceBusClientFactory>();
            factory.Create().Returns(client);

            sut = new ServiceBusSenderProvider(factory);
        }

        [Theory, AutoData]
        public void Get_CreatesSender_On_Client(
            string topicName)
        {
            sut.GetSender(topicName);

            client
                .Received(1)
                .CreateSender(topicName);
        }

        [Theory, AutoData]
        public void Get_Returns_ServiceBusSender(
            string topicName)
            => sut.GetSender(topicName)
                .Should()
                .BeAssignableTo<ServiceBusSender>();

        [Theory, AutoData]
        public void Get_Reuses_Sender_For_Same_TopicName(
            string topicName)
        {
            sut.GetSender(topicName);
            sut.GetSender(topicName);
            client
                .ReceivedWithAnyArgs(1)
                .CreateSender(default);
        }

        [Theory, AutoData]
        public void Get_Creates_New_Senders_For_Different_TopicName(
            string topicName1,
            string topicName2)
        {
            sut.GetSender(topicName1);
            sut.GetSender(topicName2);
            client
                .ReceivedWithAnyArgs(2)
                .CreateSender(default);
        }
    }
}