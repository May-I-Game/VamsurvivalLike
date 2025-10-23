using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
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

        if (!pv.IsMine)
        {
            rigid.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void FixedUpdate()
    {
        if (!GameManager.instance.isLive)
            return;

        if (pv.IsMine)
        {
            // 로컬 플레이어의 움직임
            Vector2 nextVec = inputVec * speed * Time.fixedDeltaTime;
            rigid.MovePosition(rigid.position + nextVec);
        }
    }

    private void LateUpdate()
    {
        if (!GameManager.instance.isLive || !pv.IsMine)
            return;

        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0)
        {
            sprite.flipX = inputVec.x < 0;
        }
    }

    private void OnMove(InputValue value)
    {
        if (!GameManager.instance.isLive || !pv.IsMine)
            return;

        inputVec = value.Get<Vector2>();
    }

    [PunRPC]
    public void InitPlayer(int characterId)
    {
        anim.runtimeAnimatorController = animCon[characterId];

        if (pv.IsMine)
        {
            speed *= Character.Speed;
            transform.GetChild(1).gameObject.SetActive(true); // Arae 활성화
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.instance.isLive || !pv.IsMine)
            return;

        if (!collision.gameObject.CompareTag("Enemy"))
            return;

        GameManager.instance.health -= Time.deltaTime * 10;

        if (GameManager.instance.health <= 0)
        {
            for (int index = 2; index < transform.childCount; index++)
            {
                transform.GetChild(index).gameObject.SetActive(false);
            }
            anim.SetTrigger("Dead");
            GameManager.instance.GameOver();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내(Local)가 데이터를 보낼 때
            // 1. 내 애니메이터의 "Speed" 파라미터 값을 보냄
            stream.SendNext(anim.GetFloat("Speed"));
            // 2. 내 SpriteRenderer의 flipX 상태(bool)를 보냄
            stream.SendNext(sprite.flipX);
        }
        else
        {
            // 남(Remote)의 데이터를 받을 때
            // 1. "Speed" 파라미터 값을 받아서 내 애니메이터에 적용
            float remoteSpeed = (float)stream.ReceiveNext();
            anim.SetFloat("Speed", remoteSpeed);

            // 2. flipX 상태를 받아서 내 SpriteRenderer에 적용
            bool remoteFlipX = (bool)stream.ReceiveNext();
            sprite.flipX = remoteFlipX;
        }
    }
}
