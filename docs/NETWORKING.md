# Networking Overview

This project uses Protobuf messages for all client-server communication.

## Code Generation
```
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

## Transport Abstraction
`INetworkTransport` allows swapping between UTP, NGO, or KojeomNet implementations.

## Message Flow
1. Transport receives bytes.
2. Bytes are deserialized into Protobuf messages.
3. `MessageDispatcher` routes messages to handlers such as `LoginHandler`.
