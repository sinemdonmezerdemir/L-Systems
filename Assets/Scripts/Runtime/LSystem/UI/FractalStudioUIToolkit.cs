using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using LSystem;

[RequireComponent(typeof(UIDocument))]
public class FractalStudioUIToolkit : MonoBehaviour
{
    [Header("Rendering Engine")]
    public LSystemRenderer lSystemRenderer;
    public float animationStepDelay = 0.5f;

    [Header("Default Presets (Seeding)")]
    public List<LSystemData> defaultPresets;

    private UIDocument uiDocument;
    private LSystemData activeData;

    private DropdownField presetDropdown;
    private Button saveBtn;
    private TextField nameField;
    private TextField axiomField;
    private FloatField angleField;
    private FloatField lengthField;
    private FloatField startAngleField;
    private IntegerField iterationsField;
    private VisualElement rulesContainer;
    private Image fractalImage;
    private Label statusLabel;
    private Toggle themeToggle;

    private const string PRESET_LIST_KEY = "FractalStudio_PresetList";
    private const string PRESET_DATA_PREFIX = "FractalStudio_Data_";

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || lSystemRenderer == null) return;

        var root = uiDocument.rootVisualElement;

        // UI Element Bindings
        presetDropdown = root.Q<DropdownField>("preset-dropdown");
        saveBtn = root.Q<Button>("btn-save");
        nameField = root.Q<TextField>("input-name");
        axiomField = root.Q<TextField>("input-axiom");
        angleField = root.Q<FloatField>("input-angle");
        lengthField = root.Q<FloatField>("input-length");
        startAngleField = root.Q<FloatField>("input-start-angle");
        iterationsField = root.Q<IntegerField>("input-iterations");
        rulesContainer = root.Q<VisualElement>("rules-container");
        fractalImage = root.Q<Image>("fractal-image");

        // Event Registration: Subscription to the Renderer
        lSystemRenderer.OnTextureUpdated += OnRenderCompleted;

        // Theme Listener
        themeToggle = root.Q<Toggle>("toggle-theme");
        if (themeToggle != null)
        {
            themeToggle.RegisterValueChangedCallback(evt => OnThemeChanged(evt.newValue));
        }

        ApplyGridBackground();
        CreateStatusLabel();

        // Event Registrations
        presetDropdown.RegisterValueChangedCallback(evt => LoadSelectedPreset());
        saveBtn.clicked += SaveCurrentPreset;
        root.Q<Button>("btn-generate").clicked += OnGenerateClicked;
        root.Q<Button>("btn-animate").clicked += OnAnimateClicked;
        root.Q<Button>("btn-export").clicked += () => lSystemRenderer.SaveToPNG();
        root.Q<Button>("btn-add-rule").clicked += () => AddRuleRow("", "");

        activeData = ScriptableObject.CreateInstance<LSystemData>();

        SeedDefaultData();
        RefreshDropdown();
        LoadSelectedPreset();
    }

    void OnDisable()
    {
        // Proper event unsubscription to prevent memory leaks
        if (lSystemRenderer != null)
        {
            lSystemRenderer.OnTextureUpdated -= OnRenderCompleted;
        }
    }

    /// <summary>
    /// Event handler for when the renderer completes a drawing cycle.
    /// Replaces the need for polling in LateUpdate.
    /// </summary>
    private void OnRenderCompleted(Texture2D newTexture)
    {
        if (fractalImage == null || statusLabel == null) return;

        fractalImage.image = newTexture;

        int currentIter = lSystemRenderer.currentDisplayIteration;
        int maxIter = iterationsField.value;
        bool isAnim = lSystemRenderer.isAnimating;

        string statusText = isAnim ? "ANIMATING..." : "READY";
        statusLabel.text = $"STATUS: {statusText}   |   ITERATION: {currentIter} / {maxIter}";

        statusLabel.style.color = isAnim
            ? new StyleColor(new Color(0.9f, 0.6f, 0.2f))
            : new StyleColor(new Color(0.2f, 0.8f, 0.4f));
    }

    private void OnThemeChanged(bool isLight)
    {
        var root = uiDocument.rootVisualElement;

        if (isLight)
        {
            root.AddToClassList("light-theme");
            if (themeToggle != null) themeToggle.label = "Theme: Light";
        }
        else
        {
            root.RemoveFromClassList("light-theme");
            if (themeToggle != null) themeToggle.label = "Theme: Dark";
        }

        lSystemRenderer.SetThemeColors(isLight);

        if (lSystemRenderer.resultTexture != null && !lSystemRenderer.isAnimating)
        {
            OnGenerateClicked();
        }
    }

    private List<string> GetSavedPresetNames()
    {
        string listStr = PlayerPrefs.GetString(PRESET_LIST_KEY, "");
        if (string.IsNullOrEmpty(listStr)) return new List<string>();
        return listStr.Split(',').ToList();
    }

    private void SavePresetNamesList(List<string> names)
    {
        PlayerPrefs.SetString(PRESET_LIST_KEY, string.Join(",", names));
        PlayerPrefs.Save();
    }

    private void SeedDefaultData()
    {
        if (defaultPresets == null || defaultPresets.Count == 0) return;

        List<string> savedNames = GetSavedPresetNames();
        bool listUpdated = false;

        foreach (var data in defaultPresets)
        {
            if (data == null) continue;

            string saveKey = PRESET_DATA_PREFIX + data.fractalName;

            if (!PlayerPrefs.HasKey(saveKey))
            {
                string json = JsonUtility.ToJson(data, true);
                PlayerPrefs.SetString(saveKey, json);

                if (!savedNames.Contains(data.fractalName))
                {
                    savedNames.Add(data.fractalName);
                    listUpdated = true;
                }
            }
        }

        if (listUpdated) SavePresetNamesList(savedNames);
    }

    private void RefreshDropdown()
    {
        List<string> files = GetSavedPresetNames();
        if (files.Count == 0) files.Add("No Data Found");

        presetDropdown.choices = files;

        string currentSelection = activeData != null ? activeData.fractalName : "";
        if (files.Contains(currentSelection))
        {
            presetDropdown.SetValueWithoutNotify(currentSelection);
        }
        else
        {
            presetDropdown.SetValueWithoutNotify(files[0]);
        }
    }

    private void LoadSelectedPreset()
    {
        string selectedName = presetDropdown.value;
        if (string.IsNullOrEmpty(selectedName) || selectedName == "No Data Found") return;

        string saveKey = PRESET_DATA_PREFIX + selectedName;
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
            JsonUtility.FromJsonOverwrite(json, activeData);
            activeData.fractalName = selectedName;
            RefreshUIFromData();
        }
    }

    private void SaveCurrentPreset()
    {
        SyncDataFromUI();

        string saveKey = PRESET_DATA_PREFIX + activeData.fractalName;
        string json = JsonUtility.ToJson(activeData, true);
        PlayerPrefs.SetString(saveKey, json);

        List<string> savedNames = GetSavedPresetNames();
        if (!savedNames.Contains(activeData.fractalName))
        {
            savedNames.Add(activeData.fractalName);
            SavePresetNamesList(savedNames);
        }

        RefreshDropdown();
    }

    private void CreateStatusLabel()
    {
        statusLabel = new Label("STATUS: READY  |  ITERATION: 0");
        statusLabel.style.position = Position.Absolute;
        statusLabel.style.bottom = 16;
        statusLabel.style.left = 24;
        statusLabel.style.fontSize = 11;
        statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusLabel.style.letterSpacing = 1;
        fractalImage.parent.Add(statusLabel);
    }

    private void RefreshUIFromData()
    {
        if (activeData == null) return;

        nameField.value = activeData.fractalName;
        axiomField.value = activeData.axiom;
        angleField.value = activeData.angle;
        lengthField.value = activeData.segmentLength;
        startAngleField.value = activeData.startAngle;
        iterationsField.value = activeData.iterations;

        rulesContainer.Clear();
        foreach (var rule in activeData.rules)
        {
            AddRuleRow(rule.symbol.ToString(), rule.replacement);
        }
    }

    private void AddRuleRow(string symbol = "", string replacement = "")
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
        var symbolInput = new TextField { maxLength = 1, value = symbol, style = { width = 45 } };
        var replacementInput = new TextField { value = replacement, style = { flexGrow = 1, marginLeft = 4, marginRight = 4 } };

        var deleteBtn = new Button
        {
            text = "X",
            style = {
                backgroundColor = new StyleColor(Color.clear),
                color = new StyleColor(new Color(0.9f, 0.3f, 0.3f)),
                borderBottomWidth = 0, borderTopWidth = 0, borderLeftWidth = 0, borderRightWidth = 0,
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14
            }
        };

        deleteBtn.clicked += () => rulesContainer.Remove(row);
        row.Add(symbolInput);
        row.Add(replacementInput);
        row.Add(deleteBtn);
        rulesContainer.Add(row);
    }

    private void ApplyGridBackground()
    {
        int size = 40;
        Texture2D gridTex = new Texture2D(size, size);
        Color bgColor = new Color(0, 0, 0, 0);
        Color lineColor = new Color(1f, 1f, 1f, 0.005f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x == 0 || y == 0) gridTex.SetPixel(x, y, lineColor);
                else gridTex.SetPixel(x, y, bgColor);
            }
        }
        gridTex.Apply();

        var container = fractalImage.parent;
        container.style.backgroundImage = new StyleBackground(gridTex);
        container.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        container.style.backgroundSize = new BackgroundSize(size, size);
    }

    private void SyncDataFromUI()
    {
        activeData.fractalName = string.IsNullOrEmpty(nameField.value) ? "NewFractal" : nameField.value;
        activeData.axiom = axiomField.value;
        activeData.angle = angleField.value;
        activeData.segmentLength = lengthField.value;
        activeData.startAngle = startAngleField.value;

        int safeIterations = Mathf.Clamp(iterationsField.value, 1, 10);
        if (iterationsField.value != safeIterations)
        {
            iterationsField.SetValueWithoutNotify(safeIterations);
        }
        activeData.iterations = safeIterations;

        activeData.rules.Clear();
        foreach (var row in rulesContainer.Children())
        {
            var fields = row.Query<TextField>().ToList();
            if (fields.Count == 2 && !string.IsNullOrEmpty(fields[0].value))
            {
                activeData.rules.Add(new LSystemRule { symbol = fields[0].value[0], replacement = fields[1].value });
            }
        }

        float currentWidth = fractalImage.parent.resolvedStyle.width;
        float currentHeight = fractalImage.parent.resolvedStyle.height;

        if (currentWidth > 10 && currentHeight > 10)
        {
            lSystemRenderer.textureWidth = Mathf.RoundToInt(currentWidth);
            lSystemRenderer.textureHeight = Mathf.RoundToInt(currentHeight);
        }

        lSystemRenderer.data = activeData;
    }

    private void OnGenerateClicked()
    {
        SyncDataFromUI();
        lSystemRenderer.DrawDirect();
    }

    private void OnAnimateClicked()
    {
        SyncDataFromUI();
        lSystemRenderer.DrawAnimated(animationStepDelay);
    }
}