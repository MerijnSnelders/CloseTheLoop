using System;
using UnityEngine;

public class Pressuredoor : MonoBehaviour
{
    [Header("Settings")]
    public Transform doorObject; 
    public float openHeight = 4f; 
    public float speed = 5f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 targetPos;

    void Start()
    {
        
        closedPos = doorObject.position;
       
        openPos = closedPos + Vector3.up * openHeight;
        
        targetPos = closedPos;
    }

    void Update()
    {
       
        doorObject.position = Vector3.MoveTowards(doorObject.position, targetPos, speed * Time.deltaTime);
    }

    
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            Console.WriteLine("here");
            targetPos = openPos;
        }
    }

  
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            targetPos = closedPos;
        }
    }
}
