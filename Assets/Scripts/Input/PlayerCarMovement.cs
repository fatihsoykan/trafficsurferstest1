using UnityEngine;

public class PlayerCarMovement : MonoBehaviour
{
    // -1 Left
    //0 middle
    // 1 right
    public int CurrentLane = 0;
    bool isCrossingBetweenLanes = false;
    public float LaneCrossingSpeed = 1.0f;
    int crossingDirection = 0;  
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isCrossingBetweenLanes)
        {
            //ortadan sola geçiþ
            if(CurrentLane == 0 && crossingDirection == -1)
            {
                transform.position -= new Vector3(LaneCrossingSpeed, 0, 0) * Time.deltaTime; 

                if (transform.position.x <= -1.67f)
                {
                    isCrossingBetweenLanes=false;
                    transform.position = new Vector3(-1.67f, transform.position.y, transform.position.z);
                    CurrentLane = -1;
                }
            }
            else
            //saðdan ortaya geçiþ         
            if (CurrentLane == 1 && crossingDirection == -1)
            {
                transform.position -= new Vector3(LaneCrossingSpeed, 0, 0) * Time.deltaTime;
                if (transform.position.x <= 0)
                {
                    isCrossingBetweenLanes = false;
                    transform.position = new Vector3(0, transform.position.y, transform.position.z);
                    CurrentLane = 0;
                }
            }
            else
            //ortadan saða geçiþ
            if (CurrentLane == 0 && crossingDirection == +1)
            {
                transform.position += new Vector3(LaneCrossingSpeed, 0, 0) * Time.deltaTime;
                if (transform.position.x >= 1.67f)
                {
                    isCrossingBetweenLanes = false;
                    transform.position = new Vector3(1.67f, transform.position.y, transform.position.z);
                    CurrentLane = 1;
                }
            }
            //soldan ortaya geçiþ
            else
                if (CurrentLane == -1 && crossingDirection == +1)
            {
                transform.position += new Vector3(LaneCrossingSpeed, 0, 0) * Time.deltaTime;
                if (transform.position.x <= 0)
                {
                    isCrossingBetweenLanes = false;
                    transform.position = new Vector3(0, transform.position.y, transform.position.z);
                    CurrentLane = 0;
                }
            }
        }
        else

        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                //switch to left
                isCrossingBetweenLanes=true;
                crossingDirection = -1;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                //switch to right
                isCrossingBetweenLanes = true;
                crossingDirection = +1;
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                //turn to a fire engine
            }
        }  
        
    }
}
