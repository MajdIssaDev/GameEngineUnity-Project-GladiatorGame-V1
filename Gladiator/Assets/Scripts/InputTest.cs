using UnityEngine;

public class InputTest : MonoBehaviour
{
    void Update()
    {
        if (Input.anyKeyDown)
        {
            Debug.Log("I detected a key press: " + Input.inputString);
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("SPACEBAR DETECTED SUCCESSFULLY!");
        }
    }
}