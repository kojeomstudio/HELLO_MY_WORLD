using System;
using System.Collections.Generic;

namespace Networking.Core
{
    /// <summary>
    /// Routes decoded protobuf messages to registered handlers.
    /// </summary>
    public class MessageDispatcher
    {
        private readonly Dictionary<Type, Action<object>> _handlers = new();

        /// <summary>Register a handler for message type T.</summary>
        public void Register<T>(Action<T> handler) => _handlers[typeof(T)] = msg => handler((T)msg);

        /// <summary>Dispatch message to its handler if registered.</summary>
        public void Dispatch(object message)
        {
            if (message == null) return;
            if (_handlers.TryGetValue(message.GetType(), out var handler))
                handler(message);
        }
    }
}
