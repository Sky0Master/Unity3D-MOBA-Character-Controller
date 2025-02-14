using UnityEngine;

public class UnitController : MonoBehaviour
{
   [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private LayerMask groundLayerMask;

    private UnityEngine.AI.NavMeshAgent navAgent;
    private bool isCastingSkill;
    private Vector3 pendingDestination;
    private bool hasPendingMovement;

    private void Awake()
    {
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        ConfigureNavAgent();
    }

    private void ConfigureNavAgent()
    {
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = 0; // 禁用NavMesh自带旋转
        navAgent.acceleration = 999;
        navAgent.stoppingDistance = 0.1f;
        navAgent.updateRotation = false;
    }

    private void Update()
    {
        ConfigureNavAgent();    //更新移速参数
        HandleInput();
        UpdateRotation();
    }

    private void HandleInput()
    {
        // 鼠标右键点击移动
        if (Input.GetMouseButtonDown(1))
        {
            if (GameUtils.TryGetMouseWorldPosition(out Vector3 targetPos, groundLayerMask))
            {
                if (isCastingSkill)
                {
                    // 技能释放中，暂存移动指令
                    pendingDestination = targetPos;
                    hasPendingMovement = true;
                }
                else
                {
                    MoveTo(targetPos);
                }
            }
        }

        // 按下S键立即停止
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopMovement();
        }
    }

    private void MoveTo(Vector3 targetPosition)
    {
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPosition);
    }

    private void StopMovement()
    {
        navAgent.isStopped = true;
        navAgent.ResetPath();
        hasPendingMovement = false;
    }

    private void UpdateRotation()
    {
        if (navAgent.velocity.sqrMagnitude > Mathf.Epsilon)
        {
            Vector3 lookDirection = new Vector3(
                navAgent.velocity.x,
                0,
                navAgent.velocity.z
            ).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    #region Skill System Interface
    // 技能系统调用接口 ---------------------------------
    public void StartSkillCast()
    {
        isCastingSkill = true;
        StopMovement();
    }

    public void FinishSkillCast()
    {
        isCastingSkill = false;
        if (hasPendingMovement)
        {
            MoveTo(pendingDestination);
            hasPendingMovement = false;
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        if (navAgent && navAgent.hasPath)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(navAgent.destination, 0.5f);
            Gizmos.DrawLine(transform.position, navAgent.destination);
        }
    }
    #endregion
}
