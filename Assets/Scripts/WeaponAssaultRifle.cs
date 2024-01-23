using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponAssaultRifle : WeaponBase
{
    [Header("Fire Effects")]
    [SerializeField]
    private GameObject muzzleFlashEffect;           // 총구 이펙트 (on/off)

    [Header("Spawn Points")]
    [SerializeField]
    private Transform casingSpawnPoint;             // 탄피 생성 위치
    [SerializeField]
    private Transform bulletSpawnPoint;             // 총알 생성 위치

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipTakeOutWeapon;       // 무기 장착 사운드
    [SerializeField]
    private AudioClip audioClipFire;                // 공격 사운드
    [SerializeField]
    private AudioClip audioClipReload;              // 재장전 사운드

    [Header("Aim UI")]
    [SerializeField]
    private Image imageAim;                         // default/aim mode에 따라 Aim 이미지 활성화/비활성화

    private bool isModeChange = false;              // 모드 전환 여부 체크
    private bool isAimMode = false;                 // Aim 모드인지 여부 체크
    private float defaultFOV = 60;                  // 기본 모드에서의 카메라 FOV
    private float aimModeFOV = 30;                  // Aim 모드에서의 카메라 FOV

    private CasingMemoryPool casingMemoryPool;      // 탄피 생성 후 활성/비활성화 관리
    private ImpactMemoryPool impactMemoryPool;      // 공격 효과 생성 후 활성/비활성 관리
    private Camera mainCamera;                      // 광선 발사

    private void Awake()
    {
        // 기반 클래스의 초기화를 위한 Setup() 메소드 호출   
        base.Setup();

        casingMemoryPool = GetComponent<CasingMemoryPool>();
        impactMemoryPool = GetComponent<ImpactMemoryPool>();
        mainCamera = Camera.main;

        // 처음 탄창 수는 최대로 설정
        weaponSetting.currentMagazine = weaponSetting.maxMagazine;
        // 처음 탄약 수는 최대로 설정
        weaponSetting.currentAmmo = weaponSetting.maxAmmo;
    }

    private void OnEnable()
    {
        // 무기 장착 사운드 재생
        PlaySound(audioClipTakeOutWeapon);
        // 총구 이펙트 오브젝트 비활성화
        muzzleFlashEffect.SetActive(false);

        // 무기가 활성화될 때 해당 무기의 탄창 수 정보를 제공한다.
        onMagazineEvent.Invoke(weaponSetting.currentMagazine);
        // 무기가 활성화될 때 해당 무기의 탄약 수 정보를 제공한다.
        onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

        ResetVariables();
    }

    public override void StartWeaponAction(int type = 0)
    {
        // 재장전 중일 때는 무기 액션을 할 수 없음
        if (isReload) return;

        // 모드 전환 중일 때는 무기 액션을 할 수 없음
        if (isModeChange) return;

        // 마우스 왼쪽 클릭 (공격 시작)
        if (type == 0)
        {
            // 공격을 눌렀으나 탄환이 없을 때 재장전을 함
            if (weaponSetting.currentAmmo <= 0)
            {
                StartReload();
            }
            // 연속 공격
            else if (weaponSetting.isAutomaticAttack)
            {
                isAttack = true;
                StartCoroutine("OnAttackLoop");
            }
            // 단발 공격
            else
            {
                OnAttack();
            }
        }
        // 마우스 오른쪽 클릭 (모드 전환)
        else
        {
            // 공격 중일 때는 모드 전환을 할 수 없다.
            if (isAttack) return;

            StartCoroutine("OnModeChange");
        }
    }

    public override void StopWeaponAction(int type = 0)
    {
        // 마우스 왼쪽 클릭 (공격 종료)
        if (type == 0)
        {
            isAttack = false;
            StopCoroutine("OnAttackLoop");
        }
    }

    public override void StartReload()
    {
        // 현재 재장전 중이라면 재장전 불가능
        if (isReload)
            return;
        // 이미 탄약 수가 최대거나 탄창 수가 0이면 재장전 불가능
        if (weaponSetting.currentAmmo == weaponSetting.maxAmmo || weaponSetting.currentMagazine <= 0)
            return;

        // 무기 액션 도중에 'R'키를 눌러 재장전을 시도하면 무기 액션 종료 후 재장전
        StopWeaponAction();

        // Aim 모드일 때 재장전을 하면 defaultFOV로 변경 후 재장전이 끝난 후 원상복귀
        if (isAimMode)
        {
            mainCamera.fieldOfView = defaultFOV;
        }

        StartCoroutine("OnReload");
    }

    private IEnumerator OnAttackLoop()
    {
        while (true)
        {
            OnAttack();

            yield return null;
        }
    }

    public void OnAttack()
    {
        if (Time.time - lastAttackTime > weaponSetting.attackRate)
        {
            // 뛰고 있거나 탄약이 없을 때는 공격할 수 없음
            if (animator.MoveSpeed > 0.5f || weaponSetting.currentAmmo <= 0 || animator.CurrentAnimationIs("TakeOutWeapon"))
            {
                return;
            }

            // 공격 주기가 되어야 공격할 수 있도록 하기 위해 현재 시간 저장
            lastAttackTime = Time.time;

            // 공격시 currentAmmo 1 감소
            weaponSetting.currentAmmo--;
            // 탄 수 UI 업데이트
            onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

            // 무기 애니메이션 재생 (모드에 따라 AimFire or Fire 애니메이션 재생)
            string animation = animator.AimModeIs ? "AimFire" : "Fire";
            animator.Play(animation, -1, 0);
            // 총구 이펙트 재생 (default mode일 때만 재생)
            if (!animator.AimModeIs) StartCoroutine("OnMuzzleFlashEffect");
            // 공격 사운드 재생
            PlaySound(audioClipFire);
            // 탄피 생성
            casingMemoryPool.SpawnCasing(casingSpawnPoint.position, transform.right);

            // 광선을 발사해 원하는 위치 공격 (+Impact Effect)
            TwoStepRaycast();
        }
    }

    private IEnumerator OnMuzzleFlashEffect()
    {
        muzzleFlashEffect.SetActive(true);

        yield return new WaitForSeconds(weaponSetting.attackRate * 0.3f);

        muzzleFlashEffect.SetActive(false);
    }

    private IEnumerator OnReload()
    {
        isReload = true;

        // 재장전 애니메이션, 사운드 재생
        animator.OnReload();
        PlaySound(audioClipReload);

        while (true)
        {
            // 사운드가 재생 중이 아니고, 현재 애니메이션이 Movement이면
            // 재장전 애니메이션(, 사운드) 재생이 종료되었다는 뜻
            if (audioSource.isPlaying == false && (animator.CurrentAnimationIs("Movement") || animator.CurrentAnimationIs("AimFirePose")))
            {
                // Aim 모드일 때 재장전이 끝난 후 원상복귀
                if (isAimMode)
                {
                    mainCamera.fieldOfView = aimModeFOV;
                }

                isReload = false;

                // 현재 탄창 수를 1 감소시키고, 바뀐 탄창 정보를 Text UI에 업데이트
                weaponSetting.currentMagazine--;
                onMagazineEvent.Invoke(weaponSetting.currentMagazine);
                // 현재 탄약 수를 최대로 설정하고, 바뀐 탄약 수 정보를 Text UI에 업데이트
                weaponSetting.currentAmmo = weaponSetting.maxAmmo;
                onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

                yield break;
            }

            yield return null;
        }
    }

    private void TwoStepRaycast()
    {
        Ray ray;
        RaycastHit hit;
        Vector3 targetPoint = Vector3.zero;

        // 화면의 중앙 좌표 (Aim 기준으로 Raycast 연산)
        ray = mainCamera.ViewportPointToRay(Vector2.one * 0.5f);
        // 공격 사거리(attackDistance) 안에 부딪히는 오브젝트가 있으면 targetPoint는 광선에 부딪힌 위치
        if (Physics.Raycast(ray, out hit, weaponSetting.attackDistance))
        {
            targetPoint = hit.point;
        }
        // 공격 사거리 안에 부딪히는 오브젝트가 없으면 targetPoint는 최대 사거리 위치
        else
        {
            targetPoint = ray.origin + ray.direction * weaponSetting.attackDistance;
        }
        Debug.DrawRay(ray.origin, ray.direction * weaponSetting.attackDistance, Color.red);

        // 첫번째 Raycast 연산으로 얻어진 targetPoint를 목표지점으로 설정하고,
        // 총구를 시작 지점으로 하여 Raycast 연산
        Vector3 attackDirection = (targetPoint - bulletSpawnPoint.position).normalized;
        if (Physics.Raycast(bulletSpawnPoint.position, attackDirection, out hit, weaponSetting.attackDistance))
        {
            impactMemoryPool.SpawnImpact(hit);

            if (hit.transform.CompareTag("ImpactEnemy"))
            {
                hit.transform.GetComponent<EnemyFSM>().TakeDamage(weaponSetting.damage);
            }
            else if (hit.transform.CompareTag("InteractionObject"))
            {
                hit.transform.GetComponent<InteractionObject>().TakeDamage(weaponSetting.damage);
            }
        }
        Debug.DrawRay(bulletSpawnPoint.position, attackDirection * weaponSetting.attackDistance, Color.blue);
    }

    private IEnumerator OnModeChange()
    {
        float current = 0;
        float percent = 0;
        float time = 0.35f;

        animator.AimModeIs = !animator.AimModeIs;
        imageAim.enabled = !imageAim.enabled;

        float start = mainCamera.fieldOfView;
        float end = animator.AimModeIs ? aimModeFOV : defaultFOV;

        isModeChange = true;

        while (percent < 1)
        {
            current += Time.deltaTime;
            percent = current / time;

            // mode에 따라 카메라의 시야각 변경
            mainCamera.fieldOfView = Mathf.Lerp(start, end, percent);

            yield return null;
        }

        isAimMode = animator.AimModeIs;
        isModeChange = false;
    }

    private void ResetVariables()
    {
        isReload = false;
        isAttack = false;
        isModeChange = false;
    }
}
