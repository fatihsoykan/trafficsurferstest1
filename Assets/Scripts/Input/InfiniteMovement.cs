using UnityEngine;


public class InfiniteMovement : MonoBehaviour
{
    public Vector3 MoveSpeed = new Vector3(0, 0, -5f);
    Camera cam;
    public Vector3 ResetDistance = new Vector3(0, 0, 60f);
    public float DistanceBias = -3f; 
    //kameran�n z'sini ge�ti�i anda yapt���m�zda arada bo�luk oluyordu.
    //Bu y�zden kameran�n z'sini distancebias kadar ge�ti�inde �al��s�n dedik




    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        transform.position += MoveSpeed * Time.deltaTime;

        if (transform.position.z < cam.transform.position.z + DistanceBias)
        {
            transform.position += ResetDistance;
        }


    }




}
