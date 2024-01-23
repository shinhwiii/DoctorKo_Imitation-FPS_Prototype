using UnityEngine;

public class Impact : MonoBehaviour
{
    private ParticleSystem particle;
    private MemoryPool memoryPool;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    public void Setup(MemoryPool pool)
    {
        memoryPool = pool;
    }

    private void Update()
    {
        // 파티클이 재생 중이 아니면 삭제
        if (!particle.isPlaying)
        {
            memoryPool.DeactivatePoolItem(gameObject);
        }
    }
}
