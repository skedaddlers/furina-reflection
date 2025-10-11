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
    
    private Dictionary<int, MinimapRoomIcon> roomIcons = new Dictionary<int, MinimapRoomIcon>();
    private Dictionary<string, GameObject> connectionLines = new Dictionary<string, GameObject>();
    private RectTransform playerIndicator;
    private HashSet<int> visitedRooms = new HashSet<int>();
    private int currentRoomId = -1;
    private RoomManager roomManager;
    
    void Start()
    {
        SetupMinimapUI();
        roomManager = GameManager.Instance.roomManager;
        
        // // Wait a frame for room generation to complete
        // Invoke(nameof(InitializeMinimap), 0.1f);
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
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(minimapContainer);
        Image border = borderObj.AddComponent<Image>();
        border.sprite = CreateCircleOutlineSprite();
        border.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        border.raycastTarget = false;
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;
        borderObj.transform.SetAsLastSibling();
    }
    
    public void InitializeMinimap()
    {
        if (roomManager == null) return;

        var minimapData = roomManager.minimapData;
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
        foreach (var kvp in roomManager.minimapData.roomDataMap)
        {
            var roomData = kvp.Value;
            int roomId = roomData.id;
            
            if (!roomIcons.ContainsKey(roomId)) continue;
            
            for (int dir = 0; dir < 4; dir++)
            {
                int neighborId = roomData.neighbors[dir];
                if (neighborId != 0 && neighborId > roomId) // Only create line once
                {
                    if (roomIcons.ContainsKey(neighborId))
                    {
                        CreateConnectionLine(roomId, neighborId);
                    }
                }
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
        var roomData = roomManager.minimapData.roomDataMap[roomId];
        Color baseColor = GetRoomColor(roomData.roomType);
        roomIcon.baseColor = baseColor;
        iconObj.GetComponent<Image>().color = Color.Lerp(baseColor, unvisitedRoomColor, 0.7f);
        
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
    
    void CreatePlayerIndicator()
    {
        GameObject playerObj;
        if (playerIndicatorPrefab != null)
        {
            playerObj = Instantiate(playerIndicatorPrefab, roomIconContainer);
        }
        else
        {
            playerObj = new GameObject("PlayerIndicator");
            playerObj.transform.SetParent(roomIconContainer);
            Image img = playerObj.AddComponent<Image>();
            img.sprite = CreateCircleSprite();
            img.color = currentRoomColor;
        }
        
        playerIndicator = playerObj.GetComponent<RectTransform>();
        playerIndicator.sizeDelta = new Vector2(playerIconSize, playerIconSize);
        playerIndicator.SetAsLastSibling();
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
        var roomData = roomManager.minimapData.roomDataMap[roomId];
        
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
    
    void Update()
    {
        // Animate player indicator
        if (playerIndicator != null)
        {
            float scale = 1f + Mathf.Sin(Time.time * playerBlinkSpeed) * 0.2f;
            playerIndicator.localScale = Vector3.one * scale;
        }
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
}

