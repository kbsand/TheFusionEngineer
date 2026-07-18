using UnityEngine;

// Daniel Flanigan, 2014
// This is a combined mouse look and camera move script.
// The cam move script is by: Francis R. Griffiths-Keam

/// <summary>
/// [외부 에셋 코드] 마우스로 카메라 시점을 회전하고 키보드로 자유 카메라를 이동시킵니다.
/// </summary>
public class MouseLook : MonoBehaviour
{
	Vector2 _mouseAbsolute;
	Vector2 _smoothMouse;
	[Space (20)]
	[Header ("Mouse Look Settings :")]
	public Vector2
		clampInDegrees = new Vector2 (360, 180);
	
	//public bool lockCursor;
	public CursorLockMode lockCursor;
	
	public Vector2 sensitivity = new Vector2 (2, 2);
	public Vector2 smoothing = new Vector2 (3, 3);
	public Vector2 targetDirection;
	public Vector2 targetCharacterDirection;
	
	// Assign this if there's a parent object controlling motion, such as a Character Controller.
	// Yaw rotation will affect this object instead of the camera if set.
	public GameObject characterBody;
	
	[Space (20)]
	[Header ("Camera Move Settings :")]
	
	public float acceleration = 1.0f;
	public float maxSpeed = 5;
	public float dampingSpeed = 0.2f;
	
	public KeyCode fwdKey = KeyCode.W;
	public KeyCode leftKey = KeyCode.A;
	public KeyCode backKey = KeyCode.S;
	public KeyCode rightKey = KeyCode.D;
	private float speedX, speedZ=0;

	// Unity가 첫 프레임 전에 카메라와 캐릭터의 기준 회전값을 저장합니다.
	void Start ()
	{
		// Set target direction to the camera's initial orientation.
		targetDirection = transform.localRotation.eulerAngles;
		
		// Set target direction for the character body to its inital state.
		if (characterBody)
			targetCharacterDirection = characterBody.transform.localRotation.eulerAngles;
	}
	
	// Unity가 매 프레임 마우스 입력을 읽어 카메라와 캐릭터 방향을 갱신합니다.
	void Update ()
	{
		// Ensure the cursor is always locked when set
		//Screen.lockCursor = lockCursor;
		Cursor.lockState = lockCursor;
		
		// Allow the script to clamp based on a desired target value.
		var targetOrientation = Quaternion.Euler (targetDirection);
		var targetCharacterOrientation = Quaternion.Euler (targetCharacterDirection);
		
		// Get raw mouse input for a cleaner reading on more sensitive mice.
		var mouseDelta = new Vector2 (Input.GetAxisRaw ("Mouse X"), Input.GetAxisRaw ("Mouse Y"));
		
		// Scale input against the sensitivity setting and multiply that against the smoothing value.
		mouseDelta = Vector2.Scale (mouseDelta, new Vector2 (sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));
		
		// Interpolate mouse movement over time to apply smoothing delta.
		_smoothMouse.x = Mathf.Lerp (_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
		_smoothMouse.y = Mathf.Lerp (_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);
		
		// Find the absolute mouse movement value from point zero.
		_mouseAbsolute += _smoothMouse;
		
		// Clamp and apply the local x value first, so as not to be affected by world transforms.
		if (clampInDegrees.x < 360)
			_mouseAbsolute.x = Mathf.Clamp (_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);
		
		var xRotation = Quaternion.AngleAxis (-_mouseAbsolute.y, targetOrientation * Vector3.right);
		transform.localRotation = xRotation;
		
		// Then clamp and apply the global y value.
		if (clampInDegrees.y < 360)
			_mouseAbsolute.y = Mathf.Clamp (_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);
		
		transform.localRotation *= targetOrientation;
		
		// If there's a character body that acts as a parent to the camera
		if (characterBody) {
			var yRotation = Quaternion.AngleAxis (_mouseAbsolute.x, characterBody.transform.up);
			characterBody.transform.localRotation = yRotation;
			characterBody.transform.localRotation *= targetCharacterOrientation;
		} else {
			var yRotation = Quaternion.AngleAxis (_mouseAbsolute.x, transform.InverseTransformDirection (Vector3.up));
			transform.localRotation *= yRotation;
		}
	}

	// Unity의 고정 프레임에서 이동 키 입력, 가속, 감속을 계산해 위치를 갱신합니다.
	void FixedUpdate(){

		if (Input.GetKey (rightKey)) {
			speedX += acceleration * Time.deltaTime;
		}
		// if 관련 게임 로직을 수행합니다.
		else if (Input.GetKey (leftKey)) {
			speedX -= acceleration * Time.deltaTime;
		}
		if (Input.GetKey (backKey)) {
			speedZ -= acceleration * Time.deltaTime;
		} else if (Input.GetKey (fwdKey)) {
			speedZ += acceleration * Time.deltaTime;
		}

		speedX = Mathf.Lerp( speedX,0,dampingSpeed * Time.deltaTime);
		speedZ = Mathf.Lerp( speedZ,0,dampingSpeed * Time.deltaTime);

		speedX = Mathf.Clamp( speedX,-maxSpeed*Time.deltaTime, maxSpeed*Time.deltaTime);
		speedZ = Mathf.Clamp( speedZ,-maxSpeed*Time.deltaTime, maxSpeed*Time.deltaTime);

		transform.position = transform.TransformPoint( new Vector3( speedX,0,speedZ) );
	}

}
