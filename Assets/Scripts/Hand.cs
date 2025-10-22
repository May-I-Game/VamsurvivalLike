using UnityEngine;

public class Hand : MonoBehaviour
{
    public bool isLeft;
    public SpriteRenderer sprite;

    SpriteRenderer player;

    Vector3 rightPos = new Vector3(0.35f, -0.15f, 0);
    Vector3 rightPosReverse = new Vector3(-0.15f, -0.15f, 0);
    Quaternion leftRot = Quaternion.Euler(0, 0, -35);
    Quaternion leftRotReverse = Quaternion.Euler(0, 0, -135);

    private void Awake()
    {
        player = GetComponentsInParent<SpriteRenderer>()[1];
    }

    private void LateUpdate()
    {
        bool isReverse = player.flipX;

        if (isLeft) // 근접무기
        {
            transform.localRotation = isReverse ? leftRotReverse : leftRot;
            sprite.sortingOrder = isReverse ? 4 : 6;
        }

        else // 원거리무기
        {
            // 추적 로직 (왼손 오른손 개념이라 딱히..?)
            //if (GameManager.instance.player.scanner.nearestTarget)
            //{
            //    Vector3 targetPos = GameManager.instance.player.scanner.nearestTarget.position;
            //    Vector3 dir = targetPos - transform.position;
            //    transform.localRotation = Quaternion.FromToRotation(Vector3.right, dir);

            //    bool isRotA = transform.localRotation.eulerAngles.z > 90 && transform.localRotation.eulerAngles.z < 270;
            //    bool isRotB = transform.localRotation.eulerAngles.z < -90 && transform.localRotation.eulerAngles.z > -270;
            //    sprite.flipY = isRotA || isRotB;

            //    transform.localPosition = isReverse ? rightPosReverse : rightPos;
            //    sprite.flipX = false;
            //    sprite.sortingOrder = isReverse ? 6 : 4;
            //}

            transform.localPosition = isReverse ? rightPosReverse : rightPos;
            sprite.flipX = isReverse;
            sprite.sortingOrder = isReverse ? 6 : 4;
        }

    }
}
