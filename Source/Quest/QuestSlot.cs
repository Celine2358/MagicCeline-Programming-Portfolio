using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestSlot : MonoBehaviour
{
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI storyNameText;
    public GameObject clearImage; // 완료 시 활성화할 이미지

    public QuestData questData;

    public void Setup(QuestData quest)
    {
        questData = quest;
        questNameText.text = quest.questTitle;
        storyNameText.text = string.IsNullOrEmpty(quest.storyName) ? "" : quest.storyName;
        clearImage.SetActive(quest.isCompleted);
    }

    public void OnClick()
    {
        uiClickSound();
        QuestManager.Instance.ShowQuestDetails(questData);
    }
    public void uiClickSound()
    {
        if (SoundScript.Instance != null)
        {
            SoundScript.Instance.PlaySoundEffect(0); // SoundManager의 Soundlist 0번 (uiClick)
        }
        else
        {
            Debug.LogWarning("SoundManager 오류");
        }
    }
}