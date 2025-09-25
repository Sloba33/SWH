
using UnityEngine;

public class Trap : MonoBehaviour
{

    Player player;
    private void OnTriggerEnter(Collider other)
    {


        {
            if (other.transform.CompareTag("Player"))
            {
                player = other.transform.GetComponent<Player>();
                Debug.Log("Player found");

                player.Die();

            }

        }
    }
    private void OnCollisionEnter(Collision other)
    {
        if (other.transform.CompareTag("Player"))
        {
            player = other.transform.GetComponent<Player>();
            Debug.Log("Player found");

            player.Die();
            player.transform.GetComponent<Outline>().enabled = false;

        }

    }
}
