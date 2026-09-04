using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MagicDewManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Slider dewSlider; // 마법 이슬 슬라이더
    public Image dewImage; // 마법 이슬 UI 이미지
    public Sprite normalSprite; // 0 ~ 99%
    public Sprite fullSprite; // 100%
    public TextMeshProUGUI statInfo;
    public Image rainIcon; // 레인 표정 아이콘
    public Sprite[] rainExpressions; // 환경에 따른 표정 배열
    public TextMeshProUGUI magicDewCondition; // 레인 대사
    public GameObject Description; // 설명 오브젝트
    public GameObject[] dewParticles; // 파티클

    int _maxDew = 100;
    int _currentDew = 0;

    public void SetMaxDew(int value)
    {
        _maxDew = value;
        dewSlider.maxValue = 100;
    }
    public void SetDewValue(int dew)
    {
        _currentDew = Mathf.Clamp(dew, 0, _maxDew);
        float percent = _maxDew > 0 ? (float)_currentDew / _maxDew * 100f : 0f;
        dewSlider.value = percent;
        dewImage.sprite = (_currentDew >= _maxDew) ? fullSprite : normalSprite;
        UpdateParticles(_currentDew);
        UpdateRainIcon(_currentDew);
    }

    void UpdateParticles(int dew)
    {
        float percent = (_maxDew > 0) ? ((float)dew / _maxDew) : 0f;

        if (dewParticles.Length > 0 && dewParticles[0] != null)
            dewParticles[0].SetActive(percent >= 0.5f && percent < 1.0f);

        if (dewParticles.Length > 1 && dewParticles[1] != null)
            dewParticles[1].SetActive(percent >= 1.0f);
    }

    void UpdateRainIcon(int dew)
    {
        int idx = dew >= _maxDew ? 2 : dew >= 50 ? 1 : 0;
        if (rainIcon != null && rainExpressions.Length > idx)
            rainIcon.sprite = rainExpressions[idx];
    }

    // dew: 현재 이슬, maxDew: 최대 이슬
    // bonusMagic, bonusInt, bonusWaterPow, bonusWaterResist: 보정치
    public void UpdateStatInfo(int dew, int maxDew, int bonusMagic, int bonusInt, float bonusWaterPower, float bonusWaterResist)
    {
        float percent = maxDew > 0 ? (float)dew / maxDew * 100f : 0f;
        statInfo.text =
            $"<color=#00BFFF>이슬 충전량</color>: {dew} / {maxDew} ({percent:F0}%)\n" +
            $"마력 +{bonusMagic}\n" +
            $"<color=#00BFFF>물 속성 강화</color> +{bonusWaterPower * 100}%\n" +
            $"<color=#00BFFF>물 속성 내성</color> +{bonusWaterResist * 100}%";
    }

    public void SetMagicDewCondition(string message)
    {
        magicDewCondition.text = message;
    }

    public void ShowDescription(bool show)
    {
        if (Description != null)
            Description.SetActive(show);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 point = Camera.main.ScreenToWorldPoint(new Vector2(Input.mousePosition.x,
Input.mousePosition.y - 60f));
        Description.transform.position = point;
        ShowDescription(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ShowDescription(false);
    }
}
