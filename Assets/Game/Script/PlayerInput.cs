using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Script that manages all input data for player

public class PlayerInput : MonoBehaviour
{
    public float HorizontalInput;
    public float VerticalInput;

    // Update is called once per frame
    void Update()
    {
        //Always put input in here
        HorizontalInput = Input.GetAxisRaw("Horizontal");
        VerticalInput = Input.GetAxisRaw("Vertical");
    }

    //gets called when script is disabled
    //We disable input when character is uncontrollable
    private void OnDisable()
    {
        HorizontalInput = 0;
        VerticalInput = 0;
    }
}
