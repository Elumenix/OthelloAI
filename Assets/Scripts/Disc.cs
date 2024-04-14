using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disc : MonoBehaviour
{
    [SerializeField] private Player up;
    private Animator animator;
    
    // Start is called before the first frame update
    private void Start()
    {
        animator = GetComponent<Animator>();
    }


    public void Flip()
    {
        // Gameobject might have been destroyed if game is restarting
        if (animator == null) 
        {
            return;
        }
        
        if (up == Player.Black)
        {
            animator.Play("BlackToWhite");
            up = Player.White;
        }
        else
        {
            animator.Play("WhiteToBlack");
            up = Player.Black;
        }
    }

    public void Twitch()
    {
        animator.Play("TwitchDisc");
    }
}
