using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    public static UpgradeUI instance;
    

    public Button[] buttons;
    public TextMeshProUGUI[] upgradeDescriptions;
    private System.Action[] currentChoices; // Guardamos las acciones a ejecutar

    void Awake()
    {
        instance = this;
        gameObject.SetActive(false); // Empezamos ocultos
    }

    void Start()
    {
        
    }

    // El jugador le pasa las 3 mejoras aleatorias y sus nombres al abrirse
    public void Show(System.Action[] choices, string[] descriptions)
    {
        Time.timeScale = 0f;
        currentChoices = choices;

        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;

            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => Select(index));

            // Cambiamos el texto del botón con el nombre que nos pasó el jugador
            upgradeDescriptions[i].text = descriptions[i];
        }

        gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Select(int index)
    {
        // Ejecuta la función del jugador que corresponda a este botón
        currentChoices[index]?.Invoke(); 

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameObject.SetActive(false);
        GameManager.instance.state = GameState.Playing;
    }
}