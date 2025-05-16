using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public GameObject prefab;
    public int width = 12;
    public int height = 15;
    public float prefabSize = 1f;
    public float yOffset = -0.554f;

    private Tile[,] grid;

    void Start()
    {
        GenerateGrid();
        Tile tile = GetTileAtCoordinates(2, 3);
        if (tile != null)
        {
            Debug.Log(tile.transform.position);
        }
    }


    void GenerateGrid()
    {
        grid = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 position = new Vector3(x * prefabSize - 4f, yOffset, -z * prefabSize + 9f);

                GameObject tileGO = Instantiate(prefab, position, Quaternion.identity);
                Tile tileComponent = tileGO.GetComponent<Tile>();

                if (tileComponent != null)
                {
                    grid[x, z] = tileComponent;
                }
                else
                {
                    Debug.LogError("Tile component not found on the prefab!");
                }
            }
        }
        PopulateNeighbours();
    }

    Tile GetTileAtCoordinates(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            return grid[x, z];
        }
        else
        {
            return null;
        }
    }
    void PopulateNeighbours()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Tile currentTile = grid[x, z];
                currentTile.neighbourList = new List<Tile>();

                // Check top neighbor
                if (z < height - 1)
                    currentTile.neighbourList.Add(grid[x, z + 1]);

                // Check bottom neighbor
                if (z > 0)
                    currentTile.neighbourList.Add(grid[x, z - 1]);

                // Check left neighbor
                if (x > 0)
                    currentTile.neighbourList.Add(grid[x - 1, z]);

                // Check right neighbor
                if (x < width - 1)
                    currentTile.neighbourList.Add(grid[x + 1, z]);
            }
        }
    }

}


