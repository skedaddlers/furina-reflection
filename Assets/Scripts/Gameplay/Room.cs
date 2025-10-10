using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public RoomType roomType;
    public int roomIndex;
    public SpawnTrigger spawnTrigger = null;
    public int[] roomNeighbors = new int[4]; // [forward, right, back, left]
    public GameObject[] doors = new GameObject[4]; // urutan sama

    public GameObject enemyPrefab;
    public Vector3 playerSpawn = new Vector3(0, 0, 0);

    public void Initialize(RoomData data)
    {
        roomIndex = data.id;
        roomNeighbors = data.neighbors;
        roomType = data.roomType;

        // Set doors active/inactive based on neighbors
        for (int i = 0; i < 4; i++)
        {
            if (roomNeighbors[i] != 0)
            {
                doors[i].SetActive(true);
                doors[i].GetComponent<DoorTrigger>().parentRoom = this;
                doors[i].GetComponent<DoorTrigger>().directionIndex = i;
                if (spawnTrigger != null)
                    spawnTrigger.parentRoom = this;

            }
            else
                doors[i].SetActive(false);
        }
    }

    public void SpawnEnemiesInRoom()
    {
        Debug.Log($"Spawning enemies in Room {roomIndex}");
        
    }
    public void OnPlayerEnter(int fromDirection)
    {
        Debug.Log($"Player entered Room {roomIndex} from {fromDirection}");
        if (roomNeighbors[fromDirection] != 0)
            doors[fromDirection].SetActive(true);
    }

    public void GoToNeighbor(int direction)
    {
        int nextRoomID = roomNeighbors[direction];
        if (nextRoomID == 0) return;
        GameManager.Instance.roomManager.MovePlayerToRoom(nextRoomID, GetOpposite(direction));
    }

    int GetOpposite(int dir)
    {
        // forward↔back, right↔left
        return dir switch
        {
            0 => 2,
            1 => 3,
            2 => 0,
            3 => 1,
            _ => -1
        };
    }

}