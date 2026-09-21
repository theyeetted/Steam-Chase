using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Steamchasing.Cards
{
    public class CardView : MonoBehaviour
    {
        [Header("Card Information")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text energyCostText;

        [Header("Status Icons")]
        [SerializeField] private Transform statusIconContainer;
        [SerializeField] private StatusIconView statusIconPrefab;

        [Header("Database")]
        [SerializeField] private EffectIconDatabase effectIconDatabase;

        [Header("Description")]
        [SerializeField]
        private Color statusHighlightColor =
            new Color(1f, 0.75f, 0.2f);

        private CardData currentCard;

        public void Setup(CardData cardData)
        {
            if (cardData == null)
            {
                Debug.LogError("CardView: CardData is null.", this);
                return;
            }

            currentCard = cardData;

            SetupBasicInformation();
            SetupDescription();
            SetupStatusIcons();
        }

        private void SetupBasicInformation()
        {
            cardNameText.text = currentCard.CardName;

            energyCostText.text =
                currentCard.EnergyCost.ToString();

            if (artworkImage != null)
                artworkImage.sprite = currentCard.Artwork;
        }

        private void SetupDescription()
        {
            descriptionText.text =
                BuildHighlightedDescription(currentCard);
        }

        private string BuildHighlightedDescription(CardData card)
        {
            string description = card.Description;

            foreach (CardEffect effect in card.Effects)
            {
                string statusName =
                    GetStatusName(effect.type);

                if (string.IsNullOrEmpty(statusName))
                    continue;

                string colorCode =
                    UnityEngine.ColorUtility.ToHtmlStringRGB(
                        statusHighlightColor
                    );

                string highlightedName =
                    $"<color=#{colorCode}>{statusName}</color>";

                description =
                    description.Replace(
                        statusName,
                        highlightedName
                    );
            }

            return description;
        }

        private void SetupStatusIcons()
        {
            ClearStatusIcons();

            if (effectIconDatabase == null)
            {
                Debug.LogError(
                    "CardView: EffectIconDatabase is not assigned.",
                    this
                );

                return;
            }

            if (statusIconPrefab == null)
            {
                Debug.LogError(
                    "CardView: StatusIconPrefab is not assigned.",
                    this
                );

                return;
            }

            foreach (CardEffect effect in currentCard.Effects)
            {
                if (!ShouldShowEffectIcon(effect.type))
                    continue;

                Sprite icon =
                    effectIconDatabase.GetIcon(effect.type);

                if (icon == null)
                    continue;

                StatusIconView iconView =
                    Instantiate(
                        statusIconPrefab,
                        statusIconContainer
                    );

                iconView.Setup(
                    icon,
                    effect.value
                );
            }
        }

        private void ClearStatusIcons()
        {
            if (statusIconContainer == null)
                return;

            for (int i = statusIconContainer.childCount - 1;
                 i >= 0;
                 i--)
            {
                Destroy(
                    statusIconContainer
                        .GetChild(i)
                        .gameObject
                );
            }
        }

        private bool ShouldShowEffectIcon(EffectType type)
        {
            switch (type)
            {
                case EffectType.ApplyVulnerable:
                case EffectType.ApplyAttackUp:
                case EffectType.ApplyAttackDown:
                case EffectType.ApplyStress:
                case EffectType.ApplyShock:
                case EffectType.ApplyMark:
                case EffectType.FillStunGauge:
                case EffectType.ExtraAttack:
                case EffectType.FollowUpAttack:
                case EffectType.ApplyAggravated:
                case EffectType.Disintegrate:
                    return true;

                default:
                    return false;
            }
        }

        private string GetStatusName(EffectType type)
        {
            switch (type)
            {
                case EffectType.ApplyVulnerable:
                    return "Vulnerable";

                case EffectType.ApplyAttackUp:
                    return "Attack Up";

                case EffectType.ApplyAttackDown:
                    return "Attack Down";

                case EffectType.ApplyStress:
                    return "Stress";

                case EffectType.ApplyShock:
                    return "Shock";

                case EffectType.ApplyMark:
                    return "Mark";

                case EffectType.FillStunGauge:
                    return "Stun";

                case EffectType.ExtraAttack:
                    return "Extra Attack";

                case EffectType.FollowUpAttack:
                    return "Follow-Up";

                case EffectType.ApplyAggravated:
                    return "Aggravated";

                case EffectType.Disintegrate:
                    return "Disintegrate";

                default:
                    return string.Empty;
            }
        }
    }
}