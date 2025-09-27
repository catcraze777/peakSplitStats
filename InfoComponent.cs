using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SplitsStats;

public class InfoComponentTemplate
{
    public string name;
    public Func<string> TextToDisplay;
    public Sprite icon = null;
    public float initialFontSize = SplitsManager.HEIGHT_STAT_FONT_SIZE;
    public UIComponentPosition position = UIComponentPosition.TopRight;
    public int priority = 0;
    public Color color;

    private static Color defaultColor = new Color(0.9f, 0.9f, 0.9f);
    private string DefaultNullOutput() => null;

    public InfoComponentTemplate(string name, Func<string> TextToDisplay = null, Sprite icon = null, float initialFontSize = SplitsManager.HEIGHT_STAT_FONT_SIZE, UIComponentPosition position = UIComponentPosition.TopRight, Color? color = null, int priority = 0)
    {
        this.name = name;
        this.TextToDisplay = TextToDisplay ?? DefaultNullOutput;
        this.icon = icon;
        this.initialFontSize = initialFontSize;
        this.position = position;
        this.color = (color != null) ? (Color)color : defaultColor;
        this.priority = priority;
    }

    public InfoComponentTemplate(InfoComponentTemplate original)
    {
        this.name = original.name;
        this.TextToDisplay = original.TextToDisplay;
        this.icon = original.icon;
        this.initialFontSize = original.initialFontSize;
        this.position = original.position;
        this.color = original.color;
        this.priority = original.priority;
    }
}

public class InfoComponent : BaseUIComponent
{
    /// <summary>
    /// The RectTransform of the text gameObject.
    /// </summary>
    public RectTransform textRectTransform;

    /// <summary>
    /// The RectTransform of the image icon gameObject.
    /// </summary>
    public RectTransform iconRectTransform;

    /// <summary>
    /// The UnityEngine.UI.Image component of the icon gameObject.
    /// </summary>
    public UnityEngine.UI.Image iconImage => iconRectTransform?.GetComponent<UnityEngine.UI.Image>();

    /// <summary>
    /// The TMP_Text component of the text gameObject.
    /// </summary>
    public TMP_Text tmpText => textRectTransform?.GetComponent<TMP_Text>();

    /// <summary>
    /// When called, returns the text to display for this component. If this function returns null, then the text isn't updated.
    /// </summary>
    public Func<string> TextToDisplay;

    private Color currColor = new Color(0.9f, 0.9f, 0.9f);

    private static RectTransform _templateTextObject = null;

    /// <summary>
    /// A template text object using Daruma Drop One font, uses the AscentUI component as a base. DO NOT EDIT ANY PROPERTIES OF THIS GAMEOBJECT, COPY IT USING OBJECT.INSTANTIATE() FIRST
    /// </summary>
    public static RectTransform templateTextObject { get { if (_templateTextObject == null) GetTemplateObjects(); return _templateTextObject; } private set { _templateTextObject = value; } }
    private static RectTransform _templateIconObject = null;

    /// <summary>
    /// A template icon object using the custom fuzzy shader effect, uses the Extra Stamina icon as a base. DO NOT EDIT ANY PROPERTIES OF THIS GAMEOBJECT, COPY IT USING OBJECT.INSTANTIATE() FIRST
    /// </summary>
    public static RectTransform templateIconObject { get { if (_templateIconObject == null) GetTemplateObjects(); return _templateIconObject; } private set { _templateIconObject = value; } }

