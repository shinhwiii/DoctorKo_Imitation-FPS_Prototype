using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponAssaultRifle : WeaponBase
{
    [Header("Fire Effects")]
    [SerializeField]
    private GameObject muzzleFlashEffect;           // �ѱ� ����Ʈ (on/off)

    [Header("Spawn Points")]
    [SerializeField]
    private Transform casingSpawnPoint;             // ź�� ���� ��ġ
    [SerializeField]
    private Transform bulletSpawnPoint;             // �Ѿ� ���� ��ġ

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipTakeOutWeapon;       // ���� ���� ����
    [SerializeField]
    private AudioClip audioClipFire;                // ���� ����
    [SerializeField]
    private AudioClip audioClipReload;              // ������ ����

    [Header("Aim UI")]
    [SerializeField]
    private Image imageAim;                         // default/aim mode�� ���� Aim �̹��� Ȱ��ȭ/��Ȱ��ȭ

    private bool isModeChange = false;              // ��� ��ȯ ���� üũ
    private bool isAimMode = false;                 // Aim ������� ���� üũ
    private float defaultFOV = 60;                  // �⺻ ��忡���� ī�޶� FOV
    private float aimModeFOV = 30;                  // Aim ��忡���� ī�޶� FOV

    private CasingMemoryPool casingMemoryPool;      // ź�� ���� �� Ȱ��/��Ȱ��ȭ ����
    private ImpactMemoryPool impactMemoryPool;      // ���� ȿ�� ���� �� Ȱ��/��Ȱ�� ����
    private Camera mainCamera;                      // ���� �߻�

    private void Awake()
    {
        // ��� Ŭ������ �ʱ�ȭ�� ���� Setup() �޼ҵ� ȣ��   
        base.Setup();

        casingMemoryPool = GetComponent<CasingMemoryPool>();
        impactMemoryPool = GetComponent<ImpactMemoryPool>();
        mainCamera = Camera.main;

        // ó�� źâ ���� �ִ�� ����
        weaponSetting.currentMagazine = weaponSetting.maxMagazine;
        // ó�� ź�� ���� �ִ�� ����
        weaponSetting.currentAmmo = weaponSetting.maxAmmo;
    }

    private void OnEnable()
    {
        // ���� ���� ���� ���
        PlaySound(audioClipTakeOutWeapon);
        // �ѱ� ����Ʈ ������Ʈ ��Ȱ��ȭ
        muzzleFlashEffect.SetActive(false);

        // ���Ⱑ Ȱ��ȭ�� �� �ش� ������ źâ �� ������ �����Ѵ�.
        onMagazineEvent.Invoke(weaponSetting.currentMagazine);
        // ���Ⱑ Ȱ��ȭ�� �� �ش� ������ ź�� �� ������ �����Ѵ�.
        onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

        ResetVariables();
    }

    public override void StartWeaponAction(int type = 0)
    {
        // ������ ���� ���� ���� �׼��� �� �� ����
        if (isReload) return;

        // ��� ��ȯ ���� ���� ���� �׼��� �� �� ����
        if (isModeChange) return;

        // ���콺 ���� Ŭ�� (���� ����)
        if (type == 0)
        {
            // ������ �������� źȯ�� ���� �� �������� ��
            if (weaponSetting.currentAmmo <= 0)
            {
                StartReload();
            }
            // ���� ����
            else if (weaponSetting.isAutomaticAttack)
            {
                isAttack = true;
                StartCoroutine("OnAttackLoop");
            }
            // �ܹ� ����
            else
            {
                OnAttack();
            }
        }
        // ���콺 ������ Ŭ�� (��� ��ȯ)
        else
        {
            // ���� ���� ���� ��� ��ȯ�� �� �� ����.
            if (isAttack) return;

            StartCoroutine("OnModeChange");
        }
    }

    public override void StopWeaponAction(int type = 0)
    {
        // ���콺 ���� Ŭ�� (���� ����)
        if (type == 0)
        {
            isAttack = false;
            StopCoroutine("OnAttackLoop");
        }
    }

    public override void StartReload()
    {
        // ���� ������ ���̶�� ������ �Ұ���
        if (isReload)
            return;
        // �̹� ź�� ���� �ִ�ų� źâ ���� 0�̸� ������ �Ұ���
        if (weaponSetting.currentAmmo == weaponSetting.maxAmmo || weaponSetting.currentMagazine <= 0)
            return;

        // ���� �׼� ���߿� 'R'Ű�� ���� �������� �õ��ϸ� ���� �׼� ���� �� ������
        StopWeaponAction();

        // Aim ����� �� �������� �ϸ� defaultFOV�� ���� �� �������� ���� �� ���󺹱�
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
            // �ٰ� �ְų� ź���� ���� ���� ������ �� ����
            if (animator.MoveSpeed > 0.5f || weaponSetting.currentAmmo <= 0 || animator.CurrentAnimationIs("TakeOutWeapon"))
            {
                return;
            }

            // ���� �ֱⰡ �Ǿ�� ������ �� �ֵ��� �ϱ� ���� ���� �ð� ����
            lastAttackTime = Time.time;

            // ���ݽ� currentAmmo 1 ����
            weaponSetting.currentAmmo--;
            // ź �� UI ������Ʈ
            onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

            // ���� �ִϸ��̼� ��� (��忡 ���� AimFire or Fire �ִϸ��̼� ���)
            string animation = animator.AimModeIs ? "AimFire" : "Fire";
            animator.Play(animation, -1, 0);
            // �ѱ� ����Ʈ ��� (default mode�� ���� ���)
            if (!animator.AimModeIs) StartCoroutine("OnMuzzleFlashEffect");
            // ���� ���� ���
            PlaySound(audioClipFire);
            // ź�� ����
            casingMemoryPool.SpawnCasing(casingSpawnPoint.position, transform.right);

            // ������ �߻��� ���ϴ� ��ġ ���� (+Impact Effect)
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

        // ������ �ִϸ��̼�, ���� ���
        animator.OnReload();
        PlaySound(audioClipReload);

        while (true)
        {
            // ���尡 ��� ���� �ƴϰ�, ���� �ִϸ��̼��� Movement�̸�
            // ������ �ִϸ��̼�(, ����) ����� ����Ǿ��ٴ� ��
            if (audioSource.isPlaying == false && (animator.CurrentAnimationIs("Movement") || animator.CurrentAnimationIs("AimFirePose")))
            {
                // Aim ����� �� �������� ���� �� ���󺹱�
                if (isAimMode)
                {
                    mainCamera.fieldOfView = aimModeFOV;
                }

                isReload = false;

                // ���� źâ ���� 1 ���ҽ�Ű��, �ٲ� źâ ������ Text UI�� ������Ʈ
                weaponSetting.currentMagazine--;
                onMagazineEvent.Invoke(weaponSetting.currentMagazine);
                // ���� ź�� ���� �ִ�� �����ϰ�, �ٲ� ź�� �� ������ Text UI�� ������Ʈ
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

        // ȭ���� �߾� ��ǥ (Aim �������� Raycast ����)
        ray = mainCamera.ViewportPointToRay(Vector2.one * 0.5f);
        // ���� ��Ÿ�(attackDistance) �ȿ� �ε����� ������Ʈ�� ������ targetPoint�� ������ �ε��� ��ġ
        if (Physics.Raycast(ray, out hit, weaponSetting.attackDistance))
        {
            targetPoint = hit.point;
        }
        // ���� ��Ÿ� �ȿ� �ε����� ������Ʈ�� ������ targetPoint�� �ִ� ��Ÿ� ��ġ
        else
        {
            targetPoint = ray.origin + ray.direction * weaponSetting.attackDistance;
        }
        Debug.DrawRay(ray.origin, ray.direction * weaponSetting.attackDistance, Color.red);

        // ù��° Raycast �������� ����� targetPoint�� ��ǥ�������� �����ϰ�,
        // �ѱ��� ���� �������� �Ͽ� Raycast ����
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

            // mode�� ���� ī�޶��� �þ߰� ����
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
