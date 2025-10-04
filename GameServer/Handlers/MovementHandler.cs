using System;
using GameServerApp.Models;
using GameServerApp.Database;
using GameServerApp.Systems;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// ?뚮젅?댁뼱 ?대룞 ?붿껌??泥섎━?섎뒗 ?몃뱾??
/// ?대룞 ?좏슚??寃利? ?꾩튂 ?낅뜲?댄듃, ?ㅻⅨ ?뚮젅?댁뼱?먭쾶 ?뚮┝ ?깆쓣 ?대떦?⑸땲??
/// </summary>
public class MovementHandler : MessageHandler<MoveRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly EntitySyncService _entitySync;
    
    // ?대룞 ?띾룄 ?쒗븳 (?⑥쐞: ?좊떅/珥?
    private const float MAX_MOVEMENT_SPEED = 10.0f;
    private const float MIN_MOVEMENT_SPEED = 0.1f;

    public MovementHandler(DatabaseHelper database, SessionManager sessions, EntitySyncService entitySync) : base(MessageType.MoveRequest)
    {
        _database = database;
        _sessions = sessions;
        _entitySync = entitySync;
    }

    protected override async Task HandleAsync(Session session, MoveRequest message)
    {
        try
        {
            // ?몄뀡 ?몄쬆 ?뺤씤
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendMoveFailure(session, "?몄쬆?섏? ?딆? ?몄뀡?낅땲??");
                return;
            }

            // ?깅줉???몄뀡?몄? ?뺤씤
            if (_sessions.GetSession(session.UserName) != session)
            {
                await SendMoveFailure(session, "?섎せ???몄뀡?낅땲??");
                return;
            }

            // ?낅젰 寃利?
            if (message.TargetPosition == null)
            {
                await SendMoveFailure(session, "紐⑺몴 ?꾩튂媛 吏?뺣릺吏 ?딆븯?듬땲??");
                return;
            }

            // ?대룞 ?띾룄 寃利?
            if (message.MovementSpeed < MIN_MOVEMENT_SPEED || message.MovementSpeed > MAX_MOVEMENT_SPEED)
            {
                await SendMoveFailure(session, $"?섎せ???대룞 ?띾룄?낅땲?? (?덉슜 踰붿쐞: {MIN_MOVEMENT_SPEED} - {MAX_MOVEMENT_SPEED})");
                return;
            }

            // ?꾩옱 ?뚮젅?댁뼱 ?곹깭 媛?몄삤湲?
            var playerState = _sessions.GetPlayerState(session.UserName);
            if (playerState == null)
            {
                await SendMoveFailure(session, "?뚮젅?댁뼱 ?곹깭瑜?李얠쓣 ???놁뒿?덈떎.");
                return;
            }

            // ?대룞 嫄곕━ 諛??좏슚??寃利?
            var currentPos = new SharedProtocol.Vector3((float)playerState.Position.X, (float)playerState.Position.Y, (float)playerState.Position.Z);
            var targetPos = message.TargetPosition;
            
            if (!await ValidateMovement(currentPos, targetPos, message.MovementSpeed))
            {
                await SendMoveFailure(session, "?섎せ???대룞 ?붿껌?낅땲??");
                return;
            }

            // ?뚮젅?댁뼱 ?꾩튂 ?낅뜲?댄듃 (SessionManager瑜??듯빐)
            var newPositionClient = new SharedProtocol.Vector3(targetPos.X, targetPos.Y, targetPos.Z);
            var newPositionServer = new GameServerApp.Vector3(targetPos.X, targetPos.Y, targetPos.Z);
            _sessions.UpdatePlayerState(session.UserName, newPositionServer, 0f, 0f);

            // 泥?겕 ?뺣낫 ?낅뜲?댄듃
            var chunkX = (int)Math.Floor(targetPos.X / 16);
            var chunkZ = (int)Math.Floor(targetPos.Z / 16);
            _sessions.UpdatePlayerWorld(session.UserName, playerState.CurrentWorldId, chunkX, chunkZ);

            // ?몄뀡???뚮젅?댁뼱 ?뺣낫???낅뜲?댄듃
            if (session.PlayerInfo != null)
            {
                session.PlayerInfo.Position = newPositionClient;
            }

            // ?깃났 ?묐떟 ?꾩넚
            var response = new MoveResponse
            {
                Success = true,
                NewPosition = newPositionClient,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            await session.SendAsync(MessageType.MoveResponse, response);

            // ?ㅻⅨ ?뚮젅?댁뼱?ㅼ뿉寃??꾩튂 ?낅뜲?댄듃 釉뚮줈?쒖틦?ㅽ듃 (?좏깮?ы빆)
            await BroadcastPlayerMovement(session, targetPos);

            Console.WriteLine($"Player {session.UserName} moved to ({targetPos.X:F2}, {targetPos.Y:F2}, {targetPos.Z:F2})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Movement error for user '{session.UserName}': {ex.Message}");
            await SendMoveFailure(session, "?대룞 泥섎━ 以??ㅻ쪟媛 諛쒖깮?덉뒿?덈떎.");
        }
    }

    /// <summary>
    /// ?대룞 ?ㅽ뙣 ?묐떟??蹂대깄?덈떎.
    /// </summary>
    private async Task SendMoveFailure(Session session, string errorMessage)
    {
        var response = new MoveResponse 
        { 
            Success = false,
            NewPosition = session.PlayerInfo?.Position ?? new SharedProtocol.Vector3(0, 0, 0),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.MoveResponse, response);
        Console.WriteLine($"Movement failed for {session.UserName}: {errorMessage}");
    }

    /// <summary>
    /// ?대룞 ?붿껌???좏슚?깆쓣 寃利앺빀?덈떎.
    /// </summary>
    private async Task<bool> ValidateMovement(SharedProtocol.Vector3 currentPos, SharedProtocol.Vector3 targetPos, float movementSpeed)
    {
        await Task.Delay(5); // 寃利?泥섎━ ?쒕??덉씠??
        
        // 嫄곕━ 怨꾩궛
        var deltaX = targetPos.X - currentPos.X;
        var deltaY = targetPos.Y - currentPos.Y;
        var deltaZ = targetPos.Z - currentPos.Z;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        
        // ?덈Т 硫由??대룞?섎뒗 寃껋쓣 諛⑹? (移섑듃 諛⑹?)
        const double MAX_SINGLE_MOVE_DISTANCE = 50.0; // ??踰덉뿉 理쒕? 50?좊떅源뚯?留??대룞 媛??
        if (distance > MAX_SINGLE_MOVE_DISTANCE)
        {
            Console.WriteLine($"Movement rejected: distance too large ({distance:F2} > {MAX_SINGLE_MOVE_DISTANCE})");
            return false;
        }

        // TODO: 異붽? 寃利?
        // - ?μ븷臾?異⑸룎 寃??
        // - 留?寃쎄퀎 寃??
        // - ?뚮젅?댁뼱 ?곹깭 ?뺤씤 (湲곗젅, ?뺤? ??
        // - ?대룞 媛??吏???뺤씤
        
        return true;
    }

    /// <summary>
    /// ?ㅻⅨ ?뚮젅?댁뼱?ㅼ뿉寃??뚮젅?댁뼱???대룞??釉뚮줈?쒖틦?ㅽ듃?⑸땲??
    /// </summary>
    private async Task BroadcastPlayerMovement(Session movedSession, SharedProtocol.Vector3 newPosition)
    {
        try
        {
            await _entitySync.BroadcastPlayerUpdateAsync(movedSession, newPosition);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EntitySync] Movement broadcast failed for {movedSession.UserName}: {ex.Message}");
        }
    }
}




