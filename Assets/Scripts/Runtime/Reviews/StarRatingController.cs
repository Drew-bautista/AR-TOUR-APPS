using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class StarRatingController : MonoBehaviour
{
    public event Action<int> RatingChanged;

    [SerializeField, Range(1, 5)] private int rating = 5;
    [SerializeField] private Button[] starButtons = Array.Empty<Button>();
    [SerializeField] private Text[] starLabels = Array.Empty<Text>();
    [SerializeField] private Color selectedColor = new Color32(0xFF, 0xC9, 0x33, 0xFF);
    [SerializeField] private Color emptyColor = new Color32(0x8A, 0x94, 0xA8, 0xFF);

    public int Rating => rating;

    public void Initialize(Button[] buttons, Text[] labels)
    {
        starButtons = buttons ?? Array.Empty<Button>();
        starLabels = labels ?? Array.Empty<Text>();

        for (int i = 0; i < starButtons.Length; i++)
        {
            int capturedRating = i + 1;
            if (starButtons[i] != null)
            {
                starButtons[i].onClick.RemoveAllListeners();
                starButtons[i].onClick.AddListener(() => SetRating(capturedRating));
            }
        }

        RefreshVisuals();
    }

    public void SetRating(int value)
    {
        rating = Mathf.Clamp(value, 1, 5);
        RefreshVisuals();
        RatingChanged?.Invoke(rating);
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < starLabels.Length; i++)
        {
            if (starLabels[i] == null)
            {
                continue;
            }

            bool selected = i < rating;
            starLabels[i].text = selected ? "\u2605" : "\u2606";
            starLabels[i].color = selected ? selectedColor : emptyColor;
        }
    }
}
