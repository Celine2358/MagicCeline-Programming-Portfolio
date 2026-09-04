using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Spine.Unity;
using Spine;

public class RainControl : MonoBehaviour, IDamageable, IKnockbackable, ISkillUser
{
    public SkeletonAnimation spineAnim;
    private Rigidbody2D rb;
    public Collider2D rainCollider;
    public Collider2D lyingCollider; // 엎드리기 충돌 범위

    int rainLayer;
    int droppingLayer;
    int invisibleLayer;
    bool isDropping = false; // 아래점프 중인지

    public bool storyMode = false; // 스토리 연출 중

    // 레인의 Damage 출력
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
    public float rainMagicConstant = 1.07f;
    private SkillManager skillManager;
    public GameObject Sk10Prefab; // 기본 공격 투사체
    public bool usingSkill = false;
    public bool canSkill10 = true;
    public float skill10Cooldown = 1.5f; // 기본 공격 쿨타임

    public bool canSkill11 = true;
    public Sk_12 TeleportSkill;
    public Sk_13WaterBlessing waterblessingSkill;
    public bool canSkill13 = true;
    public bool canSkill14 = true;
    public Sk_15Dewshield dewshieldSkill;
    public bool canSkill15 = true;
    public bool canSkill16 = true;

    [Header("마법 이슬 시스템")]
    public MagicDewManager magicDewManager;
    public int magicDew = 0;
    private bool IsMagicDewUnlocked
    {
        get
        {
            int Pa12Level = SkillManager.Instance?.GetSkillLevel("Pa_12") ?? 0;
            return Pa12Level >= 1;
        }
    }

    private int maxMagicDew
    {
        get
        {
            int Pa12Level = SkillManager.Instance?.GetSkillLevel("Pa_12") ?? 0;
            return Pa12Level switch
            {
                0 => 0,
                1 => 100,
                2 => 150,
                3 => 200,
                _ => 100 + (Pa12Level - 1) * 50
            };
        }
    }
    public int magicDewTier = 0;
    public void RefreshMagicDewSystem() // 마법 이슬 시스템 총괄
    {
        if (IsMagicDewUnlocked)
        {
            magicDewManager.gameObject.SetActive(true);
            magicDew = Mathf.Clamp(magicDew, 0, maxMagicDew);
            magicDewManager.SetMaxDew(maxMagicDew);
            UpdateMagicDewStat();
            if (autoChargeCoroutine == null) autoChargeCoroutine = StartCoroutine(AutoChargeMagicDew());
        }
        else
        {
            if (autoChargeCoroutine != null)
            {
                StopCoroutine(autoChargeCoroutine);
                autoChargeCoroutine = null;
            }
            magicDewManager.gameObject.SetActive(false);
            magicDew = 0;
            UpdateMagicDewStat();
        }
    }
    // 필드에 코루틴 핸들러 변수 추가
    private Coroutine autoChargeCoroutine;

    // 마법 이슬 스탯 보정용 임시 변수
    int dewBonusMagic = 0, dewBonusInt = 0;
    float dewBonusWaterPower = 0, dewBonusWaterResist = 0;

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
        LoadMagicDew();

        rainLayer = gameObject.layer;
        droppingLayer = LayerMask.NameToLayer("Dropping");
        invisibleLayer = LayerMask.NameToLayer("Invisible");
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lyingCollider.enabled = false; // 엎드리기 충돌 범위 비활성화

        damageUIManager = FindObjectOfType<DamageUIManager>();

        // 레벨 업

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

        // 마법 이슬 관련
        RefreshMagicDewSystem();
        magicDewManager.SetMaxDew(maxMagicDew);
        magicDew = Mathf.Clamp(magicDew, 0, maxMagicDew);
        UpdateMagicDewStat();

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
        float RainMove = isLyingdown ? 0 : Input.GetAxis("Horizontal");

