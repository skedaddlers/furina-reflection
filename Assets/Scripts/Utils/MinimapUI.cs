using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapUI : MonoBehaviour
{
    [Header("Minimap Settings")]
    public float minimapRadius = 100f;
    public Vector2 minimapPosition = new Vector2(-120, -120); // Offset from top-right corner
    
    [Header("Room Icons")]
    public GameObject roomIconPrefab;
    public float iconSize = 20f;
    public float iconSpacing = 40f;
    
    [Header("Colors")]
    public Color startRoomColor = Color.green;
    public Color normalRoomColor = Color.gray;
    public Color shopRoomColor = Color.yellow;
    public Color bossRoomColor = Color.red;
    public Color currentRoomColor = Color.white;
    public Color visitedRoomColor = Color.white;
    public Color unvisitedRoomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color connectionColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    [Header("Player Indicator")]
    public GameObject playerIndicatorPrefab;
    public float playerIconSize = 10f;
    public float playerBlinkSpeed = 2f;
    
    [Header("References")]
    public RectTransform minimapContainer;
    public Image minimapBackground;
    public Image minimapMask;
    public Transform roomIconContainer;
    public Transform connectionLineContainer;
    [Header("Player Facing Cone")]
    public float coneRadius = 32f;                 // panjang kerucut di minimap
    public float coneAngleDeg = 70f;               // bukaan kerucut
    public Color coneColor = new Color(1, 1, 1, 0.85f);
    [Header("Orientation Source")]
    public bool useCameraAsOrientation = true;   // pakai POV kamera
    public Transform orientationSource;          // kalau mau override manual
    public float coneTurnSmoothing = 15f;        // haluskan rotasi

    
    
    private Dictionary<int, MinimapRoomIcon> roomIcons = new Dictionary<int, MinimapRoomIcon>();
    private Dictionary<string, GameObject> connectionLines = new Dictionary<string, GameObject>();
    private RectTransform playerIndicator;
    private HashSet<int> visitedRooms = new HashSet<int>();
    private int currentRoomId = -1;
    private RoomManager roomManager;
    private RectTransform playerCone;      // ← baru

    void Start()
    {
        SetupMinimapUI();
        roomManager = GameManager.Instance.roomManager;
        if (useCameraAsOrientation)
        {
            // coba ambil kamera aktif
            if (orientationSource == null && Camera.main != null)
                orientationSource = Camera.main.transform;
        }
        else
        {
            // fallback: pakai player
            if (orientationSource == null)
                orientationSource = GameManager.Instance?.roomManager?.player;
        }


        // // Wait a frame for room generation to complete
        // Invoke(nameof(InitializeMinimap), 0.1f);
    }
    
    void Update()
    {
        if (playerIndicator != null)
        {
            float s = 1f + Mathf.Sin(Time.time * playerBlinkSpeed) * 0.2f;
            playerIndicator.localScale = Vector3.one * s;
        }

        if (playerCone != null && orientationSource != null)
        {
            // POSISI: selalu sama dengan dot
            playerCone.anchoredPosition = playerIndicator.anchoredPosition;

            // ROTASI: pakai POV kamera (sudah kamu setting sebelumnya)
            Vector3 f = orientationSource.forward;
            Vector2 f2 = new Vector2(f.x, f.z);
            if (f2.sqrMagnitude > 1e-4f)
            {
                float target = Mathf.Atan2(f2.y, f2.x) * Mathf.Rad2Deg;
                float current = playerCone.localEulerAngles.z;
                float smooth = Mathf.LerpAngle(current, target, Time.deltaTime * coneTurnSmoothing);
                playerCone.localEulerAngles = new Vector3(0, 0, smooth);
            }
        }
    }

    
    void SetupMinimapUI()
    {
        // Create minimap container if not exists
        if (minimapContainer == null)
        {
            GameObject minimapObj = new GameObject("Minimap");
            minimapObj.transform.SetParent(transform);
            minimapContainer = minimapObj.AddComponent<RectTransform>();
        }
        
        // Position minimap (top-right corner)
        minimapContainer.anchorMin = new Vector2(1, 1);
        minimapContainer.anchorMax = new Vector2(1, 1);
        minimapContainer.pivot = new Vector2(1, 1);
        minimapContainer.anchoredPosition = minimapPosition;
        minimapContainer.sizeDelta = new Vector2(minimapRadius * 2, minimapRadius * 2);
        
        // Create circular background
        if (minimapBackground == null)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(minimapContainer);
            minimapBackground = bgObj.AddComponent<Image>();
            minimapBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
        }
        
        // Create circular mask
        if (minimapMask == null)
        {
            GameObject maskObj = new GameObject("Mask");
            maskObj.transform.SetParent(minimapContainer);
            minimapMask = maskObj.AddComponent<Image>();
            minimapMask.sprite = CreateCircleSprite();
            Mask mask = maskObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            RectTransform maskRect = maskObj.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.sizeDelta = Vector2.zero;
            maskRect.anchoredPosition = Vector2.zero;
        }
        
        // Create containers
        if (connectionLineContainer == null)
        {
            GameObject lineObj = new GameObject("Connections");
            lineObj.transform.SetParent(minimapMask.transform);
            connectionLineContainer = lineObj.transform;
            RectTransform lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            lineRect.sizeDelta = Vector2.zero;
            lineRect.anchoredPosition = Vector2.zero;
        }
        
        if (roomIconContainer == null)
        {
            GameObject iconObj = new GameObject("RoomIcons");
            iconObj.transform.SetParent(minimapMask.transform);
            roomIconContainer = iconObj.transform;
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;
            iconRect.anchoredPosition = Vector2.zero;
        }
        
        // Create border
        // GameObject borderObj = new GameObject("Border");
        // borderObj.transform.SetParent(minimapContainer);
        // Image border = borderObj.AddComponent<Image>();
        // border.sprite = CreateCircleOutlineSprite();
        // border.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        // border.raycastTarget = false;
        // RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        // borderRect.anchorMin = Vector2.zero;
        // borderRect.anchorMax = Vector2.one;
        // borderRect.sizeDelta = Vector2.zero;
        // borderRect.anchoredPosition = Vector2.zero;
        // borderObj.transform.SetAsLastSibling();
    }
    
    public void InitializeMinimap(RoomManager rm)
    {
        if (roomManager == null) return;
        roomManager = rm;

        var minimapData = roomManager.Layout; 
        if (minimapData == null) return;
        
        // Calculate scale to fit all rooms in minimap
        float gridWidth = minimapData.gridMaxX - minimapData.gridMinX + 1;
        float gridHeight = minimapData.gridMaxY - minimapData.gridMinY + 1;
        float maxDimension = Mathf.Max(gridWidth, gridHeight);
        float scale = (minimapRadius * 1.5f) / (maxDimension * iconSpacing);
        
        // Create room icons
        foreach (var kvp in minimapData.roomGridPositions)
        {
            int roomId = kvp.Key;
            Vector2Int gridPos = kvp.Value;
            Debug.Log($"Room {roomId} at Grid {gridPos}");
            
            // Calculate position relative to center
            float x = (gridPos.x - (minimapData.gridMinX + minimapData.gridMaxX) / 2f) * iconSpacing * scale;
            float y = (gridPos.y - (minimapData.gridMinY + minimapData.gridMaxY) / 2f) * iconSpacing * scale;
            
            CreateRoomIcon(roomId, new Vector2(x, y));
        }
        
        // Create connection lines
        // GANTI loop pembuatan garis dengan ini:
        foreach (var kvp in roomManager.Layout.roomDataMap)
        {
            var roomData = kvp.Value;
            int roomId = roomData.id;

            if (!roomIcons.ContainsKey(roomId)) continue;

            for (int dir = 0; dir < 4; dir++)
            {
                int neighborId = roomData.neighbors[dir];
                if (neighborId == 0) continue;
                if (!roomIcons.ContainsKey(neighborId)) continue;

                string key = $"{Mathf.Min(roomId, neighborId)}_{Mathf.Max(roomId, neighborId)}";
                if (connectionLines.ContainsKey(key)) continue; // hindari duplikasi

                CreateConnectionLineAxisAligned(roomId, neighborId); // pakai fungsi baru
            }
        }

        
        // Create player indicator
        CreatePlayerIndicator();
        
        // Set initial room
        SetCurrentRoom(1);
        VisitRoom(1);
    }
    
    void CreateRoomIcon(int roomId, Vector2 position)
    {
        GameObject iconObj;
        if (roomIconPrefab != null)
        {
            Debug.Log("Using custom room icon prefab.");
            iconObj = Instantiate(roomIconPrefab, roomIconContainer);
        }
        else
        {
            iconObj = new GameObject($"Room_{roomId}");
            iconObj.transform.SetParent(roomIconContainer);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = CreateDiamondSprite();
        }
        
        RectTransform rect = iconObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(iconSize, iconSize);
        
        MinimapRoomIcon roomIcon = iconObj.AddComponent<MinimapRoomIcon>();
        roomIcon.roomId = roomId;
        roomIcon.iconImage = iconObj.GetComponent<Image>();
        
        // Set initial color based on room type
        var roomData = roomManager.Layout.roomDataMap[roomId];
        Color baseColor = GetRoomColor(roomData.roomType);
        roomIcon.baseColor = baseColor;
        roomIcon.iconImage.color = Color.Lerp(baseColor, unvisitedRoomColor, 0.7f);
        
        roomIcons[roomId] = roomIcon;
    }

    void CreateConnectionLine(int roomId1, int roomId2)
    {
        string lineKey = $"{Mathf.Min(roomId1, roomId2)}_{Mathf.Max(roomId1, roomId2)}";

        GameObject lineObj = new GameObject($"Line_{lineKey}");
        lineObj.transform.SetParent(connectionLineContainer);

        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = connectionColor;
        lineImage.raycastTarget = false;

        RectTransform lineRect = lineObj.GetComponent<RectTransform>();

        // Calculate line position and rotation
        Vector2 pos1 = roomIcons[roomId1].GetComponent<RectTransform>().anchoredPosition;
        Vector2 pos2 = roomIcons[roomId2].GetComponent<RectTransform>().anchoredPosition;

        Vector2 direction = pos2 - pos1;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.anchoredPosition = (pos1 + pos2) / 2f;
        lineRect.sizeDelta = new Vector2(distance - iconSize * 0.8f, 2f);
        lineRect.rotation = Quaternion.Euler(0, 0, angle);

        connectionLines[lineKey] = lineObj;
    }
    
    void CreateConnectionLineAxisAligned(int roomId1, int roomId2)
    {
        string lineKey = $"{Mathf.Min(roomId1, roomId2)}_{Mathf.Max(roomId1, roomId2)}";

        GameObject lineObj = new GameObject($"Line_{lineKey}");
        lineObj.transform.SetParent(connectionLineContainer);

        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = connectionColor;
        lineImage.raycastTarget = false;

        RectTransform r1 = roomIcons[roomId1].GetComponent<RectTransform>();
        RectTransform r2 = roomIcons[roomId2].GetComponent<RectTransform>();

        Vector2 p1 = r1.anchoredPosition;
        Vector2 p2 = r2.anchoredPosition;
        Vector2 d  = p2 - p1;

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

        // Karena grid kita 4-arah, salah satu komponen pasti 0.
        // Tapi untuk jaga-jaga, kita paksa ke horizontal/vertikal terdekat.
        bool horizontal = Mathf.Abs(d.x) >= Mathf.Abs(d.y);
        if (horizontal)
        {
            Vector2 mid = new Vector2((p1.x + p2.x) * 0.5f, p1.y);
            float width = Mathf.Abs(d.x) - iconSize * 0.8f;
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(Mathf.Max(0, width), 2f);
            rect.rotation = Quaternion.identity; // 0°
        }
        else
        {
            Vector2 mid = new Vector2(p1.x, (p1.y + p2.y) * 0.5f);
            float height = Mathf.Abs(d.y) - iconSize * 0.8f;
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(2f, Mathf.Max(0, height));
            rect.rotation = Quaternion.identity; // tetap, tapi orientasi vertikal dari size
        }

        connectionLines[lineKey] = lineObj;
    }

    
    void CreatePlayerIndicator()
    {
        // CONE
        GameObject coneObj = new GameObject("PlayerFacingCone");
        coneObj.transform.SetParent(roomIconContainer);
        var coneImg = coneObj.AddComponent<Image>();
        coneImg.sprite = CreateWedgeSprite(128, coneAngleDeg);
        coneImg.color = coneColor;
        coneImg.raycastTarget = false;

        playerCone = coneObj.GetComponent<RectTransform>();
        playerCone.anchorMin = playerCone.anchorMax = new Vector2(0.5f, 0.5f);
        playerCone.pivot = new Vector2(0.5f, 0.5f);                // ⟵ penting: center
        float coneDiameter = coneRadius * 2f;                       // kita pakai diameter
        playerCone.sizeDelta = new Vector2(coneDiameter, coneDiameter);

        // DOT
        GameObject playerObj = playerIndicatorPrefab != null
            ? Instantiate(playerIndicatorPrefab, roomIconContainer)
            : new GameObject("Player icon");
        if (playerIndicatorPrefab == null)
        {
            var img = playerObj.AddComponent<Image>();
            img.sprite = CreateCircleSprite();
            img.color = currentRoomColor;
        }

        playerIndicator = playerObj.GetComponent<RectTransform>();
        playerIndicator.anchorMin = playerIndicator.anchorMax = new Vector2(0.5f, 0.5f);
        playerIndicator.pivot = new Vector2(0.5f, 0.5f);
        playerIndicator.sizeDelta = new Vector2(playerIconSize, playerIconSize);

        // urutan render: cone di bawah, dot di atas
        // playerCone.SetSiblingIndex(0);
        // playerIndicator.SetAsLastSibling();
    }

    
    public void SetCurrentRoom(int roomId)
    {
        currentRoomId = roomId;
        VisitRoom(roomId);
        
        // Update player position
        if (roomIcons.ContainsKey(roomId) && playerIndicator != null)
        {
            Vector2 roomPos = roomIcons[roomId].GetComponent<RectTransform>().anchoredPosition;
            playerIndicator.anchoredPosition = roomPos;
            if (playerCone != null) playerCone.anchoredPosition = roomPos;
        }

        // Update room highlights
        foreach (var kvp in roomIcons)
        {
            kvp.Value.SetCurrent(kvp.Key == roomId);
        }

    }
    
    public void VisitRoom(int roomId)
    {
        if (!visitedRooms.Contains(roomId))
        {
            visitedRooms.Add(roomId);
            
            if (roomIcons.ContainsKey(roomId))
            {
                roomIcons[roomId].SetVisited(true);
            }
            
            // Update connected lines visibility
            UpdateConnectionVisibility(roomId);
        }
    }
    
    void UpdateConnectionVisibility(int roomId)
    {
        var roomData = roomManager.Layout.roomDataMap[roomId];
        
        for (int dir = 0; dir < 4; dir++)
        {
            int neighborId = roomData.neighbors[dir];
            if (neighborId != 0)
            {
                string lineKey1 = $"{Mathf.Min(roomId, neighborId)}_{Mathf.Max(roomId, neighborId)}";
                
                if (connectionLines.ContainsKey(lineKey1))
                {
                    connectionLines[lineKey1].GetComponent<Image>().color = Color.Lerp(connectionColor, Color.white, 0.5f);
                }
            }
        }
    }
    
    Color GetRoomColor(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Start => startRoomColor,
            RoomType.Normal => normalRoomColor,
            RoomType.Shop => shopRoomColor,
            RoomType.Boss => bossRoomColor,
            _ => normalRoomColor
        };
    }
    
    // Sprite creation helpers
    Sprite CreateCircleSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance <= center)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }
    
    Sprite CreateCircleOutlineSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;
        float thickness = 4f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance <= center && distance >= center - thickness)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    Sprite CreateDiamondSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float manhattan = Mathf.Abs(x - center) + Mathf.Abs(y - center);
                if (manhattan <= center)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }
    
    Sprite CreateWedgeSprite(int size, float angleDeg)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        // orientasi default: “menghadap kanan” (sumbu +X), nanti kita putar di runtime
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;
        float halfRad = angleDeg * 0.5f * Mathf.Deg2Rad;

        // opsional: kosongkan sedikit bagian belakang supaya terlihat “kerucut”
        float innerRadius = radius * 0.12f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                Vector2 d = (p - center);
                float r = d.magnitude;

                if (r < innerRadius || r > radius) { tex.SetPixel(x, y, Color.clear); continue; }

                if (r == 0) { tex.SetPixel(x, y, Color.clear); continue; }

                Vector2 dn = d.normalized;
                // sudut terhadap +X
                float dot = Vector2.Dot(dn, Vector2.right);            // cos(theta)
                float theta = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));   // 0..pi
                // pastikan sisi atas/bawah benar (pakai cross z)
                float crossZ = Vector3.Cross(new Vector3(1,0,0), new Vector3(dn.x,dn.y,0)).z;
                theta = crossZ >= 0 ? theta : -theta;

                if (Mathf.Abs(theta) <= halfRad)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100f);
    }

}

