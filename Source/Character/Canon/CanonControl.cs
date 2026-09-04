using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Spine.Unity;
using Spine;

public class CanonControl : MonoBehaviour, IDamageable, IKnockbackable, ISkillUser
{
    public SkeletonAnimation spineAnim;
    private Rigidbody2D rb;
    public Collider2D canonCollider;
    public Collider2D lyingCollider; // 엎드리기 충돌 범위

    int canonLayer;
    int droppingLayer;
    int invisibleLayer;
    bool isDropping = false; // 아래점프 중인지

    public bool storyMode = false; // 스토리 연출 중

    // 카논의 Damage 출력
    private DamageUIManager damageUIManager;

    // 위치 백업용 변수
    private Vector3 defaultDamagePos;

    // 레벨 업 관련
    public GameObject LevelUpParticle;
    public GameObject StatusText;
    public TextMeshProUGUI levelupText;

    // 전직 관련
    public MagicTierEffect TierEffect;
    
    // 전투 관련
    public float canonMagicConstant = 1.24f;
    private SkillManager skillManager;
    public EarthElementManager earthManager;
    public CapsuleCollider2D Sk00Range; // 공격 범위
    public bool usingSkill = false;
    public bool canSkill00 = true;
    public float skill00Cooldown = 1.2f; // 기본 공격 쿨타임

    public bool canSkill01 = true;
    public Sk_02 TeleportSkill;
    public Sk_03 LifeTransitionSkill;
    public bool canSkill03 = true;
    public bool canSkill04 = true;
    public bool canSkill05 = true;
    public bool canSkill06 = true;

    [Header("바위 조각 시스템")]
    public RockShardManager rockshardManager;
    public int hasRockShard => rockshardManager.CurrentShardCount; // 현재 개수 바로 참조

    private bool IsRockShardUnlocked
    {
        get
        {
            int Pa02Level = SkillManager.Instance?.GetSkillLevel("Pa_02") ?? 0;
            return Pa02Level >= 2;
        }
    }

    private int maxRockShard
    {
        get
        {
            int Pa02Level = SkillManager.Instance?.GetSkillLevel("Pa_02") ?? 0;
            return Pa02Level switch
            {
                2 => 8,
                3 => 12,
                _ => 0
            };
        }
    }

    // 바위 조각 생성 함수 (타격 성공 시 사용)
    public void TrySpawnRockShards(int count)
    {
        if (!IsRockShardUnlocked || maxRockShard == 0) return;

        int canSpawn = Mathf.Min(count, maxRockShard - hasRockShard);
        if (canSpawn > 0)
            rockshardManager.SpawnShards(transform.position, canSpawn);
    }

    // 필드에 코루틴 핸들러 변수 추가
    private Coroutine autoChargeCoroutine;

    // 버프 관련
    public Transform buffUI;
    // 디버프 스탯 백업
    private float savedJumpForce;
    private bool isOsmotic = false;
    private Coroutine osmoticCoroutine;

    private bool isGrounded = true; // 땅에 착지
    public bool isDead = false; // 캐릭터가 사망했는지 여부
    public bool isLyingdown = false;
    public bool isResting = false;

    private bool hitting = false; // 피격 상태 확인
    private bool knockback = false; // 넉백?
    private float hitCooldown = 1.2f; // 피격 간격
    private float kbCooldown = 0.5f; // 경직 시간

    public CharacterStats stats; // 캐릭터 스탯

    // 인벤토리 및 장비창
    public Inventory playerInventory;
    public Equipment playerEquipment;
    private string currentWeaponType = "None";

    // 키설정
    private KeyManager key;

    // 맵 속성
    private MapTitleScript mapTitle;
    private bool isTopView = false;
    private float originalGravityScale;

