using System;

namespace Networking.Core
{
    /// <summary>
    /// Client-side mirror of server MessageType enum for framing.
    /// Keep values in sync with SharedProtocol.MessageType.
    /// </summary>
    public enum ClientMessageType
    {
        // 인증 관련
        LoginRequest = 1,
        LoginResponse = 2,
        LogoutRequest = 3,
        LogoutResponse = 4,

        // 이동 관련
        MoveRequest = 10,
        MoveResponse = 11,

        // 월드/블록 관련
        WorldBlockChangeRequest = 20,
        WorldBlockChangeResponse = 21,
        WorldBlockChangeBroadcast = 22,

        // 채팅 관련
        ChatRequest = 30,
        ChatResponse = 31,
        ChatMessage = 32,

        // 서버 상태/진단
        PingRequest = 40,
        PingResponse = 41,
        ServerStatusRequest = 42,
        ServerStatusResponse = 43,

        // 플레이어 정보 업데이트
        PlayerInfoUpdate = 50,

        // AI 시스템 (Server-Authoritative)
        AIStateSyncBroadcast = 100,
        AIAttackEventBroadcast = 101,
        AIDeathEventBroadcast = 102,
        AISpawnRequest = 103,
        AISpawnResponse = 104,
        AIDebugInfoRequest = 105,
        AIDebugInfoResponse = 106,

        // 전투 시스템 (PvP/PvE)
        PlayerAttackRequest = 110,
        PlayerAttackResponse = 111,
        PlayerAttackBroadcast = 112,

        // 명령어 시스템
        CommandRequest = 120,
        CommandResponse = 121,
        CommandBroadcast = 122,
    }
}
namespace Networking.Core
{
    /// <summary>
    /// Client-side mirror of server MessageType enum for framing.
    /// Keep values in sync with SharedProtocol.MessageType.
    /// </summary>
    public enum ClientMessageType
    {
        // 인증 관련
        LoginRequest = 1,
        LoginResponse = 2,
        LogoutRequest = 3,
        LogoutResponse = 4,

        // 이동 관련
        MoveRequest = 10,
        MoveResponse = 11,

        // 월드/블록 관련
        WorldBlockChangeRequest = 20,
        WorldBlockChangeResponse = 21,
        WorldBlockChangeBroadcast = 22,

        // 채팅 관련
        ChatRequest = 30,
        ChatResponse = 31,
        ChatMessage = 32,

        // 서버 상태/진단
        PingRequest = 40,
        PingResponse = 41,
        ServerStatusRequest = 42,
        ServerStatusResponse = 43,

        // 플레이어 정보 업데이트
        PlayerInfoUpdate = 50,

        // AI 시스템 (Server-Authoritative)
        AIStateSyncBroadcast = 100,
        AIAttackEventBroadcast = 101,
        AIDeathEventBroadcast = 102,
        AISpawnRequest = 103,
        AISpawnResponse = 104,
        AIDebugInfoRequest = 105,
        AIDebugInfoResponse = 106,

        // 전투 시스템 (PvP/PvE)
        PlayerAttackRequest = 110,
        PlayerAttackResponse = 111,
        PlayerAttackBroadcast = 112,

        // 명령어 시스템
        CommandRequest = 120,
        CommandResponse = 121,
        CommandBroadcast = 122,
    }
}
