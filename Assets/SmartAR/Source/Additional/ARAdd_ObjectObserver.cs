using UnityEngine;

namespace SmartAR
{
    [AddComponentMenu("Matej Vanco/Smart AR/AR Object Observer")]

    /// <summary>
    /// Additional object observer/ viewer system written by Matej Vanco 2020.
    /// Drag object and rotate by moving with finger on screen
    /// </summary>
    public class ARAdd_ObjectObserver : MonoBehaviour
    {
        [Tooltip("Leave the field empty (mainCamera) if you would like to get the mainCamera from the scene")]
        public Camera mainCam;
        [Space]
        private Vector3 prevPos;

        public bool enableRotation = true;
        public bool enableMovement = true;
        [Space]
        public float rotationSpeed = 4f;
        public float smoothFactor = 0.24f;
        [Space]
        public Vector2 offsetPosition = new Vector2(0.05f,0.05f);
        public float depthPosition = 2.0f;

        public void Awake()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
                if(mainCam == null)
                    Debug.LogError("AR Observer: There is no main camera!");
            }
        }

        /// <summary>
        /// Enable/Disable object-observation script
        /// </summary>
        /// <param name="b">value</param>
        public void EnableDisableObservatory(bool b)
        {
            enableRotation = b;
            enableMovement = b;
        }

        private void Update()
        {
            if (enableMovement)
                transform.position = Vector3.Lerp(transform.position, mainCam.transform.position + (mainCam.transform.forward * depthPosition) + (mainCam.transform.right * offsetPosition.x) + (-mainCam.transform.up * offsetPosition.y), smoothFactor);

            if (!enableRotation)
                return;
            if (Input.touchCount != 1)
                return;

            Touch touch = Input.GetTouch(0);
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            Vector3 tPos = touch.deltaPosition;

            if (touch.phase == TouchPhase.Began)
                prevPos = tPos;

            Vector3 tcRot = (tPos - prevPos);
            Vector3 fRot = transform.InverseTransformDirection(mainCam.transform.TransformDirection(new Vector3(tcRot.y, -tcRot.x, 0)));
            transform.Rotate(fRot * rotationSpeed * Time.deltaTime);
        }
    }
}