using UnityEngine;
using UnityEngine.UI;

public class CreateUserMenu : MonoBehaviour
{
    [SerializeField]
    private Button createButton = null;

    [SerializeField]
    private InputField inputField = null;

    [SerializeField]
    private InputField serverURLInput = null;
    [SerializeField]
    private InputField serverPortInput = null;
    [SerializeField]
    private Toggle secureToggle;

    public string UserName
    {
        get { return inputField.text; }
    }

    public string ServerURL
    {
        get
        {
            if (string.IsNullOrEmpty(serverURLInput.text) == false)
            {
                return serverURLInput.text;
            }

            return ExampleManager.Instance.ColyseusServerAddress;
        }
    }

    public string ServerPort
    {
        get
        {
            if (string.IsNullOrEmpty(serverPortInput.text) == false)
            {
                return serverPortInput.text;
            }

            return ExampleManager.Instance.ColyseusServerPort;
        }
    }

    public bool UseSecure
    {
        get
        {
            return secureToggle.isOn;
        }
    }

    private void Start()
    {
        createButton.interactable = false;
        string oldName = PlayerPrefs.GetString("UserName", "");
        if (oldName.Length > 0)
        {
            inputField.text = oldName;
            createButton.interactable = true;
        }

        serverURLInput.text = ExampleManager.Instance.ColyseusServerAddress;
        serverPortInput.text = ExampleManager.Instance.ColyseusServerPort;
        secureToggle.isOn = ExampleManager.Instance.ColyseusUseSecure;
    }

    public void OnInputFieldChange()
    {
        createButton.interactable = inputField.text.Length > 0;
    }
}