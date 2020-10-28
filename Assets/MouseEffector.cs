using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MouseEffector : MonoBehaviour
{
    public GameObject effect;
    public Canvas canvas;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject instantiatedEffect = Instantiate<GameObject>(effect, canvas.transform);
            RectTransform rectTransform = instantiatedEffect.GetComponent<RectTransform>();
            RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.anchoredPosition = canvas.worldCamera.ScreenToViewportPoint(Input.mousePosition) * canvasRectTransform.sizeDelta;
            Animator animator = instantiatedEffect.GetComponent<Animator>();
        }
    }
}
