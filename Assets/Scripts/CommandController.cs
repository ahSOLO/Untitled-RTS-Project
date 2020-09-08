using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class CommandController : MonoBehaviour
{
    // Selection Variables
    public List<GameObject> selectedObjects;
    bool isSelecting = false;
    bool boxSelected;

    // Movement Variables
    List<Vector3> targetPositionList;
    float widthTotal = 0;
    float widthAverage = 0;
    [SerializeField] float formationSeparation = 0;

    // General Variables
    Vector3 mousePosition1;
    public static CommandController cC; // Singleton Variable

    // Initialization
    void Start()
    {
        selectedObjects = new List<GameObject>();
        // Initialize Singleton
        if (cC == null) cC = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        switch (GameManager.gM.gameState)
        {
            case GameManager.GameState.GAME:
                switch (GameManager.gM.orderState)
                {
                    case GameManager.OrderState.DEFAULT:
                        Selection();
                        if (Input.GetMouseButtonDown(1)) DefaultRClick();
                        break;
                        // Insert other order states here
                }
                break;
        }
    }

    void OnGUI()
    {
        if (isSelecting)
        {
            // Draw a selection rect based on mouse position
            var rect = Utils.GetScreenRect(mousePosition1, Input.mousePosition);
            Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Utils.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }
    }

    void Selection()
    {
        // If we press the left mouse button, save mouse location and begin selection
        if (Input.GetMouseButtonDown(0))
        {
            isSelecting = true;
            mousePosition1 = Input.mousePosition;
            boxSelected = false; // This variable needs to be reset in order for single click selection to activate
        }
        // If we let go of the left mouse button, end selection and list selected objects within the selection box
        if (Input.GetMouseButtonUp(0))
        {
            // If left shift is not being held, clear the previous selection
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                foreach (var selectedObject in selectedObjects)
                {
                    selectedObject.GetComponent<ObjectInfo>().isSelected = false;
                    // Remove selection circle
                    selectedObject.GetComponent<ObjectInfo>().selectionCircle.SetActive(false);
                }
                selectedObjects.Clear();
            }
            foreach (var selectableObject in FindObjectsOfType<ObjectInfo>())
            {
                if (IsWithinSelectionBounds(selectableObject.gameObject))
                {
                    var _objectInfo = selectableObject.GetComponent<ObjectInfo>();
                    if (_objectInfo.isSelected == false)
                    {
                        selectableObject.GetComponent<ObjectInfo>().isSelected = true;
                        selectedObjects.Add(selectableObject.gameObject);
                    }
                    boxSelected = true;
                }
            }
            isSelecting = false;

            // If no objects were selected within the selection box, check for single click selection by casting a ray from camera to mouse position
            if (boxSelected == false)
            {
                Ray _ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit _hit;

                if (Physics.Raycast(_ray, out _hit, 100))
                {
                    if (_hit.collider.tag == "Selectable" )
                    {
                        var _selectedObjectInfo = _hit.collider.gameObject.GetComponent<ObjectInfo>();
                        if (_selectedObjectInfo.isSelected == false)
                        {
                            _selectedObjectInfo.isSelected = true;
                            selectedObjects.Add(_selectedObjectInfo.gameObject);
                        }
                        // If shift is being held, de-select the unit instead
                        else if (Input.GetKey(KeyCode.LeftShift) == true && _selectedObjectInfo.isSelected == true)
                        {
                            _selectedObjectInfo.isSelected = false;
                            selectedObjects.Remove(_selectedObjectInfo.gameObject);
                            // Remove selection circle
                            _selectedObjectInfo.selectionCircle.SetActive(false);
                        }
                    }
                }
            }

            // Determine if the selected objects contain a mixture of units, buildings, and other types of objects
            List<GameObject> _selectedObjects = new List<GameObject>(selectedObjects);
            bool _selectedUnit = false;
            bool _selectedBldg = false;

            foreach (var selectedObject in _selectedObjects)
            {
                ObjectInfo.ObjectType _selectedObjectType = selectedObject.GetComponent<ObjectInfo>().objectType;
                if (_selectedObjectType == ObjectInfo.ObjectType.UNIT) _selectedUnit = true;
                if (_selectedObjectType == ObjectInfo.ObjectType.BUILDING) _selectedBldg = true;
            }

            // If units were selected, de-select all other object types
            if (_selectedUnit) foreach (var selectedObject in _selectedObjects)
            {
                if (selectedObject.GetComponent<ObjectInfo>().objectType != ObjectInfo.ObjectType.UNIT)
                {
                    selectedObject.GetComponent<ObjectInfo>().isSelected = false;
                    _selectedBldg = false;
                    selectedObjects.Remove(selectedObject);
                }
            }
            // Otherwise, if buildings were selected, de-select all other object types
            else if (_selectedBldg) foreach (var selectedObject in _selectedObjects)
            {
                if (selectedObject.GetComponent<ObjectInfo>().objectType != ObjectInfo.ObjectType.BUILDING)
                {
                    selectedObject.GetComponent<ObjectInfo>().isSelected = false;
                    selectedObjects.Remove(selectedObject);
                }
            }

            // Draw selection circles
            foreach (var selectedObject in selectedObjects)
            {
                selectedObject.GetComponent<ObjectInfo>().selectionCircle.SetActive(true);
            }

        }
    }

    // Detect whether an object is within the selection bounds created by the selection box
    public bool IsWithinSelectionBounds(GameObject gameObject)
    {
        if (!isSelecting) return false;

        var _camera = Camera.main;
        var _viewportBounds = Utils.GetViewportBounds(_camera, mousePosition1, Input.mousePosition);
        var _col = gameObject.GetComponent<Collider>();

        return _viewportBounds.Contains(_camera.WorldToViewportPoint(_col.bounds.center - (_col.bounds.extents / 2))) ||
            _viewportBounds.Contains(_camera.WorldToViewportPoint(_col.bounds.center + (_col.bounds.extents / 2))) ||
            _viewportBounds.Contains(_camera.WorldToViewportPoint(gameObject.transform.position));
    }

    // Default action selected objects take when right clicking
    void DefaultRClick()
    {
        Ray _ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit _hit;

        if (Physics.Raycast(_ray, out _hit, 100) && selectedObjects.Count != 0)
        {
            if (_hit.collider.tag == "Ground") MoveTo(_hit);
            else if (_hit.collider.tag == "Unit" || _hit.collider.tag == "Building") Interact(_hit);
        }
    }

    // Move command
    void MoveTo(RaycastHit _hit)
    {
        // If multiple units are selected, set individual unit destinations according to points on a circular grid
        if (selectedObjects.Count > 1)
        {
            // The distance between each point on the circular grid is determined by taking the average width of the selected units
            widthTotal = 0;
            foreach (var selectedObject in selectedObjects)
            {
                widthTotal += Mathf.Max(selectedObject.GetComponent<Collider>().bounds.size.x, selectedObject.GetComponent<Collider>().bounds.size.z);
            }
            widthAverage = widthTotal / selectedObjects.Count;
            // The circular grid is composed of three rings with an increasing number of destination points on each ring
            targetPositionList = GetPositionListAround(_hit.point, new float[] { 0f, widthAverage + formationSeparation, widthAverage * 2 + formationSeparation, widthAverage * 3 + formationSeparation}, new int[] { 1, 5, 10, 20 });
            int _targetPositionIndex = 0;

            foreach (var selectedObject in selectedObjects)
            {
                selectedObject.GetComponent<NavMeshAgent>().destination = targetPositionList[_targetPositionIndex];
                _targetPositionIndex += 1;
                if (_targetPositionIndex == targetPositionList.Count) _targetPositionIndex = 0;
            }
        }
        else selectedObjects[0].GetComponent<NavMeshAgent>().destination = _hit.point;
    }

    private List<Vector3> GetPositionListAround(Vector3 startPosition, float[] ringDistanceArray, int[] ringPositionCountArray)
    {
        List<Vector3> positionList = new List<Vector3>();
        positionList.Add(startPosition);
        for (int i = 0; i < ringDistanceArray.Length; i++)
        {
            positionList.AddRange(GetPositionListAround(startPosition, ringDistanceArray[i], ringPositionCountArray[i]));
        }
        return positionList;
    }

    private List<Vector3> GetPositionListAround(Vector3 startPosition, float distance, int positionCount)
    {
        List<Vector3> _positionList = new List<Vector3>();
        for (int i = 0; i < positionCount; i++)
        {
            float _angle = i * (360f / positionCount);
            Vector3 dir = Utils.ApplyRotationToVector(new Vector3(1, 0), _angle);
            Vector3 position = startPosition + dir * distance;
            _positionList.Add(position);
        }
        return _positionList;
    }

    // Interact command
    void Interact(RaycastHit _hit)
    {
        foreach (var _selectedObject in selectedObjects)
        {
            Debug.Log(_selectedObject.GetComponent<ObjectInfo>().name + " Interacts with " + _hit.collider.GetComponent<ObjectInfo>().name);
        }
    }
}
