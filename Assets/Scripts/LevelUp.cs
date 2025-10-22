using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelUp : MonoBehaviour
{
    RectTransform rect;
    Item[] items;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        items = GetComponentsInChildren<Item>(true);
    }

    public void Show()
    {
        Next();
        rect.localScale = Vector3.one;
        GameManager.instance.Stop();
        AudioManager.instance.EffectBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
    }

    public void Hide()
    {
        rect.localScale = Vector3.zero;
        GameManager.instance.Resume();
        AudioManager.instance.EffectBgm(false);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }

    public void Select(int index)
    {
        items[index].OnClick();
    }

    void Next()
    {
        foreach (Item item in items)
        {
            item.gameObject.SetActive(false);
        }

        int[] ran = new int[3];
        HashSet<int> pickedIndices = new HashSet<int>();
        // 3개의 유니크한 숫자가 뽑힐 때까지 반복합니다.
        while (pickedIndices.Count < 3)
        {
            // 0~4 사이의 숫자를 뽑아서 HashSet에 추가 시도
            pickedIndices.Add(Random.Range(0, items.Length - 1));
        }

        // HashSet을 배열로 변환
        ran = pickedIndices.ToArray();

        for (int i = 0; i < ran.Length; i++)
        {
            Item ranItem = items[ran[i]];

            if (ranItem.level == ranItem.data.damages.Length)
            {
                items[4].gameObject.SetActive(true);
            }
            else
            {
                ranItem.gameObject.SetActive(true);
            }
        }
    }
}
