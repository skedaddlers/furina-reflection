using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Telegraph : MonoBehaviour
{
    public enum TelegraphShape
    {
        Cone,
        Circle,
        Rectangle
    }

    [Header("Shape")]
    public TelegraphShape shape = TelegraphShape.Cone;

    [Header("Cone")]
    [Range(1f, 360f)] public float angle = 45f;
    public float radius = 5f;
    public int segments = 30;

    [Header("Circle")]
    public float circleRadius = 5f;
    public int circleSegments = 30;

    [Header("Rectangle")]
    public float rectangleWidth = 3f;
    public float rectangleLength = 5f;

    [Header("Pulse")]
    public bool pulse = true;
    public float pulseSpeed = 10f;
    public float pulseAmount = 0.05f;

    [Header("Fade In")]
    public float telegraphDuration = 1f;
    [Range(0f, 1f)] public float startAlpha = 0.15f;
    [Range(0f, 1f)] public float maxAlpha = 0.7f;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private float timer = 0f;
    private bool fading = false;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        GenerateMesh();
    }

    void Update()
    {
        if (pulse)
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
        }

        if (fading)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / telegraphDuration);
            float currentAlpha = Mathf.Lerp(startAlpha, maxAlpha, t);

            SetAlpha(currentAlpha);

            if (timer >= telegraphDuration)
                fading = false;
        }
    }
    

    void OnValidate()
    {
        radius = Mathf.Max(0f, radius);
        circleRadius = Mathf.Max(0f, circleRadius);
        rectangleWidth = Mathf.Max(0f, rectangleWidth);
        rectangleLength = Mathf.Max(0f, rectangleLength);
        segments = Mathf.Max(3, segments);
        circleSegments = Mathf.Max(3, circleSegments);

        if (!Application.isPlaying)
            GenerateMesh();
    }

    public void GenerateMesh()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "TelegraphMesh";
        }

        mesh.Clear();

        switch (shape)
        {
            case TelegraphShape.Cone:
                BuildConeMesh();
                break;
            case TelegraphShape.Circle:
                BuildCircleMesh();
                break;
            case TelegraphShape.Rectangle:
                BuildRectangleMesh();
                break;
        }
    }

    public void SetShape(TelegraphShape telegraphShape)
    {
        shape = telegraphShape;
        GenerateMesh();
    }

    public void ConfigureCone(float newRadius, float newAngle, int newSegments, float duration = 1f)
    {
        shape = TelegraphShape.Cone;
        radius = Mathf.Max(0f, newRadius);
        angle = Mathf.Clamp(newAngle, 1f, 360f);
        segments = Mathf.Max(3, newSegments);
        telegraphDuration = duration;
        fading = true;
        timer = 0f;
        GenerateMesh();
    }

    public void ConfigureCircle(float newRadius, int newSegments, float duration = 1f)
    {
        shape = TelegraphShape.Circle;
        circleRadius = Mathf.Max(0f, newRadius);
        circleSegments = Mathf.Max(3, newSegments);
        telegraphDuration = duration;
        fading = true;
        timer = 0f;
        GenerateMesh();
    }

    public void ConfigureRectangle(float width, float length, float duration = 1f)
    {
        shape = TelegraphShape.Rectangle;
        rectangleWidth = Mathf.Max(0f, width);
        rectangleLength = Mathf.Max(0f, length);
        telegraphDuration = duration;
        fading = true;
        timer = 0f;
        GenerateMesh();
    }

    private void BuildConeMesh()
    {
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -halfAngle + (angle * Mathf.Deg2Rad * i / segments);
            float x = Mathf.Sin(currentAngle) * radius;
            float z = Mathf.Cos(currentAngle) * radius;
            vertices[i + 1] = new Vector3(x, 0f, z);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        ApplyMesh(vertices, triangles);
    }

    private void BuildCircleMesh()
    {
        Vector3[] vertices = new Vector3[circleSegments + 2];
        int[] triangles = new int[circleSegments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= circleSegments; i++)
        {
            float currentAngle = 2f * Mathf.PI * i / circleSegments;
            float x = Mathf.Sin(currentAngle) * circleRadius;
            float z = Mathf.Cos(currentAngle) * circleRadius;
            vertices[i + 1] = new Vector3(x, 0f, z);
        }

        for (int i = 0; i < circleSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        ApplyMesh(vertices, triangles);
    }

    private void BuildRectangleMesh()
    {
        float halfWidth = rectangleWidth * 0.5f;

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-halfWidth, 0f, 0f),
            new Vector3(halfWidth, 0f, 0f),
            new Vector3(-halfWidth, 0f, rectangleLength),
            new Vector3(halfWidth, 0f, rectangleLength)
        };

        int[] triangles = new int[6]
        {
            0, 2, 1,
            1, 2, 3
        };

        ApplyMesh(vertices, triangles);
    }

    private void ApplyMesh(Vector3[] vertices, int[] triangles)
    {
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }

    public void SetAlpha(float alpha)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        Color c = meshRenderer.material.color;
        c.a = alpha;
        meshRenderer.material.color = c;
    }
}
