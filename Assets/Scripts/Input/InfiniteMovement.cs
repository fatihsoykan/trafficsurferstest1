using UnityEngine;


public class InfiniteMovement : MonoBehaviour
{
    public Vector3 MoveSpeed = new Vector3(0, 0, -5f);
    Camera cam;
    public Vector3 ResetDistance = new Vector3(0, 0, 60f);
    public float DistanceBias = -3f; 
    //kameranýn z'sini geçtiði anda yaptýðýmýzda arada boþluk oluyordu.
    //Bu yüzden kameranýn z'sini distancebias kadar geçtiðinde çalýþsýn dedik




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
