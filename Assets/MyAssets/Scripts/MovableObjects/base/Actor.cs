using UnityEngine;

/*
 * 
 * 대분류 타입은 대문자로, 소분류 타입은 소문자로
 * // 동물의 경우 카테고리별로 세부적인 구분을 한다.
 * 
 */

public enum ACTOR_TYPE
{
    NPC,
    MONSTER,
    ANIMAL,
    COUNT
}

public enum NPC_TYPE
{
    Merchant,
    Guard,
    COUNT
}

public enum MONSTER_TYPE
{
    // to do
    Cyclopes,
    Fiery,
    COUNT
}

public enum ANIMAL_CATEGORY
{
    None,
    Herbivore, // 초식 동물.
    FleshEating, // 육식 동물.
    Polyphagia // 잡식 동물.
}

public enum ANIMAL_TYPE
{
    Chick,
    Chiken,
    COUNT
}

public abstract class ActorSpawnData
{
    public int HP;
    public int MP;
    public int AP;
    public string NAME;
    public ACTOR_TYPE ActorType;
    public string ResourceID;
    public int UniqueID;
}

public class NPCSpawnData : ActorSpawnData
{
    public NPC_TYPE NpcType;
}

public class MonsterSpawnData : ActorSpawnData
{
    public MONSTER_TYPE MonsterType;
}

public class AnimalSpawnData : ActorSpawnData
{
    public ANIMAL_TYPE AnimalType;
    public ANIMAL_CATEGORY AnimalCategory;
}


abstract public class Actor : MonoBehaviour
{
    protected ACTOR_TYPE ActorType;
    protected int _healthPoint;
    protected int _maxHealthPoint = 100;
    protected int _magicaPoint;
    protected int _maxMagicaPoint = 100;
    protected int _attackPoint;
    protected string Name;

    /// <summary>
    /// 현재 체력 (BlackBoard 자동 동기화)
    /// </summary>
    public int HealthPoint
    {
        get => _healthPoint;
        set
        {
            _healthPoint = Mathf.Clamp(value, 0, _maxHealthPoint);

            // BlackBoard에 자동 동기화
            if (Controller != null)
            {
                ActorController actorController = Controller as ActorController;
                if (actorController != null)
                {
                    actorController.UpdateHealthRatio((float)_healthPoint / _maxHealthPoint);
                }
            }
        }
    }

    /// <summary>
    /// 최대 체력
    /// </summary>
    public int MaxHealthPoint
    {
        get => _maxHealthPoint;
        set => _maxHealthPoint = Mathf.Max(1, value);
    }

    /// <summary>
    /// 현재 마나
    /// </summary>
    public int MagicaPoint
    {
        get => _magicaPoint;
        set => _magicaPoint = Mathf.Clamp(value, 0, _maxMagicaPoint);
    }

    /// <summary>
    /// 최대 마나
    /// </summary>
    public int MaxMagicaPoint
    {
        get => _maxMagicaPoint;
        set => _maxMagicaPoint = Mathf.Max(1, value);
    }

    /// <summary>
    /// 공격력
    /// </summary>
    public int AttackPoint
    {
        get => _attackPoint;
        set => _attackPoint = Mathf.Max(0, value);
    }

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public virtual void TakeDamage(int damage, Actor attacker = null)
    {
        HealthPoint -= damage;

        // 데미지 애니메이션 재생
        PlayDamageAnimation();

        // AI에 데미지 이벤트 전달
        if (Controller != null)
        {
            ActorController actorController = Controller as ActorController;
            if (actorController != null && attacker != null)
            {
                actorController.OnDamageReceived(attacker.gameObject, attacker.transform.position, damage);
            }
        }

        // 사망 체크
        if (HealthPoint <= 0)
        {
            OnDeath();
        }
    }

    /// <summary>
    /// 데미지 애니메이션 재생
    /// </summary>
    private void PlayDamageAnimation()
    {
        ActorAnimationController animController = GetComponent<ActorAnimationController>();
        if (animController != null)
        {
            animController.PlayAnimation(ActorAnimationType.TakeDamage);
        }
    }

    /// <summary>
    /// 체력 회복
    /// </summary>
    public virtual void Heal(int amount)
    {
        HealthPoint += amount;
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    protected virtual void OnDeath()
    {
        KojeomLogger.DebugLog($"[Actor] {Name} died");

        // 사망 애니메이션 재생
        ActorAnimationController animController = GetComponent<ActorAnimationController>();
        if (animController != null)
        {
            animController.PlayAnimation(ActorAnimationType.Death);
        }

        // TODO: 아이템 드롭, 경험치 등
    }
    /// <summary>
    /// Actor가 가지고 있는 리소스 식별자.
    /// </summary>
    protected string ResourceID;
    /// <summary>
    /// Actor가 월드에 생성되면 발급받는 식별자. ( 클라이언트에서 사용되는 ID )
    /// </summary>
    protected int SpawnID;
    /// <summary>
    /// 네트워크 상에서 부여받는 Actor ID 값.
    /// </summary>
    protected int NetID;
    /// <summary>
    /// Actor가 가지고 있는 유일한 Key 식별자. 
    /// (같은 리소스이면서 다른 이름을 가진 객체를 구분하기 위해 사용)
    /// </summary>
    protected int UniqueID;
    protected ActorController Controller;

    public delegate void del_OnClickActor(Actor actor);
    abstract public event del_OnClickActor OnClickedActor;
    abstract public void Init(ActorSpawnData spawnData, SubWorld world, int spawnID);
    abstract public ActorController GetController();
    abstract public void Update();

    public ACTOR_TYPE GetActorType()
    {
        return ActorType;
    }
    public int GetSpawnID()
    {
        return SpawnID;
    }
    public string GetResourceID()
    {
        return ResourceID;
    }
    public int GetUniqueID()
    {
        return UniqueID;
    }
    public int GetNetID()
    {
        return NetID;
    }
    public void Show()
    {
        gameObject.SetActive(true);
        Controller.StartController();
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        Controller.StopController();
    }
}

abstract public class NPCActor : Actor
{
    protected NPC_TYPE NpcType;
}

abstract public class MonsterActor : Actor
{
    protected MONSTER_TYPE MonsterType;
}

abstract public class AnimalActor : Actor
{
    protected ANIMAL_TYPE AnimalType;
    protected ANIMAL_CATEGORY AnimalCategory;
}
