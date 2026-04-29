using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Stereo180VideoPlayer : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("Path relative to StreamingAssets/, e.g. 'myvideo.mp4'")]
    public string videoFileName = "myvideo.mp4";

    [Header("Mesh")]
    [Tooltip("Assign your saved hemisphere mesh asset here")]
    public Mesh hemisphereMesh;

    [Tooltip("Radius of the hemisphere")]
    public float radius = 5f;

    [Header("Shader")]
    [Tooltip("Assign the Custom/Stereo180Video shader material here, or leave null to create one automatically")]
    public Material videoMaterial;

    private VideoPlayer _videoPlayer;
    private RenderTexture _renderTexture;

    void Start()
    {
        SetupMesh();
        SetupRenderTexture();
        SetupMaterial();
        SetupVideoPlayer();
    }

    void SetupMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();

        if (hemisphereMesh != null)
        {
            mf.mesh = hemisphereMesh;
        }
        else
        {
            // Fallback: generate a hemisphere procedurally if no mesh assigned
            mf.mesh = GenerateHemisphere(radius, 64, 32);
            Debug.LogWarning("No hemisphere mesh assigned — using procedurally generated fallback.");
        }

        // Scale to radius if using the saved asset mesh (which may be unit-scale)
        //transform.localScale = Vector3.one * radius;
    }

    void SetupRenderTexture()
    {
        // 4K SBS is common for 180 stereo; adjust to match your video resolution
        _renderTexture = new RenderTexture(4096, 2048, 0, RenderTextureFormat.ARGB32);
        _renderTexture.Create();
    }

    void SetupMaterial()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (videoMaterial == null)
        {
            Shader shader = Shader.Find("Custom/Stereo180Video");
            if (shader == null)
            {
                Debug.LogError("Custom/Stereo180Video shader not found. Make sure the shader file is in your project.");
                return;
            }
            videoMaterial = new Material(shader);
        }

        videoMaterial.mainTexture = _renderTexture;
        mr.material = videoMaterial;
    }

    void SetupVideoPlayer()
    {
        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _renderTexture;
        _videoPlayer.isLooping = true;
        _videoPlayer.playOnAwake = false;

        string fullPath = System.IO.Path.Combine(
            Application.persistentDataPath, "Voytilla_COMM103 180VR_v1_150mpbs.mp4");

        _videoPlayer.url = fullPath;
        _videoPlayer.Prepare();
        _videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    
    void OnVideoPrepared(VideoPlayer vp)
    {
        // Resize the RenderTexture to exactly match the video dimensions
        _renderTexture.Release();
        _renderTexture.width = (int)vp.width;
        _renderTexture.height = (int)vp.height;
        _renderTexture.Create();
        _videoPlayer.targetTexture = _renderTexture;
        videoMaterial.mainTexture = _renderTexture;

        vp.Play();
    }

    void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }

    // -------------------------------------------------------
    // Procedural hemisphere fallback (inverted normals)
    // -------------------------------------------------------
    static Mesh GenerateHemisphere(float r, int longSegments, int latSegments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralHemisphere";

        var vertices = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();

        // Only upper hemisphere: latitude from 0 (equator) to 90 degrees (top)
        for (int lat = 0; lat <= latSegments; lat++)
        {
            float theta = Mathf.PI * 0.5f * lat / latSegments; // 0 to PI/2
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longSegments; lon++)
            {
                float phi = Mathf.PI * lon / longSegments; // 0 to PI (180 degrees)
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                Vector3 v = new Vector3(
                    r * sinTheta * cosPhi,
                    r * cosTheta,
                    r * sinTheta * sinPhi);

                vertices.Add(v);

                // UV: u maps longitude 0-180 deg to 0-1, v maps latitude
                uvs.Add(new Vector2(
                    (float)lon / longSegments,
                    (float)lat / latSegments));
            }
        }

        // Build triangles with inverted winding for inside-facing normals
        for (int lat = 0; lat < latSegments; lat++)
        {
            for (int lon = 0; lon < longSegments; lon++)
            {
                int curr = lat * (longSegments + 1) + lon;
                int next = curr + longSegments + 1;

                // Inverted winding order vs. outward-facing sphere
                triangles.Add(curr);
                triangles.Add(curr + 1);
                triangles.Add(next);

                triangles.Add(next);
                triangles.Add(curr + 1);
                triangles.Add(next + 1);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
