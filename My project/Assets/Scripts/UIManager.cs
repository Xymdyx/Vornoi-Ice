using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TMPro.TextMeshProUGUI textUI;
    public TMPro.TextMeshProUGUI upperText;
    // Start is called before the first frame update
    void Start()
    {

        textUI = transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        upperText = transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        //Debug.Log(textUI.text);
        updateText("Hello there");
    }

    // update bottom-left text
    public void updateText( string msg )
    {
        textUI.text = msg;
    }

    public void updateUpperText( string msg)
    {
        upperText.text = msg;
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
