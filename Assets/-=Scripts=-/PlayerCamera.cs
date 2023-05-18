using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform playerInstance;

    // Update is called once per frame
    void Update()
    {


        transform.position = playerInstance.position;
        transform.rotation = playerInstance.rotation;

    }
}
