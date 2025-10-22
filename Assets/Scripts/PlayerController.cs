using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed;
    public Scanner scanner;
    public Hand[] hands;
    public RuntimeAnimatorController[] animCon;

    Rigidbody2D rigid;
    SpriteRenderer sprite;
    Animator anim;
    PhotonView pv;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        scanner = GetComponent<Scanner>();
        hands = GetComponentsInChildren<Hand>(true);

        pv = GetComponent<PhotonView>();
    }

    private void OnEnable()
    {
        speed *= Character.Speed;
        if (GameManager.instance != null)
            anim.runtimeAnimatorController = animCon[GameManager.instance.playerId];
    }

    private void FixedUpdate()
    {
        // 로컬 소유자만 입력/이동 처리(멀티 환경에서 PhotonView 사용 시 확장 가능)
        if (pv != null && !pv.IsMine)
            return;

        if (!GameManager.instance.isLive)
            return;

        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
    }

    private void LateUpdate()
    {
        if (pv != null && !pv.IsMine)
            return;

        if (!GameManager.instance.isLive)
            return;

        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0)
            sprite.flipX = inputVec.x < 0;
    }

    private void OnMove(InputValue value)
    {
        if (pv != null && !pv.IsMine)
            return;

        inputVec = value.Get<Vector2>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (pv != null && !pv.IsMine)
            return;

        if (!GameManager.instance.isLive)
            return;

        GameManager.instance.health -= Time.deltaTime * 10;

        if (GameManager.instance.health <= 0)
        {
            for (int i = 2; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }

            anim.SetTrigger("Dead");
            GameManager.instance.GameOver();
        }
    }
}
