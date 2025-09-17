using UnityEngine;
using TMPro;

public class CodeInterface : MonoBehaviour
{
    public TMP_InputField inputCode;

    void Start()
    {
        ShowCode();
    }
    public void ShowCode()
    {
        // GameObject canvas = GameObject.Find("Canvas");
        // inputCode = canvas.transform.Find("InputFieldCode").GetComponent<TMP_InputField>();
        inputCode.text = RelayCode.instance.tempCode;
    }
}
