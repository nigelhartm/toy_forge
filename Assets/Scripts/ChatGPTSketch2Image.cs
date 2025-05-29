using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using OVRSimpleJSON;
using System.Collections.Generic;
using System.IO;
using GLTFast;


public class ChatGPTSketch2Image : MonoBehaviour
{
    public Whiteboard whiteboard; // Reference to the Whiteboard script
    private string openAIApiKey = "PUT_HERE_YOUR_OPENAI_KEY";
    private string MESHY_KEY = "PUT_HERE_YOUR_MESHY_KEY";
    private Texture2D output_texture;
    [SerializeField] GameObject outputCanvas;


    // Functionalities to cheat a bit for prototype
    public TMPro.TextMeshProUGUI statusText;
    public MoveObjectToTarget moveObjectToTarget;
    public Transform boxCenter;
    public GameObject ExampleToy;

    private void Start()
    {
        initOutputTexture();
        //StartCoroutine(ReceiveModel("01971314-3899-7add-a831-a3a6227716d9"));
    }

    public void initOutputTexture()
    {
        var r = outputCanvas.gameObject.GetComponent<Renderer>();
        output_texture = new Texture2D((int) whiteboard.textureSize.x, (int) whiteboard.textureSize.y, TextureFormat.RGB24, false);
        r.material.mainTexture = output_texture;
    }

    public void sketch2Image() {
        statusText.SetText("Create Preview ...");
        print("try...");
        StartCoroutine(SendBase64ToOpenAI());
    }

    public void image2Model() {
        statusText.SetText("Create Toy ...");
        print("try...");
        StartCoroutine(SendOutputToMeshy());
    }


    IEnumerator ReceiveModel(string resultID)
    {
        Debug.Log("Preparing to receive model..");
        UnityWebRequest request = new UnityWebRequest("https://api.meshy.ai/openapi/v1/image-to-3d/" + resultID, "GET");

        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Authorization", "Bearer " + MESHY_KEY);

        Debug.Log("Sending...");

        yield return request.SendWebRequest();

        Debug.Log("Done.");
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Meshy Error: " + request.error + "\n" + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Model received!");
            string json = request.downloadHandler.text;
            Debug.Log("Response: " + json);
            ModelData modelData = JsonUtility.FromJson<ModelData>(json);
            Debug.Log("OBJ URL: " + modelData.model_urls.glb);

