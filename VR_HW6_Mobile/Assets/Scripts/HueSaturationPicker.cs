using UnityEngine;
using UnityEngine.UI;

using UnityEngine.SceneManagement;

using TMPro;

public class HueSaturationPicker : MonoBehaviour
{
    public RawImage colorWheel; // Reference to the color wheel image
    public CardboardReticlePointer reticlePointer; // Reference to the Cardboard reticle pointer
    public Camera mainCamera; // Reference to the main camera
    public GameObject targetObject; // Reference to the target object whose color will be updated

    private Renderer targetRenderer; // Renderer of the target object
    public HSVColorVisualiser hsvColorScript; // Color object

    private Vector3 lastPointerPosition; // Last position of the reticle pointer
    private float pointerStayTime; // Time the pointer has stayed in the same position
    private float waitTime;
    private const float tolerance = 0.5f; // Tolerance for position difference

    public TextMeshProUGUI timerText; // Reference to the UI Text element for displaying the timer

    void Start()
    {
        // Get the Renderer component from the target object
        if (targetObject != null)
        {
            targetRenderer = targetObject.GetComponent<Renderer>();
        }

        lastPointerPosition = Vector3.zero;
        pointerStayTime = 0f;
        waitTime = 3f;

        // Initialize the timer text
        UpdateTimerText(waitTime);
    }

    void Update()
    {
        // Perform a raycast from the reticle pointer to detect the color wheel
        Ray ray = new Ray(reticlePointer.transform.position, reticlePointer.transform.forward);
        RaycastHit hit;

        // Define the layer mask for the color wheel layer
        int layerMask = 1 << LayerMask.NameToLayer("ColorWheelLayer");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            // Ensure the hit object is the color wheel
            if (hit.transform == colorWheel.transform)
            {
                // Convert hit point to local coordinates of the color wheel
                Vector2 localCursor;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    colorWheel.rectTransform, hit.point, null, out localCursor);

                // Convert local coordinates to texture coordinates
                Rect rect = colorWheel.rectTransform.rect;
                int x = Mathf.RoundToInt((localCursor.x - rect.xMin) * colorWheel.texture.width / rect.width);
                int y = Mathf.RoundToInt((localCursor.y - rect.yMin) * colorWheel.texture.height / rect.height);

                // Get the color from the texture at the calculated coordinates
                Texture2D texture = colorWheel.texture as Texture2D;
                Color color = texture.GetPixel(x, y);

                // Convert RGB color to HSV
                float h, s, v;
                Color.RGBToHSV(color, out h, out s, out v);

                // Set the hue and saturation of the visualiser object's material
                hsvColorScript.SetHue(h);
                hsvColorScript.SetSaturation(s);

                // Check if the pointer has stayed in the same position with tolerance
                if (Vector3.Distance(hit.point, lastPointerPosition) > tolerance)
                {
                    lastPointerPosition = hit.point;
                    pointerStayTime = 0f;
                }
                else
                {
                    pointerStayTime += Time.deltaTime;

                    // Update the timer text
                    UpdateTimerText(waitTime - pointerStayTime);

                    // If the pointer has stayed for more than waitTime
                    if (pointerStayTime >= waitTime)
                    {
                        ExitHandler();
                    }
                }
            }
        }
        else
        {
            // Reset the timer text if the pointer is not on the color wheel
            UpdateTimerText(waitTime);
        }
    }

    void UpdateTimerText(float timeLeft)
    {
        timerText.text = "DEBUG TIMER: " + Mathf.Max(0, timeLeft).ToString("F2") + "s";
    }

    void ExitHandler()
    {
        // 0. GET VALUES
        float h = hsvColorScript.hue;
        float s = hsvColorScript.saturation;
        float v = hsvColorScript.value;

        // 1. UPDATE THE PLAYED SCENES BOOL
        Scene currentScene = SceneManager.GetActiveScene();
        MainScreenManager.instance.UpdateBool(currentScene.name);

        // 2. CREATE A NEW HSVColor INSTANCE
        HSVColorData hsvColorInstance = HSVColorData.Create(h, s, v);

        // 3. SAVE THE HSVColor INSTANCE TO THE ARRAY IN MainScreenManager
        int colorIndex = (currentScene.buildIndex - 2);
        MainScreenManager.instance.SaveColor(hsvColorInstance, colorIndex);

        // 4. LOAD THE MAIN SCENE
        SceneManager.LoadScene("Main");
    }
}


