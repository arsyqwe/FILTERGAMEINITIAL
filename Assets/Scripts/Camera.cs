using UnityEngine;
using UnityEngine.UI;

public class Camera : MonoBehaviour
{
    WebCamTexture webcam;
    public RawImage img;
    void Start()
    {
        webcam = new WebCamTexture();
        img.texture = webcam;
        webcam.Play();
    }
}