    void Awake()
    {
        // 캐릭터 스탯 초기화
        // (HP, MP, 공격력, 마력, 방어력, 크리티컬, 크리티컬데미지, 마법 숙련도, 피해 감소, 넉백 저항)
        // (STR, INT, DEX, 발사체 추가 데미지, 이동 속도, 점프력, 회피율)
        // 레벨, 현재 경험치, 필요 경험치, 최대 레벨, 전직 차수
        // 땅 속성 강화, 땅 속성 내성, 물 속성 강화, 물 속성 내성, 아이템 드롭률 증가, 배낭 용량, HP 재생력, MP 재생력
        stats =
            new CharacterStats
            (50, 50, 3, 1, 0, 0.05f, 1.3f, 0.7f, 0, 10, 5, 5, 5, 1f, 1.8f, 4.9f, 0.01f, 1, 0, 70, 30, 0, 0f, 0f, 0f, 0f, 1.0f, 2, 10, 10, 0f);
        stats.LoadStats();
        stats.RecalcLevelupExp();
        stats.UpdateDEX();

        canonLayer = gameObject.layer;
        droppingLayer = LayerMask.NameToLayer("Dropping");
        invisibleLayer = LayerMask.NameToLayer("Invisible");
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lyingCollider.enabled = false; // 엎드리기 충돌 범위 비활성화

        damageUIManager = FindObjectOfType<DamageUIManager>();

        Sk00Range.enabled = false; // 초기에는 비활성화

        if (buffUI != null)
        {
            buffUI.localPosition = new Vector3(0, 2f, 0);
        }
        // 인벤토리 장비창 데이터 불러오기
        playerInventory.LoadInventory();
        playerEquipment.LoadEquipment();
        playerEquipment.OnEquipmentChanged += OnEquipmentChanged;

        ItemData equippedWeapon = playerEquipment.weaponSlot.equippedItem;
        UpdateWeaponVisuals(equippedWeapon);

        skillManager = SkillManager.Instance;

        // 키세팅 불러오기
        key = KeyManager.Instance;

        // 탑뷰 여부
        mapTitle = FindObjectOfType<MapTitleScript>();
        isTopView = mapTitle.TopViewMap;
        originalGravityScale = rb.gravityScale;
        ApplyTopView(isTopView);
    }

    void Update()
    {
        // 휴식 모드
        if (isResting)
        {
            if (rb != null) rb.velocity = Vector2.zero;

            PlayAnimation("Sit", true);

            // 휴식 해제 입력 (좌/우 이동키, 점프키)
            bool moveKey = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f;
            bool jumpKey = Input.GetKeyDown(key.GetKey("Jump"));
            if (moveKey || jumpKey)
            {
                isResting = false;
                PlayAnimation("Stand", true); // 자연스럽게 서기
            }
            return;
        }

        if (isDead || knockback || usingSkill) return; // 캐릭터 조작 불가 조건

        stats.UpdateHPRegeneration(Time.deltaTime);
        stats.UpdateMPRegeneration(Time.deltaTime);

        if (storyMode) return;

        if (isTopView)
        {
            // 탑뷰 이동
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // 대각선 가속 방지, 정규화
            Vector2 dir = new Vector2(h, v);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            rb.velocity = dir * stats.MoveSpeed;

            // 좌/우에만 Flip 적용
            if (Mathf.Abs(h) > 0.01f) Flip(h);

            // 애니메이션
            if (dir.sqrMagnitude > 0.001f) PlayAnimation("Walk", true);
            else PlayAnimation("Stand", true);

            return;
        }

        // 이동 입력 받기
        float CanonMove = isLyingdown ? 0 : Input.GetAxis("Horizontal");

        if (!isLyingdown)
        {
            rb.velocity = new Vector2(CanonMove * stats.MoveSpeed, rb.velocity.y);

            // 이동 방향이 있을 때만 Flip 실행
            if (Mathf.Abs(CanonMove) > 0.01f)
            {
                Flip(CanonMove);
            }
        }

        if (isLyingdown)
        {
            PlayAnimation("LyingDown", true);
        }
        else if (!isGrounded)
        {
            PlayAnimation("Jump", false);
        }
        else if (CanonMove != 0 && !hitting)
        {
            PlayAnimation("Walk", true);
        }
        else if (!hitting)
        {
            PlayAnimation("Stand", true);
        }

        // 점프
        if (Input.GetKeyDown(key.GetKey("Jump")) && isGrounded && !isLyingdown)
        {
            rb.AddForce(new Vector2(0f, stats.JumpForce), ForceMode2D.Impulse);
            isGrounded = false; // 점프 중이므로 땅에 닿아 있지 않음
            PlayAnimation("Jump", false);
        }

        // 마을에서 아래로 발판 통과
        if (Input.GetKey(KeyCode.DownArrow) &&
            Input.GetKeyDown(key.GetKey("Jump")))
        {
            StartCoroutine(DropPlatform());
            return;
        }

        // 엎드리기
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            isLyingdown = true;
            rb.velocity = Vector2.zero;
            LyingDownCollider();
            PlayAnimation("LyingDown", true);
            return;
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            isLyingdown = false;
            LyingDownCollider();
            PlayAnimation("Stand", true);
        }