    private static void GetTemplateObjects()
    {
        // Find the ascent text to use as a template.
        RectTransform ascentUITransform = null;
        foreach (GameObject currGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            ascentUITransform = currGameObject.GetComponentInChildren<AscentUI>()?.GetComponent<RectTransform>();
            if (ascentUITransform != null) break;
        }
        if (ascentUITransform == null) throw new MissingReferenceException("Could not find an AscentUI object to use for a text template!");

        // Copy and create the template text.
        templateTextObject = UnityEngine.Object.Instantiate(ascentUITransform, ascentUITransform.parent);
        templateTextObject.name = $"InfoComponent Template Text";
        UnityEngine.Object.Destroy(templateTextObject.GetComponent<AscentUI>());
        templateTextObject.offsetMin = Vector2.zero;
        templateTextObject.offsetMax = Vector2.zero;

        // Find the stamina icon.
        RectTransform staminaIcon = null;
        foreach (GameObject currGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (UnityEngine.UI.Image currImgComp in currGameObject.GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                Transform currObj = currImgComp.transform;
                if (currObj.name == "Icon" && currObj.parent.name == "ExtraStaminaBar")
                {
                    staminaIcon = (RectTransform)currObj;
                    break;
                }
            }
        }
        if (staminaIcon == null) throw new MissingReferenceException("Could not find the stamina icon object to use for an icon template!");

        // Create the template icon.
        templateIconObject = UnityEngine.Object.Instantiate<RectTransform>(staminaIcon, ascentUITransform.parent);
        templateIconObject.name = "InfoComponent Template Icon";
        templateIconObject.offsetMin = Vector2.zero;
        templateIconObject.offsetMax = Vector2.zero;
        templateIconObject.anchoredPosition = Vector2.zero;
        templateIconObject.sizeDelta = new Vector2(200, 200);

        templateTextObject.gameObject.SetActive(false);
        templateIconObject.gameObject.SetActive(false);
    }

    /// <summary>
    /// Create and return an InfoComponent attached to a new gameObject parented to a provided transform.
    /// </summary>
    /// <param name="template">A template to use to fill in basic data needed for constructing the internal processes and game objects.</param>
    /// <param name="parent">The parent transform to set as this InfoComponent's gameObject's parent.</param>
    /// <returns>A InfoComponent attached to a newly created gameObject.</returns>
    public static InfoComponent CreateInfoComponent(InfoComponentTemplate template, Transform parent)
    {
        return CreateInfoComponent<InfoComponent>(template, parent);
    }

    /// <summary>
    /// The template used/to be used for creating the internal gameObjects used for the UI. Modifying it has no affect once Start() has executed.
    /// </summary>
    public InfoComponentTemplate template;

    /// <summary>
    /// Create and return a InfoComponent attached to a new gameObject parented to a provided transform. Use a template to allow subclasses to reuse object construction in the superclass.
    /// </summary>
    /// <typeparam name="T">InfoComponent or one of its defined subclasses.</typeparam>
    /// <param name="template">A template to use to fill in basic data needed for constructing the internal processes and game objects.</param>
    /// <param name="parent">The parent transform to set as this InfoComponent's gameObject's parent.</param>
    /// <returns>A InfoComponent attached to a newly created gameObject. Matches type of template if using a subclass.</returns>
    public static T CreateInfoComponent<T>(InfoComponentTemplate template, Transform parent) where T : InfoComponent
    {
        T currComponent = CreateBaseUIComponent<T>(template.name, parent);
        currComponent.template = new InfoComponentTemplate(template);
        currComponent.SetSortingPriority(template.priority);

        return currComponent;
    }

