using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Transform objectToFollow; // 카메라가 따라갈 기준점. player의 Followcam으로 설정한다.
    public float followSpeed = 10f; // 따라가는 속도
    public float sensitivity = 100f; // 카메라 회전 마우스 감도
    public float clampAngle = 70f; // 카메라가 위아래로 회전하는 최댓값

    private float rotX; // 마우스의 움직임을 받음
    private float rotY;

    public Transform realCamera; // 어떤 카메라가 따라갈지 정한다. Camera의 Main Camera로 설정한다.
    private Vector3 dirNormalized;
    private Vector3 finalDir; // dirNormalized에 거리를 곱해서 최종 방향을 만든다.
    public float minDistance; // 벽 등 장애물에 막혔을 때 카메라를 이 거리만큼 당긴다.
    public float maxDistance; // 일반적인 상황에서의 카메라의 거리
    public float finalDistance; // 최종적으로 계산되는 카메라와 캐릭터의 실제 거리. 이값이 실제로 적용된다.
    public float smoothness = 10f; // lerp 함수에서 쓰일 변수, 카메라 움직임이 얼마나 부드럽게 움직일지 결정하는 변수.

    // Start is called before the first frame update
    void Start()
    {   
        // 마우스의 움직임을 변수에 저장
        rotX = transform.localRotation.eulerAngles.x;
        rotY = transform.localRotation.eulerAngles.y;

        dirNormalized = realCamera.localPosition.normalized;
        finalDistance = realCamera.localPosition.magnitude;

        // 마우스 고정시키고 안보이게 하기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        // 마우스의 움직임을 변수에 저장
        rotX -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
        rotY += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;

        // 위아래(x축 회전) 회전의 최댓값을 지정해서 캐릭터가 한바퀴 돌아서 보이는 경우를 방지
        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
        transform.rotation = rot; // 카메라가 회전함
    }

    void LateUpdate(){
        // Camera오브젝트는 objectToFollow로 설정된 Player의 FollowCam을 항상 따라간다.
        // transform.position = Vector3.MoveTowards(transform.position, objectToFollow.position, followSpeed * Time.deltaTime);
        transform.position = objectToFollow.position;

        finalDir = transform.TransformPoint(dirNormalized * maxDistance);
        RaycastHit hit;

        if(Physics.Linecast(transform.position, finalDir, out hit)){//카메라와 캐릭터 사이에 광선을 쏘아서 장애물을 인식
            // 거리의 최대 최소 사이에서, 장애물과 캐릭터의 거리를 최종 거리로 만든다.
            finalDistance = Mathf.Clamp(hit.distance, minDistance, maxDistance);
        }else{
            // 장애물이 없으므로 최종 거리를 최대로 설정.
            finalDistance = maxDistance;
        }
        realCamera.localPosition = Vector3.Lerp(realCamera.localPosition, dirNormalized * finalDistance, Time.deltaTime * smoothness);
    }
}
