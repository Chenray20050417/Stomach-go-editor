using UnityEngine;

public class AutoForwardMovement : MonoBehaviour
{
    [Header("自動前進速度")]
    public float forwardSpeed = 10f;

    void Update()
    {
        transform.Translate(
            Vector3.forward * forwardSpeed * Time.deltaTime
        );
    }
}