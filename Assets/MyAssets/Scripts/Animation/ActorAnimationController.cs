using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actor 애니메이션 타입
/// </summary>
public enum ActorAnimationType
{
    Idle,
    Walk,
    Run,
    Jump,
    Attack,
    MeleeAttack,
    RangedAttack,
    SpecialAttack,
    TakeDamage,
    Death,
    Flee
}

/// <summary>
/// Actor Animation Controller
///
/// AI와 통합된 애니메이션 관리 시스템
/// Animator 파라미터를 통해 애니메이션 재생
///
/// 사용법:
/// 1. Actor GameObject에 Animator 컴포넌트 추가
/// 2. ActorAnimationController 컴포넌트 추가
/// 3. BT 노드에서 PlayAnimation() 호출
/// </summary>
public class ActorAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Animator 컴포넌트 (자동 탐색)")]
    public Animator AnimatorComponent;

    [Tooltip("애니메이션 전환 속도")]
    public float TransitionSpeed = 0.1f;

    [Tooltip("공격 애니메이션 재생 시간")]
    public float AttackAnimationDuration = 1.0f;

    [Tooltip("데미지 애니메이션 재생 시간")]
    public float DamageAnimationDuration = 0.5f;

    [Header("Debug")]
    public bool ShowDebugLogs = false;

    // ============================================================
    // INTERNAL
    // ============================================================
    private ActorAnimationType _currentAnimation = ActorAnimationType.Idle;
    private float _animationTimer = 0f;
    private bool _isPlayingTemporaryAnimation = false;

    // Animator 파라미터 이름
    private static readonly string PARAM_SPEED = "Speed";
    private static readonly string PARAM_IS_GROUNDED = "IsGrounded";
    private static readonly string PARAM_ATTACK_TRIGGER = "Attack";
    private static readonly string PARAM_DAMAGE_TRIGGER = "TakeDamage";
    private static readonly string PARAM_DEATH_TRIGGER = "Death";
    private static readonly string PARAM_ANIMATION_TYPE = "AnimationType";

    void Awake()
    {
        // Animator 자동 탐색
        if (AnimatorComponent == null)
        {
            AnimatorComponent = GetComponent<Animator>();
            if (AnimatorComponent == null)
            {
                Debug.LogWarning($"[ActorAnimationController] {gameObject.name}: Animator component not found!");
            }
        }
    }

    void Update()
    {
        // 임시 애니메이션 타이머 처리
        if (_isPlayingTemporaryAnimation)
        {
            _animationTimer -= Time.deltaTime;
            if (_animationTimer <= 0f)
            {
                _isPlayingTemporaryAnimation = false;
                // 기본 Idle로 복귀
                PlayAnimation(ActorAnimationType.Idle);
            }
        }
    }

    /// <summary>
    /// 애니메이션 재생
    /// </summary>
    public void PlayAnimation(ActorAnimationType animType)
    {
        if (AnimatorComponent == null)
            return;

        // 임시 애니메이션 재생 중이면 무시 (Attack, Damage 등)
        if (_isPlayingTemporaryAnimation && animType != ActorAnimationType.Death)
            return;

        _currentAnimation = animType;

        if (ShowDebugLogs)
        {
            KojeomLogger.DebugLog($"[AnimationController] {gameObject.name}: Playing {animType}");
        }

        switch (animType)
        {
            case ActorAnimationType.Idle:
                AnimatorComponent.SetFloat(PARAM_SPEED, 0f);
                AnimatorComponent.SetInteger(PARAM_ANIMATION_TYPE, 0);
                break;

            case ActorAnimationType.Walk:
                AnimatorComponent.SetFloat(PARAM_SPEED, 0.5f);
                AnimatorComponent.SetInteger(PARAM_ANIMATION_TYPE, 1);
                break;

            case ActorAnimationType.Run:
                AnimatorComponent.SetFloat(PARAM_SPEED, 1.0f);
                AnimatorComponent.SetInteger(PARAM_ANIMATION_TYPE, 2);
                break;

            case ActorAnimationType.Jump:
                AnimatorComponent.SetBool(PARAM_IS_GROUNDED, false);
                break;

            case ActorAnimationType.Attack:
            case ActorAnimationType.MeleeAttack:
                AnimatorComponent.SetTrigger(PARAM_ATTACK_TRIGGER);
                PlayTemporaryAnimation(AttackAnimationDuration);
                break;

            case ActorAnimationType.RangedAttack:
                AnimatorComponent.SetTrigger("RangedAttack");
                PlayTemporaryAnimation(AttackAnimationDuration);
                break;

            case ActorAnimationType.SpecialAttack:
                AnimatorComponent.SetTrigger("SpecialAttack");
                PlayTemporaryAnimation(AttackAnimationDuration * 1.5f);
                break;

            case ActorAnimationType.TakeDamage:
                AnimatorComponent.SetTrigger(PARAM_DAMAGE_TRIGGER);
                PlayTemporaryAnimation(DamageAnimationDuration);
                break;

            case ActorAnimationType.Death:
                AnimatorComponent.SetTrigger(PARAM_DEATH_TRIGGER);
                // 사망은 영구적
                break;

            case ActorAnimationType.Flee:
                // 도주는 Run과 동일하게 처리
                AnimatorComponent.SetFloat(PARAM_SPEED, 1.0f);
                AnimatorComponent.SetInteger(PARAM_ANIMATION_TYPE, 2);
                break;
        }
    }

    /// <summary>
    /// 임시 애니메이션 재생 (공격, 데미지 등)
    /// </summary>
    private void PlayTemporaryAnimation(float duration)
    {
        _isPlayingTemporaryAnimation = true;
        _animationTimer = duration;
    }

    /// <summary>
    /// 지면 접촉 상태 설정
    /// </summary>
    public void SetGrounded(bool grounded)
    {
        if (AnimatorComponent != null)
        {
            AnimatorComponent.SetBool(PARAM_IS_GROUNDED, grounded);
        }
    }

    /// <summary>
    /// 이동 속도 설정 (0.0 ~ 1.0)
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (AnimatorComponent != null)
        {
            AnimatorComponent.SetFloat(PARAM_SPEED, speed);
        }
    }

    /// <summary>
    /// 현재 재생 중인 애니메이션 타입
    /// </summary>
    public ActorAnimationType GetCurrentAnimation()
    {
        return _currentAnimation;
    }

    /// <summary>
    /// 공격 애니메이션 재생 중인지 확인
    /// </summary>
    public bool IsPlayingAttackAnimation()
    {
        return _isPlayingTemporaryAnimation &&
               (_currentAnimation == ActorAnimationType.Attack ||
                _currentAnimation == ActorAnimationType.MeleeAttack ||
                _currentAnimation == ActorAnimationType.RangedAttack ||
                _currentAnimation == ActorAnimationType.SpecialAttack);
    }

    /// <summary>
    /// 데미지 애니메이션 재생 중인지 확인
    /// </summary>
    public bool IsPlayingDamageAnimation()
    {
        return _isPlayingTemporaryAnimation && _currentAnimation == ActorAnimationType.TakeDamage;
    }
}
