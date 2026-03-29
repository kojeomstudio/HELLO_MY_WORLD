using UnityEngine;

/// <summary>
/// Simple logging utility for the game.
/// </summary>
public static class KojeomLoggerSimple
{
    public enum LOG_TYPE
    {
        INFO,
        ERROR,
        NETWORK_CLIENT_INFO,
        NETWORK_SERVER_INFO,
        DEBUG
    }

    public static void DebugLog(string message, LOG_TYPE type = LOG_TYPE.DEBUG)
    {
        switch (type)
        {
            case LOG_TYPE.INFO:
                Debug.Log($"[INFO] {message}");
                break;
            case LOG_TYPE.ERROR:
                Debug.LogError($"[ERROR] {message}");
                break;
            case LOG_TYPE.NETWORK_CLIENT_INFO:
                Debug.Log($"[NET_CLIENT] {message}");
                break;
            case LOG_TYPE.NETWORK_SERVER_INFO:
                Debug.Log($"[NET_SERVER] {message}");
                break;
            case LOG_TYPE.DEBUG:
            default:
                Debug.Log($"[DEBUG] {message}");
                break;
        }
    }

    public static void DebugLog(string message, LOG_TYPE type, Object context)
    {
        switch (type)
        {
            case LOG_TYPE.INFO:
                Debug.Log($"[INFO] {message}", context);
                break;
            case LOG_TYPE.ERROR:
                Debug.LogError($"[ERROR] {message}", context);
                break;
            case LOG_TYPE.NETWORK_CLIENT_INFO:
                Debug.Log($"[NET_CLIENT] {message}", context);
                break;
            case LOG_TYPE.NETWORK_SERVER_INFO:
                Debug.Log($"[NET_SERVER] {message}", context);
                break;
            case LOG_TYPE.DEBUG:
            default:
                Debug.Log($"[DEBUG] {message}", context);
                break;
        }
    }
}
