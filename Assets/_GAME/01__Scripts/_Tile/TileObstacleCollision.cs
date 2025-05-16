using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class TileObstacleCollision : MonoBehaviour
{
    public Tile tile;
    public bool isPositioned;
    public Obstacle obstacle;

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ObstaclePositionCollider"))
        {
            if (tile.obstacleDict.ContainsKey(obstacle))
            {
                tile.obstacleDict.Remove(obstacle);
            }
            tile.isObstaclePositioned = false;
       
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Obstacle"))
        {
            Tile oldTile = other.GetComponent<Obstacle>().tile;
            if (oldTile != null && oldTile.obstacleDict.ContainsKey(other.transform.GetComponent<Obstacle>()))
                RemoveObstacle(other.transform.GetComponent<Obstacle>(), oldTile);
            if (!tile.obstacleDict.ContainsKey(other.transform.GetComponent<Obstacle>()))
                AddObstacle(other.transform.GetComponent<Obstacle>(), tile);
        }
    }
    private void RemoveObstacle(Obstacle obs, Tile oldTile)
    {
        if (oldTile == null) return;
        else if (oldTile.obstacleDict.ContainsKey(obs))
        {
            oldTile.obstacleDict.Remove(obs);
          
            oldTile.hasObject = false;
            if (!obs.isBeingPushed)
            {
                isPositioned = false;
            }
        }
    }
    private void AddObstacle(Obstacle obs, Tile tile)
    {
        tile.obstacleDict.Add(obs, obs.transform.position.y);
        obs.tile = this.tile;
       
        if (!obs.isBeingPushed)
        {
            isPositioned = false;
        }
    }

}
