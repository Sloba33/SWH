using UnityEngine;
using Obstacles;
using System.Collections;
public class JackInTheBox : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] ParticleSystem ps;
    private Obstacle obstacle;
    [SerializeField] AudioSource audioSource;
  


    private void Awake()
    {
        obstacle = GetComponent<Obstacle>();
        audioSource = GetComponent<AudioSource>();
    }

    public void TriggerJackInTheBox()
    {
        // Play the animation
        PlayAnimation();

        // Change the obstacle type to "Fake"
        obstacle.obstacleType = ObstacleType.Fake;

        // Additional logic can go here if needed
    }
    private void PlayAnimation()
    {
        // Your animation logic here
        anim.Play("JackInTheBox");
        audioSource.Play();
        ps.Play();
        StartCoroutine(DestroyJitb());

    }
    public IEnumerator DestroyJitb()
    {
        yield return new WaitForSeconds(3.5f);
        obstacle.ParticleDestroy();
        
    }
}
