using UnityEngine;

public class EyeBlinker : MonoBehaviour
{
    private string _blinkText = "";
    private float _textClearTime = 0f;

    public void Update()
    {
        if (!Wrapper.Instance.IsCalibrated) return;

        if (Wrapper.Instance.ConsumeLeftBlink())
        {
            MoveCharacterLeft();
        }
        else if (Wrapper.Instance.ConsumeRightBlink())
        {
            MoveCharacterRight();
        }
    }

    public void MoveCharacterLeft()
    {
        _blinkText = "Sola ";
        _textClearTime = Time.time + 1.5f;
    }

    public void MoveCharacterRight()
    {
        _blinkText = "Sağa ";
        _textClearTime = Time.time + 1.5f;
    }

    public void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.UpperRight };
        GUI.color = Color.green;

        Rect rect = new Rect(Screen.width - 350, 20, 330, 50);

        if (!Wrapper.Instance.IsCalibrated)
        {
            GUI.Label(rect, Wrapper.Instance.CalibrationText, style);
        }
        else if (Time.time < _textClearTime)
        {
            GUI.Label(rect, _blinkText, style);
        }
    }
}