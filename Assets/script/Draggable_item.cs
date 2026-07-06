using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable_item : MonoBehaviour , IBeginDragHandler , IEndDragHandler , IDragHandler 
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("begin drag");
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        throw new NotImplementedException();
    }

    public void OnDrag(PointerEventData eventData)
    {
        throw new NotImplementedException();
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
