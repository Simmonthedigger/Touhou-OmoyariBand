using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Seletor de habilidade: coloque em um objeto de UI vazio DENTRO do painel de seleção de fase.
/// Ele cria sozinho um botão por habilidade (mais o "Sem habilidade") e guarda a escolha no AbilitySelection.
/// Respeita o ProgressionState: habilidades ainda não desbloqueadas na sessão ficam travadas.
/// </summary>
public class AbilitySelectUI : MonoBehaviour
{
    [Header("Habilidades disponíveis")]
    [Tooltip("Arraste aqui os assets de habilidade (Assets > Create > Abilities > Ability).")]
    [SerializeField] private AbilityData[] abilities;

    [Tooltip("Mostra o botão para ir sem nenhuma habilidade.")]
    [SerializeField] private bool includeNoneOption = true;
    [SerializeField] private string noneLabel = "Sem habilidade";

    [Header("Visual")]
    [SerializeField] private Vector2 buttonSize = new Vector2(150f, 150f);
    [SerializeField] private float spacing = 14f;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 1f, 0.9f);
    [SerializeField] private Color lockedColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);

    [Header("Descrição (opcional)")]
    [Tooltip("Texto onde aparece a descrição e o custo da habilidade escolhida.")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private string noneDescription = "Nenhuma habilidade nesta fase.";

    private readonly List<AbilityData> entries = new List<AbilityData>(); // null = "sem habilidade"
    private readonly List<Image> buttonBackgrounds = new List<Image>();

    private void OnEnable()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        buttonBackgrounds.Clear();
        entries.Clear();

        SetupLayout();

        if (includeNoneOption) entries.Add(null);

        if (abilities != null)
        {
            foreach (AbilityData ability in abilities)
            {
                if (ability != null) entries.Add(ability);
            }
        }

        // Garante que nenhuma habilidade bloqueada de um visitante anterior permaneça selecionada
        ValidateCurrentSelection();

        for (int i = 0; i < entries.Count; i++)
        {
            CreateButton(i);
        }

        RefreshHighlight();
    }

    private void SetupLayout()
    {
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void ValidateCurrentSelection()
    {
        // Se já havia uma habilidade selecionada, verifica se ela ainda está desbloqueada para o visitante atual
        if (AbilitySelection.HasChosen)
        {
            AbilityData current = AbilitySelection.Selected;
            if (current != null && !PlayerAbilities.IsCurrentAbilityUnlocked(current.type))
            {
                AbilitySelection.ResetSelection();
            }
        }

        // Se ainda não há escolha válida, seleciona "Sem habilidade" (ou a primeira liberada)
        if (!AbilitySelection.HasChosen)
        {
            AbilityData fallback = null;
            if (!includeNoneOption)
            {
                foreach (AbilityData entry in entries)
                {
                    if (entry != null && PlayerAbilities.IsCurrentAbilityUnlocked(entry.type))
                    {
                        fallback = entry;
                        break;
                    }
                }
            }
            AbilitySelection.Select(fallback);
        }
    }

    private void CreateButton(int index)
    {
        AbilityData ability = entries[index];
        bool unlocked = ability == null || PlayerAbilities.IsCurrentAbilityUnlocked(ability.type);
        bool hasIcon = ability != null && ability.icon != null;

        GameObject go = new GameObject("AbilityButton_" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = gameObject.layer;

        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.sizeDelta = buttonSize;

        Image background = go.GetComponent<Image>();
        background.color = unlocked ? normalColor : lockedColor;
        buttonBackgrounds.Add(background);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.interactable = unlocked;

        if (unlocked)
        {
            button.onClick.AddListener(() => OnButtonClicked(index));
        }

        // Ícone (opcional)
        if (hasIcon)
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.layer = gameObject.layer;

            RectTransform iconRt = (RectTransform)iconObj.transform;
            iconRt.SetParent(rt, false);
            iconRt.anchorMin = new Vector2(0.15f, 0.4f);
            iconRt.anchorMax = new Vector2(0.85f, 0.95f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            Image icon = iconObj.GetComponent<Image>();
            icon.sprite = ability.icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }

        // Nome ou aviso de bloqueio
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.layer = gameObject.layer;

        RectTransform labelRt = (RectTransform)labelObj.transform;
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = new Vector2(1f, hasIcon ? 0.4f : 1f);
        labelRt.offsetMin = new Vector2(4f, 2f);
        labelRt.offsetMax = new Vector2(-4f, -2f);

        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        if (ability == null)
        {
            label.text = noneLabel;
        }
        else if (unlocked)
        {
            label.text = ability.displayName;
        }
        else
        {
            label.text = $"{ability.displayName}\n<size=70%><color=#FF6B6B>[BLOQUEADA]</color></size>";
        }

        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.color = unlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 26f;
        label.raycastTarget = false;
    }

    private void OnButtonClicked(int index)
    {
        AbilityData chosen = entries[index];
        AbilitySelection.Select(chosen);
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        for (int i = 0; i < buttonBackgrounds.Count; i++)
        {
            bool unlocked = entries[i] == null || PlayerAbilities.IsCurrentAbilityUnlocked(entries[i].type);
            if (!unlocked)
            {
                buttonBackgrounds[i].color = lockedColor;
                continue;
            }

            bool selected = entries[i] == AbilitySelection.Selected;
            buttonBackgrounds[i].color = selected ? selectedColor : normalColor;
        }

        if (descriptionText == null) return;

        AbilityData current = AbilitySelection.Selected;
        if (current == null)
        {
            descriptionText.text = noneDescription;
        }
        else
        {
            descriptionText.text = current.displayName + "\n" + current.description + "\nCusto: " + current.GetCostText();
        }
    }
}