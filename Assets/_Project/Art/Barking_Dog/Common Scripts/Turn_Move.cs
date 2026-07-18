using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [외부 에셋 코드] 설정된 속도로 오브젝트를 계속 회전시키고 이동시킵니다.
/// World 설정에 따라 월드 좌표 또는 오브젝트 로컬 좌표를 사용합니다.
/// </summary>
public class Turn_Move : MonoBehaviour {
	public int TurnX;
	public int TurnY;
	public int TurnZ;

	public int MoveX;
	public int MoveY;
	public int MoveZ;

	public bool World;

	// Unity가 첫 프레임 전에 호출합니다. 현재 스크립트에는 별도 초기화 작업이 없습니다.
	void Start () {
		
	}
	
	// Unity가 매 프레임 호출하며 설정된 이동량과 회전량을 Transform에 반영합니다.
	void Update () {
		if (World == true) {
			transform.Rotate(TurnX * Time.deltaTime,TurnY * Time.deltaTime,TurnZ * Time.deltaTime, Space.World);
			transform.Translate(MoveX * Time.deltaTime, MoveY * Time.deltaTime, MoveZ * Time.deltaTime, Space.World);
		}else{
			transform.Rotate(TurnX * Time.deltaTime,TurnY * Time.deltaTime,TurnZ * Time.deltaTime, Space.Self);
			transform.Translate(MoveX * Time.deltaTime, MoveY * Time.deltaTime, MoveZ * Time.deltaTime, Space.Self);
		}
	}
}
