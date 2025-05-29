using UnityEngine;
using System.Collections;
using System.IO;

public class Whiteboard : MonoBehaviour
{
    public Texture2D texture;
    public Vector2 textureSize = new Vector2(1024, 1024);

    private void Awake()
    {
        var r = GetComponent<Renderer>();
        texture = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGB24, false);
        r.material.mainTexture = texture;
    }
    void Start()
    {
        
    }
}