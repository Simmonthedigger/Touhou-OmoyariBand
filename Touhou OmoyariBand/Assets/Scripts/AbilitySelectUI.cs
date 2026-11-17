using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Seletor de habilidade: coloque em um objeto de UI vazio DENTRO do painel de seleção de fase.
/// Ele cria sozinho um botão por habilidade (mais o "Sem habilidade") e guarda a escolha no AbilitySelection.
/// Só uma habilidade pode ser escolhida por vez. A escolha é lembrada entre as sessões.
/// Não coloque outros objetos dentro dele: os filhos são recriados toda vez que o painel abre.
/// </summary>
public class AbilitySelectUI : MonoBehaviour
{
    private const string PrefsKey = "SelectedAbilityName";
    private const string NoneName = "None";

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

    [Header("Descrição (opcional)")]
    [Tooltip("Texto onde aparece a descrição e o custo da habilidade escolhida.")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private string noneDescription = "Nenhuma habilidade nesta fase.";

    private readonly List<AbilityData> entries = new List<AbilityData>(); // null = "sem habilidade"
    private readonly List<Image> buttonBackgrounds = new List<Image>();

    // O painel pode começar desativado: a lista é montada toda vez que ele abre
    private void OnEnable()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        // Limpa os botões antigos
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

        // Primeira vez no seletor: restaura a última escolha salva
        if (!AbilitySelection.HasChosen) RestoreSavedSelection();

        for (int i = 0; i < entries.Count; i++)
        {
            CreateButton(i);
        }

        RefreshHighlight();
    }

    // Organiza os botões lado a lado
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

    private void RestoreSavedSelection()
    {
        string saved = PlayerPrefs.GetString(PrefsKey, NoneName);

        AbilityData found = null;
        if (saved != NoneName)
        {
            foreach (AbilityData entry in entries)
            {
                if (entry != null && entry.name == saved)
                {
                    found = entry;
                    break;
                }
            }
        }

        // Sem o botão "sem habilidade", precisa haver uma escolha: usa a primeira
        if (found == null && !includeNoneOption && entries.Count > 0) found = entries[0];

        AbilitySelection.Select(found);
    }

    private void CreateButton(int index)
    {
        AbilityData ability = entries[index];
        bool hasIcon = ability != null && ability.icon != null;

        GameObject go = new GameObject("AbilityButton_" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = gameObject.layer;

        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.sizeDelta = buttonSize;

        Image background = go.GetComponent<Image>();
        background.color = normalColor;
        buttonBackgrounds.Add(background);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => OnButtonClicked(index));

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
        }

        // Nome
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.layer = gameObject.layer;

        RectTransform labelRt = (RectTransform)labelObj.transform;
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = new Vector2(1f, hasIcon ? 0.4f : 1f);
        labelRt.offsetMin = new Vector2(4f, 2f);
        labelRt.offsetMax = new Vector2(-4f, -2f);

        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        label.text = ability != null ? ability.displayName : noneLabel;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 26f;
        label.raycastTarget = false;
    }

    private void OnButtonClicked(int index)
    {
        AbilityData chosen = entries[index];
        AbilitySelection.Select(chosen);

        PlayerPrefs.SetString(PrefsKey, chosen != null ? chosen.name : NoneName);
        PlayerPrefs.Save();

        RefreshHighlight();
    }

    // Destaca o botão escolhido e atualiza a descrição
    private void RefreshHighlight()
    {
        for (int i = 0; i < buttonBackgrounds.Count; i++)
        {
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
