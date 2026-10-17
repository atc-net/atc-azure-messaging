namespace Atc.Azure.Messaging.Tests.Common;

internal static class TestDispatcher
{
    public static MessageDispatcher<TestMessage, TProcessor, TMetadata> Create<TProcessor, TMetadata>(
        TProcessor processor,
        params Func<TMetadata, bool>[] filters)
        where TProcessor : class, IMessageProcessor<TestMessage>
        where TMetadata : MessageMetadata
    {
        var services = new ServiceCollection()
            .AddSingleton(processor)
            .BuildServiceProvider();

        return new MessageDispatcher<TestMessage, TProcessor, TMetadata>(
            services.GetRequiredService<IServiceScopeFactory>(),
            new JsonMessageSerializer(),
            filters,
            NullLogger.Instance);
    }

    public static BinaryData Serialize(TestMessage message)
        => new JsonMessageSerializer().Serialize(message);
}