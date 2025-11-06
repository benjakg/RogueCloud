using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flour : MonoBehaviour
{
    public float rightTimer = 0;
    public float leftTimer = 0;
    public float endMovement = 3;
    public float moveSpeed = 6;
    public bool isRightPosition;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        flourMovement();
    }
    void flourMovement()
    {
        if (isRightPosition)
        {
            if (rightTimer <= endMovement)
            {
                transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
                rightTimer += Time.deltaTime;
            }
            else if (leftTimer <= endMovement)
            {
                transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
                leftTimer += Time.deltaTime;
            }
            else
            {
                rightTimer = 0f;
                leftTimer = 0f;
            }
        }
        else
        {
            if (leftTimer <= endMovement)
            {
                transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
                leftTimer += Time.deltaTime;
            }
            else if (rightTimer <= endMovement)
            {
                transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
                rightTimer += Time.deltaTime;
            }
            else
            {
                rightTimer = 0f;
                leftTimer = 0f;
            }
        }

    }
}