            if (modelData.status == "SUCCEEDED")
            {
                Debug.Log("Model received! OBJ URL: " + modelData.model_urls.glb);
                loadGLB(modelData.model_urls.glb);
            }
            else
            {
                Debug.Log("Model not ready yet. Status: " + modelData.status + ". Retrying in 15 seconds...");
                yield return new WaitForSeconds(15f);
                StartCoroutine(ReceiveModel(resultID));  // Recursively restart coroutine
            }
        }

    }

    async void loadGLB(string Url)
    {
        var gltf = new GLTFast.GltfImport();
        var settings = new ImportSettings
        {
            GenerateMipMaps = true,
            AnisotropicFilterLevel = 3,
            NodeNameMethod = NameImportMethod.OriginalUnique
        };
        var success = await gltf.Load(Url, settings);
        if (success)
        {
            var gameObject = new GameObject("glTF");
            gameObject.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f); // Scale down the model
            ExampleToy.transform.position = boxCenter.position; // Center the ExampleToy at the box center
            gameObject.transform.position = ExampleToy.transform.position; // Center the model at the box center
            gameObject.transform.parent = ExampleToy.transform; // Set the parent to ExampleToy
            await gltf.InstantiateMainSceneAsync(gameObject.transform);
            statusText.SetText("Finished!");
            moveObjectToTarget.toggleOpen();
        }
        else
        {
            Debug.LogError("Loading glTF failed!");
        }
    }


    [System.Serializable]
    public class ModelUrls
    {
        public string glb;
        public string fbx;
        public string usdz;
        public string obj;
    }

    [System.Serializable]
    public class ModelData
    {
        public string id;
        public ModelUrls model_urls;
        public string status;
    }

    IEnumerator SendOutputToMeshy() {
        Debug.Log("Preparing to send..");
        byte[] imageBytes = output_texture.EncodeToPNG();
        string base64Image = Convert.ToBase64String(imageBytes);
        string jsonPayload = $@"{{
            ""image_url"": ""data:image/png;base64,{base64Image}"",
            ""enable_pbr"": true,
            ""should_remesh"": true,
            ""should_texture"": true
        }}";

        UnityWebRequest request = new UnityWebRequest("https://api.meshy.ai/openapi/v1/image-to-3d", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + MESHY_KEY);

        Debug.Log("Sending...");
        yield return request.SendWebRequest();

        Debug.Log("DOne.");
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Meshy Error: " + request.error + "\n" + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Model received!");
            string json = request.downloadHandler.text;
            Debug.Log("Response: " + json);
            ResultResponse response = JsonUtility.FromJson<ResultResponse>(json);
            string resultId = response.result;
            Debug.Log("Result ID: " + resultId);
            StartCoroutine(ReceiveModel(resultId));
        }
    }

    [System.Serializable]
    public class ResultResponse
    {
        public string result;
    }

    IEnumerator SendBase64ToOpenAI()
    {
        Debug.Log("Preparing to send...");
        // Convert Texture2D to PNG and then to base64
        Texture2D inputTexture = whiteboard.texture;
        byte[] imageBytes = inputTexture.EncodeToPNG();
        string base64Image = Convert.ToBase64String(imageBytes);
        string jsonPayload = $@"{{
            ""model"" : ""gpt-4.1"",
            ""tools"": [{{
                ""type"": ""image_generation"",
                ""model"": ""gpt-image-1""
            }}],
            ""input"" : [
                {{
                ""role"" : ""user"",
                ""content"" : [
                    {{
                        ""type"" : ""input_text"",
                        ""text"" : ""Create out of the attached Sketch a image of a toy.""
                    }},
                    {{
                        ""type"" : ""input_image"",
                        ""image_url"" : ""data:image/png;base64,{base64Image}""
                    }}
            ]
        }}]}}";

        UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/responses", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        Debug.Log("Sending...");
        yield return request.SendWebRequest();

        Debug.Log("DOne.");
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("OpenAI Error: " + request.error + "\n" + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Image received!");
            string json = request.downloadHandler.text;
            Debug.Log("Response: " + json);
            string base64 = JsonUtility.FromJson<OpenAIResponse>(json).output[0].result;
            byte[] imageData = System.Convert.FromBase64String(base64);
            output_texture.LoadImage(imageData);

            // automatically call 3d model
            image2Model();
        }
    }
    [System.Serializable]
    public class OpenAIImageData
    {
        public string result;
    }

    [System.Serializable]
    public class OpenAIResponse
    {
        public OpenAIImageData[] output;
    }
    IEnumerator LoadAndImportOBJFromURL(string objUrl)
    {
        Debug.Log("Downloading OBJ from: " + objUrl);
        UnityWebRequest www = UnityWebRequest.Get(objUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download OBJ: " + www.error);
        }
        else
        {
            string objText = www.downloadHandler.text;
            GameObject model = ImportFromText(objText);
            model.transform.position = Vector3.zero; // adjust position if needed
            Debug.Log("OBJ model imported successfully!");
        }
    }

    public GameObject ImportFromText(string objText)
    {
        Debug.Log("import start");

        string[] objLines = objText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        List<Vector2> finalUVs = new List<Vector2>();
        List<Vector3> finalVerts = new List<Vector3>();

        foreach (string line in objLines)
        {
            string[] parts = line.Trim().Split(' ');
            if (parts.Length < 1) continue;

            switch (parts[0])
            {
                case "v":
                    vertices.Add(ConvertBlenderToUnity(ParseVector3(parts)));
                    break;

                case "vt":
                    uvs.Add(ParseVector2(parts));
                    break;

                case "vn":
                    normals.Add(ConvertBlenderToUnity(ParseVector3(parts)).normalized);
                    break;

                case "f":
                    for (int i = 1; i < 4; i++)
                    {
                        string[] subParts = parts[i].Split('/');
                        int vIndex = int.Parse(subParts[0]) - 1;
                        int vtIndex = int.Parse(subParts[1]) - 1;

                        finalVerts.Add(vertices[vIndex]);
                        finalUVs.Add(uvs[vtIndex]);
                        triangles.Add(finalVerts.Count - 1);
                    }
                    break;
            }
        }

        // Create material
        Material currentMaterial = new Material(Shader.Find("Unlit/Color"));
        currentMaterial.color = Color.gray;

        // Create mesh
        Mesh mesh = new Mesh();
        mesh.name = "ImportedMesh";
        mesh.vertices = finalVerts.ToArray();
        mesh.uv = finalUVs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // Create GameObject
        GameObject objGO = new GameObject("ImportedOBJ");
        MeshFilter mf = objGO.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = objGO.AddComponent<MeshRenderer>();
        mr.material = currentMaterial;

        return objGO;
    }


    private static Vector3 ConvertBlenderToUnity(Vector3 v)
    {
        float scaleFix = 3.0f; // or 2.95f if precision needed
        return new Vector3(
            v.x * scaleFix,
            v.y * scaleFix,     // Blender Z  Unity Y
            v.z * scaleFix     // Blender Y  Unity -Z
        );
    }

    private static Vector3 ParseVector3(string[] parts)
    {
        return new Vector3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3]));
    }

    private static Vector2 ParseVector2(string[] parts)
    {
        return new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2]));
    }

    private static float ParseFloat(string s)
    {
        return float.Parse(s.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
    }

}
