using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private new Rigidbody rigidbody;

    public Transform Camera; // Camera의 Main Camera를 받아옴
    public Transform GroundReycast; // player의 GroundRaycast를 받아옴

    public Vector3 cameraDir; // 카메라 방향 : 카메라가 캐릭터를 바라보는 방향
    public Vector3 intentedDir; // 움직이는 방향 : 카메라 방향을 정북으로 두고, WASD조작을 통해 동서남북으로 이동하려고 하는 방향. 즉, 플레이어가 의도하는 방향
    public Vector3 movingVec; // 이동 벡터 : 캐릭터가 실제로 움직이는 벡터. 최종적으로 반영되는 값. 실제 움직이는 값을 표현. 아래 3개의 벡터를 다 더한 벡터.
    public Vector3 planeMovingVec; // 높이를 무시한 이동 벡터. 걷기와 달리기에 쓰인다.
    public Vector3 upMovingVec; // 캐릭터의 위아래 이동 벡터. 점프와 하강에 쓰인다.
    public Vector3 externalMovingVec; // 지형 가속이나 이동 점프 가속 등의 추가적인 가속 요소. 조작으로 인한 캐릭터의 이동 벡터 연산 이후 이 값이 더해져서 반영된다.
    // 지형 위에 서 있거나 벽에 붙어 있을 때는 이 값은 조작으로 인한 이동 연산 이후 더해지고, 지형이나 벽에서 떨어지게 되면, 이 값이 더해진 이후 이동 연산이 이루어진다. (세부로직 아직 안정함)

    public float walkingAccerlation = 1.08f; // 걷기 가속도
    public float walkingMaxSpeed = 9f; // 걷기 최대 속력
    public float landFriction = 1.67f; // 지면에서의 마찰
    public float planeMovingSpeed; // 평면 이동 속도. 디버그용.

    public float GroundHitDistance = 1.1f; // 지면에 닿는 판정인 거리
    public float jumpingTime = 0.2f; // 점프 이후 점프키를 유지하는 동안 중력 가속도를 무시하는 시간. 점프 높이 조절에 사용
    public float jumpingTimeLeft; // jumpingTime - deltaTime 으로 남은 점프키 유지 시간 계산
    public float jumpingAccerlation = 10.5f; // 지면을 박차고 점프하는 힘
    public float fallingAccerlation = 1.5f; // 중력 가속도
    public float fallingMaxSpeed = 16f; // 최대 낙하 속력
    public int jumpTimes = 2; // 몇단 점프
    public int jumpTimesLeft; // 점프 한번 할때마다 1씩 줄어들고 땅에 착지하면 jumpTimes로 다시 초기화
    public bool JumpFirstFrame = false; // 처음 점프를 누를때 점프 가속을 받는다.
    public float groundDistance; // 땅과의 거리.

    public float groundBufferTime = 1f; // 지면 선입력 시간. 캐릭터가 낙하하는 속력에 이 값을 곱한 거리만큼 raycast해서 점프 등 지면 기술들의 선입력을 결정함.
    public bool isJumpBuffered = false; // 점프가 선입력이 되면 true. 이후 지면에 닿고 점프가 나가면 다시 false로 돌아옴.

    public float koyteTime = 1f; // 코요테 시간. 땅에 떨어진 이후에도 점프를 가능하게 해주는 시간.

    public bool isPlayerWalking;
    public bool isPlayerRunning; // 아직 미구현
    public bool isPlayerJumping; // 아직 미구현
    public bool isPlayerFalling;

    private float x; // 좌우 방향 키 입력 (A,D키)
    private float z; // 전후 방향 키 입력 (W,S키)

    void Start()
    {
        rigidbody = this.GetComponent<Rigidbody>();


        // 카메라 방향. 수평 이동을 위한 값이므로 y는 관여하지 않는다. y=0인 2차원 벡터
        cameraDir.x = transform.position.x - Camera.transform.position.x;
        cameraDir.z = transform.position.z - Camera.transform.position.z;
        cameraDir.y = 0f;

        // WASD 조작을 통한, 플레이어가 의도하는 방향. y=0인 2차원 벡터
        intentedDir = Vector3.zero;

        // 이동 방향 : 캐릭터가 실제로 움직이고 있는 방향. 게임 시작에는 캐릭터가 어떠한 가속도 받지 않으므로 0. 3차원 벡터
        planeMovingVec = Vector3.zero; // 이 값의 y는 수정하지 않는다.
        upMovingVec = Vector3.zero; // x, z는 수정하지 않는다.
        externalMovingVec = Vector3.zero;

        isPlayerFalling = false;
        isPlayerRunning = false;
        isPlayerJumping = false;
        isPlayerWalking = false;
    }

    void Update()
    {
        // 캐릭터가 조작없이 움직이는 것 방지
        rigidbody.velocity = Vector3.zero;

        // 카메라 방향, 카메라와 캐릭터의 x,z 좌표의 차이가 된다.
        cameraDir.x = Mathf.Floor((transform.position.x - Camera.transform.position.x)*1000)/1000;
        cameraDir.z = Mathf.Floor((transform.position.z - Camera.transform.position.z)*1000)/1000;
        cameraDir.Normalize();


        // 움직이는 방향
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        float degree = 0f;

        if(x==0&&z==0){degree = 0f;} // 카메라가 플레이어가 보는 방향을 북쪽이라 하면
        else if(x==0&&z==1){degree = 0f;} // 북쪽으로 이동
        else if(x==0&&z==-1){degree = 180f;} // 남쪽
        else if(x==1&&z==0){degree = 90f;} // 동쪽
        else if(x==1&&z==1){degree = 45f;} // 북동쪽
        else if(x==1&&z==-1){degree = 135f;} // 남동쪽
        else if(x==-1&&z==0){degree = -90f;} // 서쪽
        else if(x==-1&&z==1){degree = -45f;} // 북서쪽
        else if(x==-1&&z==-1){degree = -135f;} // 남서쪽

        intentedDir = Quaternion.AngleAxis(degree, Vector3.up) * cameraDir; // 카메라가 플레이어를 보는 방향을 키입력을 통해 만들어진 각도값만큼 회전시킨 방향이 움직이는 방향

        // 캐릭터가 걷는 상태 체크. WASD 입력이 있으면 걷는 상태이다.
        if(x!=0 || z!=0){
            isPlayerWalking = true;
        } else {
            isPlayerWalking = false;
        }

        // 캐릭터가 떨어지고 있는지 상태 체크. 발밑으로부터 GroundHitDistance 거리 안에 오브젝트가 없으면 떨어지는 상태이다.
        RaycastHit GroundHit;
        Physics.Raycast(GroundReycast.transform.position, -GroundReycast.transform.up, out GroundHit);
        groundDistance = GroundHit.distance;
        if(groundDistance < GroundHitDistance){
            isPlayerFalling = false;
        }else{
            isPlayerFalling = true;
        }        


        if(!isPlayerFalling && !isPlayerJumping){
            jumpTimesLeft = jumpTimes;
        }

        // 플레이어가 점프키를 누르고 있는지 체크
        if(Input.GetButtonDown("Jump")){
            if(jumpTimesLeft > 0){ // 새로운 점프 시작
                jumpTimesLeft -= 1;
                isPlayerJumping = true;
                jumpingTimeLeft = jumpingTime;
                JumpFirstFrame = true;
            }
        }
        if(Input.GetButtonUp("Jump")){
            isPlayerJumping = false;
        }

        if(Input.GetButtonDown("Shift")){
            planeMovingVec = 100f*planeMovingVec.normalized;
        }
    }

    private void FixedUpdate()
    {
        if(isPlayerWalking){// 걷기 상태일 때 가속과 감속
            if(Vector3.Magnitude(planeMovingVec) < walkingMaxSpeed){ // 평면 이동 방향이 걷기 최대 속도보다 작으면
                planeMovingVec = planeMovingVec + walkingAccerlation * intentedDir; // 벡터를 더해서 다음 이동 벡터를 만든다.
                planeMovingVec = Mathf.Min(Vector3.Magnitude(planeMovingVec), walkingMaxSpeed) * planeMovingVec.normalized; // 최대속도를 넘지는 않도록 벡터의 크기 조정
            }
            else if(Vector3.Magnitude(planeMovingVec) == walkingMaxSpeed){
                planeMovingVec = planeMovingVec + 2 * walkingAccerlation * intentedDir; // 벡터를 더해서 방향을 만들고
                planeMovingVec = planeMovingVec.normalized * (walkingMaxSpeed - 0.01f); // 최대속도를 넘지는 않도록 크기만 조정
            }
            else if(Vector3.Magnitude(planeMovingVec) > walkingMaxSpeed){
                float previousPMVMagnitude = Vector3.Magnitude(planeMovingVec); // 이전 이동 벡터의 크기
                planeMovingVec = planeMovingVec + 2 * walkingAccerlation * intentedDir; // 벡터를 더해서 방향을 만들고
                planeMovingVec = planeMovingVec.normalized * (previousPMVMagnitude - landFriction); // 크기는 (이전벡터의 크기 - 마찰) 이 된다.
                planeMovingVec = Mathf.Max(Vector3.Magnitude(planeMovingVec), (walkingMaxSpeed - 0.01f)) * planeMovingVec.normalized; // 최대속도 이하로 떨어지지는 않도록 벡터의 크기 조정
            }
            transform.forward = intentedDir;
        } else {// 가만히 있을 때 감속
            if(Vector3.Magnitude(planeMovingVec) <= walkingMaxSpeed){
                planeMovingVec = planeMovingVec.normalized * Mathf.Max(0, (Vector3.Magnitude(planeMovingVec) - landFriction * 0.66f)); // 마찰은 3분의 2만 받음
            }
            else if(Vector3.Magnitude(planeMovingVec) > walkingMaxSpeed){
                planeMovingVec = planeMovingVec.normalized * Mathf.Max(0, (Vector3.Magnitude(planeMovingVec) - landFriction)); // 최대 속도 이상이면 마찰을 그대로 받음
            }
        }

        if(isPlayerJumping){
            if(JumpFirstFrame){
                upMovingVec.y = jumpingAccerlation;
                JumpFirstFrame = false;
            }

            if(jumpingTimeLeft > 0){
                // 중력 미적용
            }else{
                // 중력 적용
                if(Mathf.Abs(upMovingVec.y) < 0.7f){
                    upMovingVec.y -= fallingAccerlation/3f; // 저속 하강. 캐릭터가 좀 더 가벼운 느낌을 줌.
                }else{
                    upMovingVec.y -= fallingAccerlation;
                }
            }
            jumpingTimeLeft -= Time.deltaTime;
        }

        if(!isPlayerJumping && isPlayerFalling){
            upMovingVec.y -= fallingAccerlation;
        }

        if(fallingMaxSpeed <= -upMovingVec.y){
            upMovingVec.y = Mathf.Min(-fallingMaxSpeed, upMovingVec.y + fallingAccerlation*4/3f);
        }


        planeMovingSpeed = Vector3.Magnitude(planeMovingVec);
        movingVec = planeMovingVec + upMovingVec + externalMovingVec; // 계산되어진 벡터들을 다 더하고

        // 땅 뚫는 버그 방지
        // Mathf.Clamp(movingVec.y , - groundDistance / Time.deltaTime, Mathf.Abs(movingVec.y));
        if(groundDistance <= 1f){
            movingVec.y = groundDistance;
            upMovingVec = Vector3.zero;
            isPlayerFalling = false;
        }

        rigidbody.MovePosition(transform.position + movingVec * Time.deltaTime); // 캐릭터에게 이동 벡터 적용
    }
}
