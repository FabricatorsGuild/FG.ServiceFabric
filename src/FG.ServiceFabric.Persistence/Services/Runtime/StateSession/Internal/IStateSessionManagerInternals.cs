using System;
using System.Collections.Generic;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Nito.AsyncEx;

namespace FG.ServiceFabric.Services.Runtime.StateSession.Internal
{
    public interface IStateSessionManagerInternals : IStateSessionManager
    {
        IDictionary<string, QueueInfo> OpenQueues { get; }

        AsyncReaderWriterLock Lock { get; }

        IServiceMetadata GetMetadata();

        IValueMetadata GetOrCreateMetadata(IValueMetadata metadata, StateWrapperType type);

        StateWrapper BuildWrapper(IValueMetadata valueMetadata, SchemaStateKey key);

        StateWrapper BuildWrapper(IValueMetadata metadata, SchemaStateKey key, Type valueType,
            object value);

        StateWrapper<T> BuildWrapperGeneric<T>(IValueMetadata valueMetadata, SchemaStateKey key, T value);
        string GetEscapedKey(string key);
        string GetUnescapedKey(string key);

        SchemaStateKey GetKey(ISchemaKey id);
    }
}