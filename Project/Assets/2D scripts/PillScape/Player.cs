using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Player : MonoBehaviour
{
    public Rigidbody2D rb;
    public float moveSpeed; 
    private bool IsGrounded;
    public float jumpForce = 10f;
    public float lifes = 3f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
    }


    void Movement()
    {

        float x = Input.GetAxis("Horizontal");

        IsGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.7f, LayerMask.GetMask("Ground"));
        Debug.DrawRay(transform.position, Vector2.down * 1.7f, Color.red);

        rb.velocity = new Vector2(x * moveSpeed, rb.velocity.y);

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded)
        {
            Jump();

        }
       
    }
    public void Jump()
    {

        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);

        Debug.Log("Entro al Start");


    }
    public void getDamage()
    {
        if (lifes > 1)
        {
            lifes-=1;
        }
        else
        {
            Destroy(gameObject, 0.05f);
            SceneManager.LoadScene("LevelOne");
        }

    }
    public void insKill()
    {
        
            Destroy(gameObject, 0.05f);
            SceneManager.LoadScene("LevelOne");

    }
}
