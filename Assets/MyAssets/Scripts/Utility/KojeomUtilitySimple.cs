using UnityEngine;

/// <summary>
/// Simple utility functions for the game.
/// </summary>
public static class KojeomUtilitySimple
{
    /// <summary>
    /// Change the world seed for terrain generation.
    /// </summary>
    public static void ChangeSeed(int seedValue)
    {
        Debug.Log($"World seed changed to: {seedValue}");

        // TODO: Implement actual seed change logic by hooking into the terrain generation system.
    }
}
