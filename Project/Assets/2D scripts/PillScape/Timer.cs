using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public int minutes;
    public float seconds;
    public int restTime;
    public Player Player;
    public Spawner spawner;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        seconds += Time.deltaTime;
        if (Player.lifes == 0)
        {

        }
        if (seconds == 30)
        {
            spawner.timer = 1.2f;
        }
        if (minutes == 1 )
        {
            SceneManager.LoadScene("LevelOne");
        }
        if (seconds >= 60)
        {
            SceneManager.LoadScene("LevelOne");
            minutes += 1;
            seconds = 0;

        }
   
        FixedUpdate();
    }
    public void FixedUpdate()
    {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, (int)seconds);
        
       
    }

}
