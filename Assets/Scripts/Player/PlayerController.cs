using Photon.Pun;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 input;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D missing on " + gameObject.name, gameObject);
            enabled = false;
            return;
        }

        if (photonView == null)
        {
            Debug.LogError("PhotonView missing on " + gameObject.name, gameObject);
            enabled = false;
            return;
        }

        if (!photonView.IsMine)
        {
            enabled = false;
            rb.bodyType = RigidbodyType2D.Kinematic; 
        }
    }

    private void Update()
    {
        if (photonView == null || !photonView.IsMine) return;

        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    private void FixedUpdate()
    {
        if (photonView == null || !photonView.IsMine) return;

        rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
    }
}