        // 기본 공격 Sk_00
        if (Input.GetKeyDown(key.GetKey("Attack")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#DAA520>무기가 없어 공격할 수 없습니다.</color>");
                return;
            }

            if (canSkill00)
            {
                AttackSk00();
            }
        }

        // 스킬 페블 샷 Sk_01
        if (Input.GetKeyDown(key.GetKey("Sk_01")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#DAA520>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_01") && skillManager.skillLevels["Sk_01"] > 0)
            {
                if (canSkill01)
                {
                    skillManager.UseSkill("Sk_01");
                }
            }
        }
        // 스킬 텔레포트 Sk_02
        if (Input.GetKeyDown(key.GetKey("Sk_02")))
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#DAA520>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_02") && skillManager.skillLevels["Sk_02"] > 0)
            {
                if (TeleportSkill.canTeleport)
                {
                    skillManager.UseSkill("Sk_02");
                }
            }
        }
        // 스킬 생명의 전환 Sk_03
        if (Input.GetKeyDown(key.GetKey("Sk_03")))
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#DAA520>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_03") && skillManager.skillLevels["Sk_03"] > 0)
            {
                if (canSkill03)
                {
                    skillManager.UseSkill("Sk_03");
                }
            }
        }
        // 스킬 스털렉타이트 Sk_04
        if (Input.GetKeyDown(key.GetKey("Sk_04")))
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#DAA520>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_04") && skillManager.skillLevels["Sk_04"] > 0)
            {
                if (canSkill04)
                {
                    skillManager.UseSkill("Sk_04");
                }
            }
        }
        // 스킬 돌개바람 Sk_05
        if (Input.GetKeyDown(key.GetKey("Sk_05")))
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#DAA520>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_05") && skillManager.skillLevels["Sk_05"] > 0)
            {
                if (canSkill05)
                {
                    skillManager.UseSkill("Sk_05");
                }
            }
        }
        // 스킬 퇴적 : 석상 Sk_06
        if (Input.GetKeyDown(key.GetKey("Sk_06")))
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#DAA520>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_06") && skillManager.skillLevels["Sk_06"] > 0)
            {
                if (rockshardManager.CurrentShardCount < 8)
                {
                    ConsoleManager.Instance.SetMessage("<color=#B8860B>퇴적 : 석상</color> 스킬을 사용하려면 <color=#DAA520>바위조각 8개</color>가 필요합니다.");
                    return;
                }

                if (canSkill06)
                {
                    skillManager.UseSkill("Sk_06");
                }
            }
        }

        foreach (var slot in QuickSlotManager.Instance.quickSlots) // 아이템 퀵슬롯
        {
            if (slot.HasItem() && Input.GetKeyDown(slot.GetAssignedKey()))
            {
                string itemCode = slot.GetAssignedItem();
                var itemData = QuickSlotManager.Instance.inventory.itemDatabase.GetItemByCode(itemCode);

                if (itemData != null)
                {
                    QuickSlotManager.Instance.inventory.UseItemDirect(itemData);
                    slot.RefreshItemAmount();
                }
            }
        }
    }

    public void PromoteMagicTierNovice() // 1차 전직 노비스
    {
        if (stats.magicTier < 1)
        {
            stats.PromoteMagicTier(1);
            ConsoleManager.Instance.SetMessage("<color=#FFD700>1차 전직 완료! 새로운 마법을 사용할 수 있게 되었습니다.</color>");
            // SkillUI 갱신
            var skillUI = FindObjectOfType<SkillUI>();
            if (skillUI != null) skillUI.UpdateSkillSlots();
        }
    }

    void Flip(float direction)
    {
        if (direction > 0)
            spineAnim.Skeleton.ScaleX = -1;
        else if (direction < 0)
            spineAnim.Skeleton.ScaleX = 1;

        float newX = (spineAnim.Skeleton.ScaleX > 0) ? -0.6f : 0.6f;
        Vector3 rangePos = Sk00Range.transform.localPosition;
        rangePos.x = newX;
        Sk00Range.transform.localPosition = rangePos;
    }

    private void ApplyTopView(bool enable)
    {
        if (enable)
        {
            rb.gravityScale = 0f;
            isGrounded = true;
            isLyingdown = false;
            Sk00Range.enabled = false;
        }
        else
        {
            rb.gravityScale = originalGravityScale;
        }
    }

    public void PlayAnimation(string baseAnim, bool loop)
    {
        string fullAnim = baseAnim;

        if (baseAnim == "Stand" || baseAnim == "Walk" || baseAnim == "Hit" || baseAnim == "Jump" || baseAnim == "Sit")
        {
            if (currentWeaponType == "Staff") fullAnim += "_Staff";
            else if (currentWeaponType == "Mace") fullAnim += "_Staff";
        }

        if (spineAnim.AnimationName != fullAnim)
        {
            spineAnim.AnimationState.SetAnimation(0, fullAnim, loop);
        }
    }
    void LyingDownCollider()
    {
        canonCollider.enabled = !isLyingdown;
        lyingCollider.enabled = isLyingdown;
    }

    public void AttackSk00()
    {
        if (!canSkill00) return;

        StartCoroutine(Skill00Coroutine());

        // Pa_05 패시브 효과 적용: 둔기+패시브 레벨>=1
        if (currentWeaponType == "Mace" && EarthManagerPassive())
        {
            Vector2 dir = spineAnim.Skeleton.ScaleX > 0 ? Vector2.left : Vector2.right;
            Vector2 spawnPosition = transform.position;
            spawnPosition.y -= 0.2f;
            if (spineAnim.Skeleton.ScaleX > 0)
            {
                // 왼쪽
                spawnPosition.x -= 1.7f;
            }
            else
            {
                // 오른쪽
                spawnPosition.x += 1.7f;
            }
            earthManager.GroundExplode(spawnPosition, dir);
        }
    }

    private IEnumerator Skill00Coroutine()
    {
        canSkill00 = false;
        usingSkill = true;

        PlayAnimation("Attack_Staff", false);
        SetMagicTemporary();

        Sk00Range.enabled = true; // 공격 범위 활성화
        SoundScript.Instance.PlaySoundEffect(20);

        // 0.9초 동안 스킬 상태 유지
        yield return new WaitForSeconds(0.9f);
        usingSkill = false;
        Sk00Range.enabled = false; // 공격 범위 비활성화

        yield return new WaitForSeconds(skill00Cooldown - 1f);
        canSkill00 = true;
    }

    public void StartSk01()
    {
        StartCoroutine(Skill01Coroutine());
        StartCoroutine(UseCanonSkill());
    }
    public IEnumerator Skill01Coroutine()
    {
        canSkill01 = false;
        usingSkill = true;

        PlayAnimation("Skill1", false);
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_01");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        Vector2 spawnPosition = transform.position;
        if (spineAnim.Skeleton.ScaleX > 0)
        {
            // 왼쪽
            spawnPosition.x -= 1.7f;
        }
        else
        {
            // 오른쪽
            spawnPosition.x += 1.7f;
        }

        GameObject Sk01 = Instantiate(skill.skillPrefab, spawnPosition, Quaternion.identity);

        Sk_01PebbleShot pebbleShot = Sk01.GetComponent<Sk_01PebbleShot>();
        if (pebbleShot != null)
        {
            pebbleShot.Initialize(skill, level, this);
            Vector2 direction = spineAnim.Skeleton.ScaleX > 0 ? Vector2.left : Vector2.right;
            pebbleShot.SetDirection(direction);
        }

        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill01 = true;
    }
    public void StartSk03()
    {
        StartCoroutine(Skill03Coroutine());
    }

    public IEnumerator Skill03Coroutine()
    {
        canSkill03 = false;
        usingSkill = true;

        PlayAnimation("Skill2", false);
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_03");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        if (LifeTransitionSkill != null)
        {
            LifeTransitionSkill.Initialize(skill, level, this);
        }

        StartCoroutine(UseCanonSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill03 = true;
    }
    public void StartSk04()
    {
        StartCoroutine(Skill04Coroutine());
    }
    public IEnumerator Skill04Coroutine()
    {
        canSkill04 = false;
        usingSkill = true;

        PlayAnimation("Skill1", false);
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_04");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        Vector2 spawnPosition = transform.position;
        spawnPosition.y += 2.3f;
        if (spineAnim.Skeleton.ScaleX > 0)
        {
            // 왼쪽
            spawnPosition.x -= 2.1f;
        }
        else
        {
            // 오른쪽
            spawnPosition.x += 2.1f;
        }

        GameObject Sk04 = Instantiate(skill.skillPrefab, spawnPosition, Quaternion.identity);

        Sk_04Stalactite stalactite = Sk04.GetComponent<Sk_04Stalactite>();
        if (stalactite != null)
        {
            stalactite.Initialize(skill, level, this);
            Vector2 direction = spineAnim.Skeleton.ScaleX > 0 ? Vector2.left : Vector2.right;
            stalactite.SetDirection(direction);
        }

        StartCoroutine(UseCanonSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill04 = true;
    }
    public void StartSk05()
    {
        StartCoroutine(Skill05Coroutine());
    }
    public IEnumerator Skill05Coroutine()
    {
        canSkill05 = false;
        usingSkill = true;

        PlayAnimation("Skill3", false);
        SetMagicTemporary();

        // 주변 바위조각 확인
        // 개선 전 범위 (플레이어위치, X=7, Y=3)
        var shards = rockshardManager.GetNearbyShards(transform.position, 9.5f, 4f);
        bool isSandstorm = shards.Count >= 6;

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_05");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        Vector2 spawnPosition = transform.position;
        spawnPosition.y += 0.7f;
        if (spineAnim.Skeleton.ScaleX > 0)
        {
            // 왼쪽
            spawnPosition.x -= 3.2f;
        }
        else
        {
            // 오른쪽
            spawnPosition.x += 3.2f;
        }

        GameObject Sk05 = Instantiate(skill.skillPrefab, spawnPosition, Quaternion.identity);

        Sk_05Whirlwind whirlwind = Sk05.GetComponent<Sk_05Whirlwind>();
        if (whirlwind != null)
        {
            Vector2 direction = spineAnim.Skeleton.ScaleX > 0 ? Vector2.left : Vector2.right;
            whirlwind.SetDirection(direction);
            if (isSandstorm)
            {
                List<GameObject> usedShards = rockshardManager.ConsumeShards(shards, 6);
                whirlwind.Initialize(skill, level, this, isSandstorm, usedShards);
            }
            else
            {
                whirlwind.Initialize(skill, level, this, false, null);
            }
        }

        StartCoroutine(UseCanonSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill05 = true;
    }
    public void StartSk06()
    {
        StartCoroutine(Skill06Coroutine());
    }
    public IEnumerator Skill06Coroutine()
    {
        canSkill06 = false;
        usingSkill = true;

        PlayAnimation("Skill2", false);
        SetMagicTemporary();

        // 주변 바위조각 확인
        // 개선 전 범위 (플레이어위치, X=7, Y=3)
        var shards = rockshardManager.GetNearbyShards(transform.position, 9.5f, 4f);

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_06");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        Vector2 spawnPosition = transform.position;
        spawnPosition.y += 0.1f;
        if (spineAnim.Skeleton.ScaleX > 0)
        {
            // 왼쪽
            spawnPosition.x -= 3f;
        }
        else
        {
            // 오른쪽
            spawnPosition.x += 3f;
        }

        GameObject Sk06 = Instantiate(skill.skillPrefab, spawnPosition, Quaternion.identity);

        Sk_06Statue statue = Sk06.GetComponent<Sk_06Statue>();
        List<GameObject> usedShards = rockshardManager.ConsumeShards(shards, 8);
        if (statue != null)
        {
            statue.Initialize(skill, level, this, usedShards);
            Vector2 direction = spineAnim.Skeleton.ScaleX > 0 ? Vector2.left : Vector2.right;
            statue.SetDirection(direction);
        }

        StartCoroutine(UseCanonSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill06 = true;
    }
    private IEnumerator UseCanonSkill()
    {
        yield return new WaitForSeconds(1f); // 이펙트 재생 시간 등
        usingSkill = false;
    }

    public bool EarthManagerPassive()
    {
        int pa05Level = SkillManager.Instance?.GetSkillLevel("Pa_05") ?? 0;
        return pa05Level > 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isTopView) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            if (storyMode) return;

            // 땅에 닿았을 때 애니메이션 복원
            float move = Input.GetAxis("Horizontal");
            if (isLyingdown) return;
            if (Mathf.Abs(move) > 0.1f)
            {
                PlayAnimation("Walk", true);
            }
            else
            {
                PlayAnimation("Stand", true);
            }
        }
    }

    private void OnEquipmentChanged(EquipSlot slot, ItemData item)
    {
        if (slot == EquipSlot.Weapon)
        {
            UpdateWeaponVisuals(item);
        }
    }
    public void UpdateWeaponVisuals(ItemData weaponItem)
    {
        if (weaponItem == null)
        {
            spineAnim.Skeleton.SetAttachment("HandSlot", null);
            currentWeaponType = "None";
            return;
        }

        string code = weaponItem.itemCode.ToLower();
        spineAnim.Skeleton.SetAttachment("HandSlot", code);

        currentWeaponType = code.StartsWith("staff") ? "Staff" :
                            code.StartsWith("mace") ? "Mace" : "None";
    }

    public void SetMagicTemporary()
    {
        if (playerEquipment.weaponSlot.equippedItem == null) return;

        string code = playerEquipment.weaponSlot.equippedItem.itemCode.ToLower();
        string magicCode = code + "_magic";

        StartCoroutine(SwapMagicAttachment(code, magicCode, 3.5f)); // 3.5초 동안 유지
    }

    private IEnumerator SwapMagicAttachment(string normal, string magic, float duration)
    {
        spineAnim.Skeleton.SetAttachment("HandSlot", magic);
        yield return new WaitForSeconds(duration);
        spineAnim.Skeleton.SetAttachment("HandSlot", normal);
    }

    void SetLayerAll(int layer)
    {
        gameObject.layer = layer;
    }

    private IEnumerator DropPlatform()
    {
        if (isDropping) yield break;
        isDropping = true;

        // Dropping 전환
        SetLayerAll(droppingLayer);
        rb.velocity = new Vector2(rb.velocity.x, -3f);

        // 0.4초 후 원래 레이어로 복구
        yield return new WaitForSeconds(0.4f);

        // 무적(피격) 중이면 끝날 때까지 대기
        while (hitting) yield return null;

        isDropping = false;
        SetLayerAll(canonLayer);
    }

    public void TakeDamage(int damage, ElementType elementType, DamageType damageType)
    {
        if (isDead || hitting) return;

        float isDodge = Random.value; // 0 ~ 1
        if (isDodge < stats.DodgeChance)
        {
            if (damageUIManager != null)
            {
                damageUIManager.ShowDodge(transform.position);
            }
            hitting = true;
            // 피격 중엔 충돌 무시 레이어로
            gameObject.layer = invisibleLayer;
            StartCoroutine(ResetDamaged());
            return;
        }

        hitting = true;
        // 피격 중엔 충돌 무시 레이어로
        gameObject.layer = invisibleLayer;

        // 데미지 처리
        stats.TakeDamage(damage, elementType, damageType);
        // 데미지 UI 표시
        if (damageUIManager != null)
            damageUIManager.ShowDamage(transform.position, stats.finalDamage, elementType);

        if (!isLyingdown)
        {
            PlayAnimation("Hit", false); // Spine Hit 
        }
        spineAnim.Skeleton.SetColor(new Color(0.7f, 0.7f, 0.7f)); // 피격 모션 동안 어두워짐

        if (stats.currentHP <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(ResetDamaged());
        }
    }
    public void Knockback(Vector2 direction, float power) // 넉백
    {
        if (rb == null || isDead) return;

        float KnockbackPower = stats.CalculateKnockback(power);
        if (KnockbackPower >= 2f)
        {
            knockback = true;
            StartCoroutine(Resetkb());
        }
        rb.AddForce(direction * KnockbackPower, ForceMode2D.Impulse);
    }
    public void Die()
    {
        isDead = true;
        isDropping = false;
        gameObject.layer = canonLayer;

        PlayAnimation("Die", false);

        canonCollider.enabled = false;
        lyingCollider.enabled = false;

        if (!isLyingdown)
        {
            Vector3 diePos = gameObject.transform.localPosition;
            diePos.y -= 0.7f;
            gameObject.transform.localPosition = diePos;
        }

        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // 물리 상호작용 중단

        // 부활 UI
        PlayerUIScript playerUI = FindObjectOfType<PlayerUIScript>();
        if (playerUI != null)
        {
            playerUI.ShowReviveUI();
        }
        SoundScript.Instance?.PlaySoundEffect(1);
    }
    public void RespawnPlayer()
    {
        LifeTransitionSkill.DeactivateBuff();
        stats.currentHP = Mathf.Max(1, Mathf.RoundToInt(stats.maxHP * 0.1f));
        stats.currentMP = Mathf.Max(1, Mathf.RoundToInt(stats.maxMP * 0.1f));

        rb.isKinematic = false;
        canonCollider.enabled = true;
        lyingCollider.enabled = false;
        isDead = false;
        isDropping = false;
        gameObject.layer = canonLayer;

        PlayAnimation("Stand", true);
        StartCoroutine(ResetDamaged());
        StartCoroutine(Resetkb());
    }

    // 매직 라이프 사용 제자리 부활
    public void MagicLifeRespawn()
    {
        // 체력 / 마나 회복
        stats.currentHP = stats.maxHP;
        stats.currentMP = stats.maxMP;

        // 상태 복구
        rb.isKinematic = false;
        canonCollider.enabled = true;
        lyingCollider.enabled = false;
        isDead = false;
        isDropping = false;
        knockback = false;
        hitting = false;

        gameObject.layer = canonLayer;

        spineAnim.Skeleton.SetColor(Color.white);
        PlayAnimation("Stand", true);
    }
    private IEnumerator ResetDamaged()
    {
        yield return new WaitForSeconds(hitCooldown); // 1.2초 동안 피격 상태 유지
        hitting = false;
        spineAnim.Skeleton.SetColor(Color.white);
        // 레이어 복원
        SetLayerAll(isDropping ? droppingLayer : canonLayer);
    }
    private IEnumerator Resetkb()
    {
        float kbResistFactor = Mathf.Clamp01(1f - (stats.KnockbackResistance / 100f));
        float FinalkbCooldown = kbCooldown * kbResistFactor;
        yield return new WaitForSeconds(FinalkbCooldown);
        knockback = false;
    }

    public void LevelUpEffect(int newLevel)
    {
        SoundScript.Instance.PlaySoundEffect(27);
        // 파티클
        LevelUpParticle.SetActive(true);
        LevelUpParticle.GetComponent<ParticleSystem>().Play();
        StartCoroutine(DeactivateAfter(LevelUpParticle, 1.5f)); // 파티클 길이

        // 레벨 텍스트
        StatusText.SetActive(true);
        levelupText.text = $"Lv.{newLevel}";
        StartCoroutine(FadeOutText(levelupText, 2.5f));
    }

    private IEnumerator DeactivateAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    private IEnumerator FadeOutText(TextMeshProUGUI text, float duration)
    {
        float time = 0;
        Color originalColor = text.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time / duration);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        text.color = originalColor;
        StatusText.SetActive(false);
    }

    // 버프 및 디버프
    public void ApplySlow(float SlowRatio)
    {
        stats.MoveSpeed *= SlowRatio; // 이동 속도 감소
    }

    public void RemoveSlow(float SlowRatio)
    {
        stats.MoveSpeed /= SlowRatio; // 속도 복원
    }
    public void ApplyOsmoticDebuff()
    {
        if (isOsmotic) return;
        isOsmotic = true;
        SoundScript.Instance.PlaySoundEffect(51);

        // 스탯 백업
        savedJumpForce = stats.JumpForce;

        stats.JumpForce *= 0.5f;

        if (osmoticCoroutine != null)
            StopCoroutine(osmoticCoroutine);

        osmoticCoroutine = StartCoroutine(OsmoticDot());
    }

    public void RemoveOsmoticDebuff()
    {
        if (!isOsmotic) return;
        isOsmotic = false;

        // 스탯 복원
        stats.JumpForce = savedJumpForce;

        if (osmoticCoroutine != null)
            StopCoroutine(osmoticCoroutine);
        osmoticCoroutine = null;
    }

    // 초당 HP/MP 3% 감소
    private IEnumerator OsmoticDot()
    {
        float tick = 1f;
        int duration = 5; // 5초간 반복
        for (int i = 0; i < duration; i++)
        {
            int hpLoss = Mathf.Max(1, Mathf.RoundToInt(stats.maxHP * 0.03f));
            int mpLoss = Mathf.Max(1, Mathf.RoundToInt(stats.maxMP * 0.03f));

            stats.currentHP = Mathf.Max(stats.currentHP - hpLoss, 1);
            stats.currentMP = Mathf.Max(stats.currentMP - mpLoss, 0);

            yield return new WaitForSeconds(tick);
        }
    }

    // ISkillUser 인터페이스 구현
    public CharacterStats GetStats()
    {
        return stats;
    }

    public void UseSkill(string skillCode, int level)
    {
        // 실제 스킬 사용 로직은 여기에 추가
        Debug.Log($"[Canon] 스킬 사용: {skillCode}, 레벨: {level}");
    }

    public void PlaySkillAnimation(string animName)
    {
        // PlayAnimation(animName, false);
    }

    public void UseMP(int amount)
    {
        stats.UseMP(amount);
    }

    public Transform GetTransform()
    {
        return this.transform;
    }

    public void SaveRaidExitState()
    {
        string sceneToSave = SceneManager.GetActiveScene().name;
        Vector3 posToSave = transform.position;

        PlayerUIScript ui = FindObjectOfType<PlayerUIScript>();

        if (ui != null && ui.isBossGround)
        {
            sceneToSave = ui.respawnMap;
            posToSave = ui.respawnPosition;

            PlayerPrefs.SetInt("LastRaidFailed", 1);

            MagicLifeManager magicLife = FindObjectOfType<MagicLifeManager>();
            if (magicLife != null && !string.IsNullOrEmpty(magicLife.raidFailMessage))
            {
                PlayerPrefs.SetString("Console_RaidFail", magicLife.raidFailMessage);
            }
        }

        SavePlayer.SavePlayerState(sceneToSave, posToSave);
        PlayerPrefs.Save();
    }

    void OnApplicationQuit() // 게임 종료 시 데이터 저장
    {
        LifeTransitionSkill.DeactivateBuff();

        SaveRaidExitState();
        // 스탯 정보 저장
        stats.SaveStats();
        // 인벤토리 장비창 데이터 저장
        // playerInventory.SaveInventory();
        playerEquipment.SaveEquipment();
    }
    void OnDisable() // 씬 이동 시
    {
        LifeTransitionSkill.DeactivateBuff();
        stats.SaveStats();
        // 인벤토리 장비창 데이터
        playerInventory.SaveInventory();
        playerEquipment.SaveEquipment();
    }
}