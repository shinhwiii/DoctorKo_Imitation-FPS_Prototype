using UnityEngine;

public class ParticleAutoDestroyerByTime : MonoBehaviour
{
    private ParticleSystem particle;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        // 파티클이 재생 중이 아니라면 삭제
        if (!particle.isPlaying)
            Destroy(gameObject);
    }
}
