using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectInfo : MonoBehaviour
{
    public string name;
    public enum ObjectType { UNIT, BUILDING, RESOURCE, OTHER2}
    public ObjectType objectType;
    public bool isSelected = false;
    public GameObject selectionCircle = null;
    public NavMeshAgent agent = null;

    private void OnEnable()
    {
        if (objectType == ObjectType.UNIT) agent = GetComponent<NavMeshAgent>();
        selectionCircle = transform.Find("SelectionCircle").gameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if ((isSelected) && (!selectionCircle.activeSelf))
        //{
        //    selectionCircle.SetActive(true);
        //}

        //if ((isSelected == false) && (selectionCircle.activeSelf))
        //{
        //    selectionCircle.SetActive(false);
        //}
    }
}
