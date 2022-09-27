using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TMPro.TextMeshProUGUI textUI;
    // Start is called before the first frame update
    void Start()
    {

        textUI = transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        Debug.Log(textUI.text);
        textUI.text = "Hello there";
    }

    public void updateText( string msg )
    {
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