        if (!isLyingdown)
        {
            rb.velocity = new Vector2(RainMove * stats.MoveSpeed, rb.velocity.y);

            // 이동 방향이 있을 때만 Flip 실행
            if (Mathf.Abs(RainMove) > 0.01f)
            {
                Flip(RainMove);
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
        else if (RainMove != 0 && !hitting)
        {
            PlayAnimation("Walk", true);
        }
        else if (!hitting)
        {
            PlayAnimation("Stand", true);
        }

        // 점프
        if (Input.GetKeyDown(key.GetKey("Jump")) && isGrounded && !isLyingdown 
            && currentWeaponType != "Mace")
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

        // 기본 공격 Sk_10
        if (Input.GetKeyDown(key.GetKey("Attack")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (canSkill10)
            {
                ShotSk10();
            }
        }
        // 스킬 마법 거품 Sk_11
        if (Input.GetKeyDown(key.GetKey("Sk_11")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_11") && skillManager.skillLevels["Sk_11"] > 0)
            {
                if (canSkill11)
                {
                    skillManager.UseSkill("Sk_11");
                }
            }
        }
        // 스킬 텔레포트 Sk_12
        if (Input.GetKeyDown(key.GetKey("Sk_12")))
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_12") && skillManager.skillLevels["Sk_12"] > 0)
            {
                if (TeleportSkill.canTeleport)
                {
                    skillManager.UseSkill("Sk_12");
                }
            }
        }
        // 스킬 물의 은혜 Sk_13
        if (Input.GetKeyDown(key.GetKey("Sk_13")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_13") && skillManager.skillLevels["Sk_13"] > 0)
            {
                int level = skillManager.skillLevels["Sk_13"];
                SkillData skill = skillManager.skillDatabase.GetSkillByCode("Sk_13");
                int dewCost = skill.GetDewCost(level);

                if (magicDew < dewCost)
                {
                    ConsoleManager.Instance.SetMessage("<color=#6fd9ff>마법에 필요한 마법 이슬이 부족합니다!</color>");
                    return;
                }

                if (canSkill13)
                {
                    skillManager.UseSkill("Sk_13");
                }
            }
        }
        // 스킬 이슬 세례 Sk_14
        if (Input.GetKeyDown(key.GetKey("Sk_14")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_14") && skillManager.skillLevels["Sk_14"] > 0)
            {
                if (canSkill14)
                {
                    skillManager.UseSkill("Sk_14");
                }
            }
        }
        // 스킬 이슬 보호막 Sk_15
        if (Input.GetKeyDown(key.GetKey("Sk_15")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_15") && skillManager.skillLevels["Sk_15"] > 0)
            {
                int level = skillManager.skillLevels["Sk_15"];
                SkillData skill = skillManager.skillDatabase.GetSkillByCode("Sk_15");
                int dewCost = skill.GetDewCost(level);

                if (magicDew < dewCost)
                {
                    ConsoleManager.Instance.SetMessage("<color=#6fd9ff>마법에 필요한 마법 이슬이 부족합니다!</color>");
                    return;
                }

                if (canSkill15)
                {
                    skillManager.UseSkill("Sk_15");
                }
            }
        }
        // 스킬 물의 분노 Sk_16
        if (Input.GetKeyDown(key.GetKey("Sk_16")) && !isLyingdown)
        {
            if (currentWeaponType == "None")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>무기가 없어 마법을 사용할 수 없습니다.</color>");
                return;
            } else if (currentWeaponType == "Mace")
            {
                ConsoleManager.Instance.SetMessage("<color=#FF4500>둔기로는 이 마법을 사용할 수 없습니다.</color>");
                return;
            }

            if (skillManager.skillLevels.ContainsKey("Sk_16") && skillManager.skillLevels["Sk_16"] > 0)
            {
                int level = skillManager.skillLevels["Sk_16"];
                SkillData skill = skillManager.skillDatabase.GetSkillByCode("Sk_16");
                int dewCost = skill.GetDewCost(level);

                if (magicDew < dewCost)
                {
                    ConsoleManager.Instance.SetMessage("<color=#6fd9ff>마법에 필요한 마법 이슬이 부족합니다!</color>");
                    return;
                }

                if (canSkill16)
                {
                    skillManager.UseSkill("Sk_16");
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

        if (IsMagicDewUnlocked)
        {
            magicDewManager.SetDewValue(magicDew);
            magicDewManager.UpdateStatInfo
                (magicDew, maxMagicDew, dewBonusMagic, dewBonusInt, dewBonusWaterPower, dewBonusWaterResist);
        }
    }

    public void PromoteMagicTierNovice() // 1차 전직 노비스
    {
        if (stats.magicTier < 1)
        {
            stats.PromoteMagicTier(1);
            ConsoleManager.Instance.SetMessage("<color=#0099ff>1차 전직 완료! 새로운 마법을 사용할 수 있게 되었습니다.</color>");
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
    }

    private void ApplyTopView(bool enable)
    {
        if (enable)
        {
            rb.gravityScale = 0f;
            isGrounded = true;
            isLyingdown = false;
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
            else if (currentWeaponType == "Mace") fullAnim += "_Mace";
        }

        if (spineAnim.AnimationName != fullAnim)
        {
            spineAnim.AnimationState.SetAnimation(0, fullAnim, loop);
        }
    }

    void LyingDownCollider()
    {
        rainCollider.enabled = !isLyingdown;
        lyingCollider.enabled = isLyingdown;
    }
    public void ShotSk10()
    {
        if (Sk10Prefab == null) return;
        if (!canSkill10) return;

        StartCoroutine(Skill10Coroutine());
    }
    private IEnumerator Skill10Coroutine()
    {
        canSkill10 = false;
        usingSkill = true;

        if (currentWeaponType == "Mace")
            PlayAnimation("Skill2", false); // 둔기 전용 공격
        else
            PlayAnimation("Skill1", false); // 스태프 등 기본 공격
        SetMagicTemporary();

        // 바라보는 방향에 따라 위치 조정
        Vector2 spawnPosition = transform.position;
        spawnPosition.x += spineAnim.Skeleton.ScaleX > 0 ? -1.6f : 1.6f;

        GameObject Sk10 = Instantiate(Sk10Prefab, spawnPosition, Quaternion.identity);
        Sk_10Attack sk10script = Sk10.GetComponent<Sk_10Attack>();
        if (sk10script != null)
        {
            sk10script.rain = this;
        }

        // 1초 동안 스킬 상태 유지
        yield return new WaitForSeconds(1f);
        usingSkill = false;

        yield return new WaitForSeconds(skill10Cooldown - 1f);
        canSkill10 = true;
    }

    public void StartSk11()
    {
        StartCoroutine(Skill11Coroutine());
        StartCoroutine(UseRainSkill());
    }
    public IEnumerator Skill11Coroutine()
    {
        canSkill11 = false;
        usingSkill = true;

        if (currentWeaponType == "Mace")
            PlayAnimation("Skill2", false); // 둔기 전용 공격
        else
            PlayAnimation("Skill1", false); // 스태프 등 기본 공격
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_11");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        Vector2 spawnPosition = transform.position;
        if (spineAnim.Skeleton.ScaleX > 0)
        {
            // 왼쪽
            spawnPosition.x -= 2f;
        }
        else
        {
            // 오른쪽
            spawnPosition.x += 2f;
        }

        GameObject Sk11 = Instantiate(skill.skillPrefab, spawnPosition, Quaternion.identity);

        Sk_11Bubble bubble = Sk11.GetComponent<Sk_11Bubble>();
        if (bubble != null)
        {
            bubble.Initialize(skill, level, this);
            Vector2 direction = spineAnim.Skeleton.ScaleX > 0 ? Vector2.left : Vector2.right;
            bubble.SetDirection(direction);
        }

        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill11 = true;
    }
    public void StartSk13()
    {
        StartCoroutine(Skill13Coroutine());
    }

    public IEnumerator Skill13Coroutine()
    {
        canSkill13 = false;
        usingSkill = true;

        PlayAnimation("Skill2", false);
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_13");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        if (waterblessingSkill != null)
        {
            waterblessingSkill.Initialize(skill, level, this);
        }

        StartCoroutine(UseRainSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill13 = true;
    }

    public void StartSk14()
    {
        StartCoroutine(Skill14Coroutine());
    }
    public IEnumerator Skill14Coroutine()
    {
        canSkill14 = false;
        usingSkill = true;

        if (currentWeaponType == "Mace")
            PlayAnimation("Skill2", false); // 둔기 전용 공격
        else
            PlayAnimation("Skill1", false); // 스태프 등 기본 공격
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_14");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        Vector2 spawnPosition = transform.position;
        if (spineAnim.Skeleton.ScaleX > 0) // 왼쪽
        {
            spawnPosition.x -= 2.2f;
        }
        else // 오른쪽
        {
            spawnPosition.x += 2.2f;
        }

        GameObject Sk14 = Instantiate(skill.skillPrefab, spawnPosition, Quaternion.identity);

        Sk_14Dewfall dewfall = Sk14.GetComponent<Sk_14Dewfall>();
        if (dewfall != null)
        {
            dewfall.Initialize(skill, level, this);
        }

        StartCoroutine(UseRainSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill14 = true;
    }
    public void StartSk15()
    {
        StartCoroutine(Skill15Coroutine());
    }

    public IEnumerator Skill15Coroutine()
    {
        canSkill15 = false;
        usingSkill = true;

        if (currentWeaponType == "Mace")
            PlayAnimation("Skill2", false); // 둔기 전용 공격
        else
            PlayAnimation("Skill4", false); // 스태프 등 기본 공격
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_15");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        if (dewshieldSkill != null)
        {
            dewshieldSkill.Initialize(skill, level, this);
        }

        StartCoroutine(UseRainSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill15 = true;
    }

    public void StartSk16()
    {
        StartCoroutine(Skill16Coroutine());
    }
    public IEnumerator Skill16Coroutine()
    {
        canSkill16 = false;
        usingSkill = true;

        PlayAnimation("Skill3", false); // 스태프 등 기본 공격
        SetMagicTemporary();

        SkillData skill = SkillManager.Instance.skillDatabase.GetSkillByCode("Sk_16");
        int level = SkillManager.Instance.skillLevels.ContainsKey(skill.skillCode)
            ? SkillManager.Instance.skillLevels[skill.skillCode]
            : 1;

        Vector2 spawnPosition = transform.position;
        spawnPosition.y += 1f;

        GameObject Sk16 = Instantiate(skill.skillPrefab, spawnPosition, Quaternion.identity);

        Sk_16WrathWater wrathwater = Sk16.GetComponent<Sk_16WrathWater>();
        if (wrathwater != null)
        {
            wrathwater.Initialize(skill, level, this);
        }

        StartCoroutine(UseRainSkill());
        float cooldownTime = skill.GetCooldown(level);
        yield return new WaitForSeconds(cooldownTime);
        canSkill16 = true;
    }
    private IEnumerator UseRainSkill()
    {
        yield return new WaitForSeconds(1f); // 이펙트 재생 시간 등
        usingSkill = false;
    }

    // 마법 이슬 기능 함수들
    IEnumerator AutoChargeMagicDew()
    {
        float dewTimer = 0f;
        float mpTimer = 0f;

        bool canChargeDew = true;

        while (IsMagicDewUnlocked)
        {
            yield return null;
            dewTimer += Time.deltaTime;
            mpTimer += Time.deltaTime;

            // MP는 5초마다 소모
            if (mpTimer >= 5f)
            {
                if (magicDewTier > 0 && magicDew < maxMagicDew)
                {
                    int mpDrain = Mathf.CeilToInt(stats.maxMP * 0.01f) + 12;
                    if (stats.currentMP >= mpDrain)
                    {
                        stats.UseMP(mpDrain);
                        canChargeDew = true;
                    }
                    else
                    {
                        canChargeDew = false;
                    }
                }
                mpTimer = 0f;
            }

            // 이슬은 3초마다 증가, 단 canChargeDew가 true일 때만
            if (dewTimer >= 3f)
            {
                if (canChargeDew && magicDewTier > 0 && magicDew < maxMagicDew)
                {
                    magicDew = Mathf.Min(magicDew + magicDewTier, maxMagicDew);
                    UpdateMagicDewStat();
                }
                dewTimer = 0f;
            }
        }
    }

    public void UpdateMagicDewStat()
    {
        RemoveMagicDewStat();
        dewBonusMagic = magicDew / 10;
        dewBonusWaterPower = dewBonusWaterResist = 0f;
        if (magicDew >= 50)
        {
            int dewForWater = (magicDew - 50) / 10 + 1;
            dewBonusWaterPower = dewBonusWaterResist = dewForWater * 0.01f;
        }
        stats.Magic += dewBonusMagic;
        stats.WaterElementPower += dewBonusWaterPower;
        stats.WaterElementResist += dewBonusWaterResist;
    }

    void RemoveMagicDewStat()
    {
        if (dewBonusMagic != 0) stats.Magic -= dewBonusMagic;
        if (dewBonusWaterPower != 0f) stats.WaterElementPower -= dewBonusWaterPower;
        if (dewBonusWaterResist != 0f) stats.WaterElementResist -= dewBonusWaterResist;

        dewBonusMagic = 0;
        dewBonusWaterPower = dewBonusWaterResist = 0f;
    }
    public bool UseMagicDew(int amount)
    {
        if (magicDew >= amount)
        {
            magicDew -= amount;
            UpdateMagicDewStat();
            return true;
        }
        return false;
    }
    public void SetMagicDewTier(int tier)
    {
        magicDewTier = Mathf.Clamp(tier, 0, 5);

        // 대사 출력
        string msg = tier switch
        {
            0 => "<color=#b8e3ff>여기는 이슬이<br>모이지 않는 지역이야...</color>",
            1 => "고요히 이슬이 모여요.",
            2 => "이슬이 천천히<br>차오르고 있어요.",
            3 => "이슬이 제법 모이네요!",
            4 => "<color=#66bfff>이슬이 빠르게<br>차오르고 있어요!</color>",
            5 => "<color=#42c4ff>이런 곳은 드물죠!<br>이슬이 넘쳐나요!</color>",
            _ => "null"
        };
        if (magicDewManager != null)
            magicDewManager.SetMagicDewCondition(msg);
    }

    public void GainMagicDew(int amount) // 마법 이슬을 획득하는 스킬 사용
    {
        if (!IsMagicDewUnlocked) return;
        magicDew = Mathf.Min(magicDew + amount, maxMagicDew);
        UpdateMagicDewStat();
    }
    public void SaveMagicDew()
    {
        PlayerPrefs.SetInt("Rain_MagicDew", magicDew);
        PlayerPrefs.Save();
    }

    public void LoadMagicDew()
    {
        magicDew = PlayerPrefs.GetInt("Rain_MagicDew", 0);
        if (magicDewManager != null)
            magicDewManager.SetDewValue(magicDew);
        UpdateMagicDewStat();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
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
        SetLayerAll(rainLayer);
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
        gameObject.layer = rainLayer;

        PlayAnimation("Die", false);

        rainCollider.enabled = false;
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
        stats.currentHP = Mathf.Max(1, Mathf.RoundToInt(stats.maxHP * 0.1f));
        stats.currentMP = Mathf.Max(1, Mathf.RoundToInt(stats.maxMP * 0.1f));

        rb.isKinematic = false;
        rainCollider.enabled = true;
        lyingCollider.enabled = false;
        isDead = false;
        isDropping = false;
        gameObject.layer = rainLayer;

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
        rainCollider.enabled = true;
        lyingCollider.enabled = false;
        isDead = false;
        isDropping = false;
        knockback = false;
        hitting = false;

        gameObject.layer = rainLayer;

        spineAnim.Skeleton.SetColor(Color.white);
        PlayAnimation("Stand", true);
    }

    private IEnumerator ResetDamaged()
    {
        yield return new WaitForSeconds(hitCooldown); // 1.2초 동안 피격 상태 유지
        hitting = false;
        spineAnim.Skeleton.SetColor(Color.white);
        // 레이어 복원
        SetLayerAll(isDropping ? droppingLayer : rainLayer);
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
        Debug.Log($"[Rain] 스킬 사용: {skillCode}, 레벨: {level}");
    }

    public void PlaySkillAnimation(string animName)
    {
        PlayAnimation(animName, false);
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
        dewshieldSkill.EndDewShieldEffect();

        SaveRaidExitState();

        // 스탯 정보 저장
        stats.SaveStats();
        // 인벤토리 장비창 데이터 저장
        // playerInventory.SaveInventory();
        playerEquipment.SaveEquipment();
        SaveMagicDew();
    }
    void OnDisable() // 씬 이동 시
    {
        dewshieldSkill.EndDewShieldEffect();
        RemoveMagicDewStat();
        stats.SaveStats();
        // 인벤토리 장비창 데이터 저장
        playerInventory.SaveInventory();
        playerEquipment.SaveEquipment();
        SaveMagicDew();
    }
}