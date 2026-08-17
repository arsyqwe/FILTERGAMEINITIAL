using UnityEngine;
using UnityEngine.Android;

public class CameraPermission : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }
#endif
    }
}