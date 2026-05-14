using System.Collections;
using UnityEngine;
using TMPro;

public class SkinChooser : MonoBehaviour
{
    [SerializeField] private TMP_InputField nickField;
    [SerializeField] private TMP_Text nickText;
    [SerializeField] private float nickChangeDuration;

    [SerializeField] private Animator[] skinPreviews;

    private Coroutine changeNickCor;
    private int skin;

    private void Awake()
    {
        nickField.onValueChanged.AddListener(OnNickChange);

        if (PlayerPrefs.HasKey("Nickname") == false)
        {
            PlayerPrefs.SetString("Nickname", $"Player_{Random.Range(1111, 9999)}");
        }
        nickField.text = PlayerPrefs.GetString("Nickname");

        skin = PlayerPrefs.GetInt("Skin");
        SetSkin(skin);
    }

    public void NextSkin()
    {
        skin++;
        if (skin >= skinPreviews.Length)
        {
            skin = 0;
        }
        SetSkin(skin);
    }


    private void OnNickChange(string s)
    {
        if (ValidateInput(s))
        {
            nickText.text = "";
        }
        else
        {
            nickText.text = "Некорректный ник";
        }
    }

    private IEnumerator OnNickChange()
    {
        nickText.text = "Ник сменён";
        yield return new WaitForSeconds(nickChangeDuration);
        nickText.text = "";
    }

    private bool ValidateInput(string input)
    {
        if (input.Length == 0 || input.Length > 20)
        {
            return false;
        }

        return true;
    }

    public void PreviousSkin()
    {
        skin--;
        if (skin < 0)
        {
            skin = skinPreviews.Length - 1;
        }
        SetSkin(skin);
    }

    public void SubmitNick()
    {
        if (ValidateInput(nickField.text) == false)
        {
            return;
        }
        if (changeNickCor != null)
        {
            StopCoroutine(changeNickCor);
        }
        changeNickCor = StartCoroutine(OnNickChange());
        PlayerPrefs.SetString("Nickname", nickField.text);
    }

    private void SetSkin(int id)
    {
        int oldId = PlayerPrefs.GetInt("Skin");
        Animator oldAnimator = skinPreviews[oldId];

        float normalizedTime = oldAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;

        PlayerPrefs.SetInt("Skin", id);
        foreach (var item in skinPreviews)
        {
            item.gameObject.SetActive(false);
        }

        Animator newAnimator = skinPreviews[id];

        newAnimator.gameObject.SetActive(true);

        AnimatorStateInfo newState =
            newAnimator.GetCurrentAnimatorStateInfo(0);

        newAnimator.Play(
            newState.shortNameHash,
            0,
            normalizedTime);

        skinPreviews[id].gameObject.SetActive(true);
    }
}
