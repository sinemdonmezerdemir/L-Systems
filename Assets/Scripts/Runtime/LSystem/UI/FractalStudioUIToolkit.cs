using System.Collections.Generic;
using System.IO;
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
    private Button generateBtn;
    private Button animateBtn;
    private Button exportBtn;
    private Button addRuleBtn;

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

    private const string PRESET_LIST_FILENAME = "FractalStudio_PresetList.json";
    private const string PRESET_DATA_PREFIX = "FractalPreset_";

    private string SaveDirectory => Application.persistentDataPath;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || lSystemRenderer == null) return;

        var root = uiDocument.rootVisualElement;

        presetDropdown = root.Q<DropdownField>("preset-dropdown");
        saveBtn = root.Q<Button>("btn-save");
        generateBtn = root.Q<Button>("btn-generate");
        animateBtn = root.Q<Button>("btn-animate");
        exportBtn = root.Q<Button>("btn-export");
        addRuleBtn = root.Q<Button>("btn-add-rule");

        nameField = root.Q<TextField>("input-name");
        axiomField = root.Q<TextField>("input-axiom");
        angleField = root.Q<FloatField>("input-angle");
        lengthField = root.Q<FloatField>("input-length");
        startAngleField = root.Q<FloatField>("input-start-angle");
        iterationsField = root.Q<IntegerField>("input-iterations");
        rulesContainer = root.Q<VisualElement>("rules-container");
        fractalImage = root.Q<Image>("fractal-image");
        themeToggle = root.Q<Toggle>("toggle-theme");

        lSystemRenderer.OnTextureUpdated += OnRenderCompleted;

        if (themeToggle != null) themeToggle.RegisterValueChangedCallback(OnThemeChangedEvent);
        presetDropdown.RegisterValueChangedCallback(OnPresetSelectionChanged);

        saveBtn.clicked += SaveCurrentPreset;
        generateBtn.clicked += OnGenerateClicked;
        animateBtn.clicked += OnAnimateClicked;
        exportBtn.clicked += ExportImage;
        addRuleBtn.clicked += AddEmptyRuleRow;

        ApplyGridBackground();
        CreateStatusLabel();

        activeData = ScriptableObject.CreateInstance<LSystemData>();

        SeedDefaultData();
        RefreshDropdown();
        LoadSelectedPreset();
    }

    void OnDisable()
    {
        if (lSystemRenderer != null) lSystemRenderer.OnTextureUpdated -= OnRenderCompleted;

        if (themeToggle != null) themeToggle.UnregisterValueChangedCallback(OnThemeChangedEvent);
        if (presetDropdown != null) presetDropdown.UnregisterValueChangedCallback(OnPresetSelectionChanged);

        if (saveBtn != null) saveBtn.clicked -= SaveCurrentPreset;
        if (generateBtn != null) generateBtn.clicked -= OnGenerateClicked;
        if (animateBtn != null) animateBtn.clicked -= OnAnimateClicked;
        if (exportBtn != null) exportBtn.clicked -= ExportImage;
        if (addRuleBtn != null) addRuleBtn.clicked -= AddEmptyRuleRow;
    }

    private void OnThemeChangedEvent(ChangeEvent<bool> evt) => OnThemeChanged(evt.newValue);
    private void OnPresetSelectionChanged(ChangeEvent<string> evt) => LoadSelectedPreset();
    private void ExportImage() => lSystemRenderer.SaveToPNG();
    private void AddEmptyRuleRow() => AddRuleRow("", "");

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

    [System.Serializable]
    private class PresetListWrapper
    {
        public List<string> names = new List<string>();
    }

    private List<string> GetSavedPresetNames()
    {
        string path = Path.Combine(SaveDirectory, PRESET_LIST_FILENAME);
        if (!File.Exists(path)) return new List<string>();

        try
        {
            string json = File.ReadAllText(path);
            var wrapper = JsonUtility.FromJson<PresetListWrapper>(json);
            return wrapper != null ? wrapper.names : new List<string>();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FractalStudio] Preset listesi okunamadý: {e.Message}");
            return new List<string>();
        }
    }

    private void SavePresetNamesList(List<string> names)
    {
        string path = Path.Combine(SaveDirectory, PRESET_LIST_FILENAME);
        var wrapper = new PresetListWrapper { names = names };
        File.WriteAllText(path, JsonUtility.ToJson(wrapper));
    }

    private void SeedDefaultData()
    {
        if (defaultPresets == null || defaultPresets.Count == 0) return;

        List<string> savedNames = GetSavedPresetNames();
        bool listUpdated = false;

        foreach (var data in defaultPresets)
        {
            if (data == null) continue;

            string filePath = Path.Combine(SaveDirectory, $"{PRESET_DATA_PREFIX}{data.FractalName}.json");

            if (!File.Exists(filePath))
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(filePath, json);

                if (!savedNames.Contains(data.FractalName))
                {
                    savedNames.Add(data.FractalName);
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

        string currentSelection = activeData != null ? activeData.FractalName : "";
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

        string filePath = Path.Combine(SaveDirectory, $"{PRESET_DATA_PREFIX}{selectedName}.json");

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(json, activeData);

            RefreshUIFromData();
        }
    }

    private void SaveCurrentPreset()
    {
        SyncDataFromUI();

        string filePath = Path.Combine(SaveDirectory, $"{PRESET_DATA_PREFIX}{activeData.FractalName}.json");
        string json = JsonUtility.ToJson(activeData, true);
        File.WriteAllText(filePath, json);

        List<string> savedNames = GetSavedPresetNames();
        if (!savedNames.Contains(activeData.FractalName))
        {
            savedNames.Add(activeData.FractalName);
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

        nameField.value = activeData.FractalName;
        axiomField.value = activeData.Axiom;
        angleField.value = activeData.Angle;
        lengthField.value = activeData.Thickness;
        startAngleField.value = activeData.StartAngle;
        iterationsField.value = activeData.Iterations;

        rulesContainer.Clear();
        foreach (var rule in activeData.Rules)
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
        string newName = string.IsNullOrEmpty(nameField.value) ? "NewFractal" : nameField.value;
        string newAxiom = axiomField.value;
        float newAngle = angleField.value;
        int newThickness = (int)lengthField.value;
        float newStartAngle = startAngleField.value;

        int safeIterations = Mathf.Clamp(iterationsField.value, 1, 10);
        if (iterationsField.value != safeIterations)
        {
            iterationsField.SetValueWithoutNotify(safeIterations);
        }

        var parsedRules = new List<LSystemRule>();
        foreach (var row in rulesContainer.Children())
        {
            var fields = row.Query<TextField>().ToList();
            if (fields.Count == 2 && !string.IsNullOrEmpty(fields[0].value))
            {
                parsedRules.Add(new LSystemRule { symbol = fields[0].value[0], replacement = fields[1].value });
            }
        }

        activeData.UpdateData(newName, newAxiom, newAngle, newThickness, newStartAngle, safeIterations, parsedRules);

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
        lSystemRenderer.RenderImmediate();
    }

    private void OnAnimateClicked()
    {
        SyncDataFromUI();
        lSystemRenderer.RenderAnimated(animationStepDelay);
    }
}