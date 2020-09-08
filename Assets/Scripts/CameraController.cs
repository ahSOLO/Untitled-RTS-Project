using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Panning Variables
    [SerializeField] float panSpeed;
    [SerializeField] float panSmoothness;
    [SerializeField] float panDuration;
    [SerializeField] float mousePanSpeed;
    float panDetect = 15f;
    Vector3 panDestination;
    bool panningCamera = false;
    float panCounter = 0;

    // Zoom Variables
    [SerializeField] float zoomSpeed;
    [SerializeField] float zoomSmoothness;
    [SerializeField] float zoomDuration;
    bool zoomingCamera = false;
    Vector3 minZoom = new Vector3(0, 5, -2.5f);
    Vector3 maxZoom = new Vector3(0, 15, -7.5f);
    Vector3 zoomDestination;
    float zoomCounter = 0;

    // Rotation Variables
    Transform cameraRotator;
    [SerializeField] float rotateSpeed;
    [SerializeField] float rotateSmoothness;
    [SerializeField] float rotateTime;
    [SerializeField] float resetSmoothness;
    [SerializeField] float resetTime;
    [SerializeField] private float doubleClickDelay;
    Transform rotateDestination;
    bool rotatingCamera = false;
    float rotateCounter = 0;
    float clickTimer = 0;
    bool resettingView = false;
    float resetCounter = 0;

    // General Camera Variables
    Quaternion defaultRotation;
    Vector3 defaultPosition;
    [SerializeField] float aboveGroundResp = 10f;
    public static CameraController cC;

    // Initialization
    void Start()
    {
        cameraRotator = transform.Find("Camera Rotator");
        rotateDestination = transform.Find("Rotate Destination");
        defaultRotation = cameraRotator.localRotation;
        defaultPosition = Camera.main.transform.localPosition;
        panDestination = transform.localPosition;
        // Set up Singleton
        if (cC == null) cC = this;
        else Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        switch (GameManager.gM.gameState)
        {
            case GameManager.GameState.GAME:
                PanCamera();
                ZoomCamera();
                RotateCamera(); // Camera rotation should be called after camera zoom
                SteadyAboveGround();
                break;
            case GameManager.GameState.MENU:
                break;
                // Insert other game states here
        }
    }

    // Camera panning is handled by moving an invisible player object which is a parent of the main camera. This ensures camera panning does not interfere with zoom and rotation, which utilize the camera's local transform.
    public void PanCamera()
    {
        // Get mouse position
        float _xPos = Input.mousePosition.x;
        float _yPos = Input.mousePosition.y;

        // Get panning input by multiplying X/Z axis by pan speed
        Vector3 panInput = new Vector3(Input.GetAxisRaw("Horizontal") * panSpeed * Time.deltaTime, 0, Input.GetAxisRaw("Vertical") * panSpeed * Time.deltaTime);

        // Add mouse pan to panning input
        if (_xPos > 0 && _xPos < panDetect) panInput += Vector3.left * mousePanSpeed * (Camera.main.transform.position.y / defaultPosition.y) * Time.deltaTime;
        if (_xPos < Screen.width && _xPos > Screen.width - panDetect) panInput += Vector3.right * mousePanSpeed * (Camera.main.transform.position.y / defaultPosition.y) * Time.deltaTime;
        if (_yPos > 0 && _yPos < panDetect) panInput += Vector3.back * mousePanSpeed * (Camera.main.transform.position.y / defaultPosition.y) * Time.deltaTime;
        if (_yPos < Screen.height && _yPos > Screen.height - panDetect) panInput += Vector3.forward * mousePanSpeed * (Camera.main.transform.position.y / defaultPosition.y) * Time.deltaTime;

        // If input is detected, determine camera panning destination
        if (panInput != Vector3.zero)
        {
            // Get camera facing direction and calculate camera movement in world space
            float _camHeading = Camera.main.transform.eulerAngles.y;
            Vector3 _camMovement = Quaternion.Euler(0, _camHeading, 0) * panInput;
            // Add calculated camera movement into pan destination 
            panDestination += _camMovement;
            panningCamera = true; // This bool signals that a destination has been set and panning can now occur
            panCounter = 0;
        }
        // Apply smooth camera movement through lerp for a period equal to panDuration
        if (panningCamera == true)
        {
            panCounter += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, panDestination, panSmoothness);
            if (panCounter >= panDuration)
            {
                panningCamera = false;
            }
        }
    }

    public void ZoomCamera()
    {
        // Get current zoom length which is equal to the local position of the camera transform
        Vector3 _zoomLength = Camera.main.transform.localPosition;

        // Check for input
        if (Input.GetAxisRaw("Mouse ScrollWheel") != 0)
        {
            // Checks if zoom destination is within acceptable bounds; if not, zoom destination is adjusted until it falls within the correct range.
            zoomDestination = _zoomLength - (Input.GetAxisRaw("Mouse ScrollWheel") * zoomSpeed * _zoomLength);
            while (minZoom.sqrMagnitude > zoomDestination.sqrMagnitude) zoomDestination *= 1.01f;
            while (zoomDestination.sqrMagnitude > maxZoom.sqrMagnitude) zoomDestination *= 0.99f;
            zoomingCamera = true; // This bool signals that a destination has ben set and zooming can now occur
            zoomCounter = 0;
        }
        // Apply smooth camera zoom through lerp for a period equal to zoomDuration
        if (zoomingCamera == true)
        {
            zoomCounter += Time.deltaTime;
            Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, zoomDestination, Time.deltaTime * zoomSmoothness);
            if (zoomCounter >= zoomDuration)
            {
                zoomingCamera = false;
            }
        }
    }

    public void RotateCamera()
    {
        // If middle mouse button is held down, rotate the camera around the player's z axis according to horizontal mouse movements
        if (Input.GetMouseButton(2))
        {
            rotateDestination.RotateAround(transform.position, Vector3.up, Input.GetAxisRaw("Mouse X") * Time.deltaTime * rotateSpeed);
            rotatingCamera = true;
            rotateCounter = 0;
        }

        // Advance timer for double click checks
        if (clickTimer >= 0) clickTimer -= Time.deltaTime;

        // If middle mouse button is double clicked, begin resetting camera's rotation to default rotation and position
        if (Input.GetMouseButtonDown(2))
        {
            if (clickTimer > 0 && clickTimer < doubleClickDelay)
            {
                resettingView = true;
                resetCounter = 0;
            }
            else clickTimer = doubleClickDelay;
        }

        // If resettingView variable is true, smoothly reset camera rotation to default rotation and height
        ResetView();

        if (rotatingCamera == true)
        {
            rotateCounter += Time.deltaTime;
            cameraRotator.localRotation = Quaternion.Lerp(cameraRotator.localRotation, rotateDestination.localRotation, Time.deltaTime * rotateSmoothness);
            if (rotateCounter >= rotateTime) rotatingCamera = false;
        }

    }

    private void ResetView()
    {
        // Smoothly reset camera rotation
        if (resettingView == true)
        {
            // Disable camera zoom and rotation functionality while resetting
            zoomingCamera = false;
            rotatingCamera = false;
            // Smoothly set camera transform to default rotation and position
            resetCounter += Time.deltaTime;
            rotateDestination.rotation = defaultRotation;
            Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, defaultPosition, Time.deltaTime * resetSmoothness);
            cameraRotator.localRotation = Quaternion.Slerp(cameraRotator.localRotation, defaultRotation, Time.deltaTime * resetSmoothness);
            if (resetCounter >= resetTime) resettingView = false;
        }
    }

    private void SteadyAboveGround()
    {
        Vector3 _camPos = cameraRotator.transform.position;
        float groundLevel = Terrain.activeTerrain.SampleHeight(_camPos);
        _camPos.y = Mathf.Lerp(_camPos.y, groundLevel, Time.deltaTime * aboveGroundResp); // 10 responsiveness results in a good movement
        cameraRotator.transform.position = _camPos;
    }



}