    /// <summary>
    /// The Monobehavior Start Script: This is responsible for creating and modifying child gameObjects needed for the UI component, if they don't already exist.
    /// </summary>
    public override void Start()
    {
        base.Start();

        if (template != null) uiPosition = template.position;
        SplitsManager.SetAlignment(rectTransform, uiPosition);
        rectTransform.pivot = new Vector2(rectTransform.pivot.x, 1.0f);

        if (textRectTransform == null || textRectTransform.GetComponent<RectTransform>() == null || textRectTransform.GetComponent<TMP_Text>() == null)
        {
            if (template == null) throw new ArgumentNullException("An InfoComponentTemplate template is required to create the default text object! Please ensure this.template is set prior to the Awake() call!");

            // Disable the present textRectTransform since it doesn't match our expectations to replace it.
            textRectTransform?.gameObject.SetActive(false);

            RectTransform tempRectTransform = UnityEngine.Object.Instantiate<RectTransform>(InfoComponent.templateTextObject, gameObject.transform);
            tempRectTransform.gameObject.SetActive(true);
            tempRectTransform.name = template.name + " Text";
            tempRectTransform.anchoredPosition = Vector2.zero;
            SplitsManager.SetAlignment(tempRectTransform, uiPosition);

            foreach (Transform child in tempRectTransform.transform) UnityEngine.Object.Destroy(child.gameObject);

            textRectTransform = tempRectTransform;
        }

        if (iconRectTransform == null || iconRectTransform.GetComponent<RectTransform>() == null || iconRectTransform.GetComponent<UnityEngine.UI.Image>() == null)
        {
            if (template == null) throw new ArgumentNullException("An InfoComponentTemplate template is required to create the default icon object! Please ensure this.template is set prior to the Awake() call!");

            // Disable the present iconRectTransform since it doesn't match our expectations to replace it.
            iconRectTransform?.gameObject.SetActive(false);

            if (templateIconObject != null && template.icon != null)
            {
                RectTransform tempIconRectTransform = UnityEngine.Object.Instantiate<RectTransform>(InfoComponent.templateIconObject, gameObject.transform);
                tempIconRectTransform.gameObject.SetActive(true);
                SplitsManager.SetAlignment(tempIconRectTransform, uiPosition);
                tempIconRectTransform.anchoredPosition = Vector2.zero;
                tempIconRectTransform.sizeDelta = new Vector2(INITIAL_HEIGHT * SplitsManager.ICON_SIZE_SCALE, INITIAL_HEIGHT * SplitsManager.ICON_SIZE_SCALE);
                tempIconRectTransform.name = template.name + " Icon";
                tempIconRectTransform.GetComponent<UnityEngine.UI.Image>().sprite = template.icon;
                iconRectTransform = tempIconRectTransform;

                float iconSize = tempIconRectTransform.sizeDelta.x;
                int direction = uiPosition == UIComponentPosition.TopLeft ? 1 : -1;
                textRectTransform.anchoredPosition += new Vector2(direction * (iconSize + SplitsManager.ICON_TEXT_SPACING), 0f);
            }
        }

        TextToDisplay = template.TextToDisplay;
        SetColor(template.color);
        SetHeight(template.initialFontSize);

        tmpText.autoSizeTextContainer = true;
        tmpText.textWrappingMode = (TextWrappingModes)0;
        tmpText.alignment = uiPosition == UIComponentPosition.TopLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        tmpText.lineSpacing = 0f;
        tmpText.fontSize = INITIAL_HEIGHT;
        tmpText.outlineColor = new Color32((byte)0, (byte)0, (byte)0, byte.MaxValue);
        tmpText.outlineWidth = 0f; //0.055f;
        tmpText.color = currColor;

        this.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(INITIAL_HEIGHT, INITIAL_HEIGHT * 2.0f);
    }

    /// <summary>
    /// Change the color of the text.
    /// </summary>
    /// <param name="newColor"> The new color of the text. </param>
    /// <returns> A boolean indicating if the change was successful. </returns>
    public virtual bool SetColor(Color newColor)
    {
        bool changedColor = false;
        currColor = newColor;

        if (tmpText != null)
        {
            tmpText.color = newColor;
            changedColor = true;
        }
        if (iconImage != null)
        {
            iconImage.color = newColor;
            changedColor = true;
        }

        return changedColor;
    }

    /// <summary>
    /// Update the text of the info stat. Text shows the time and the info stat's label if present.
    /// </summary>
    public virtual void Update()
    {
        if (tmpText != null) tmpText.text = TextToDisplay() ?? tmpText.text;
    }
}