using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        // Добавляем слушателей на события нажатия кнопок.
        // Когда кнопка будет нажата, вызовется соответствующий метод (лямбда-функция).

        hostButton.onClick.AddListener(() =>
        {
            // Запускаем режим Хоста. Unity создаст сервер и сразу подключится к нему как клиент.
            NetworkManager.Singleton.StartHost();
            Debug.Log("Starting Host...");
        });

        serverButton.onClick.AddListener(() =>
        {
            // Запускаем режим выделенного Сервера.
            NetworkManager.Singleton.StartServer();
            Debug.Log("Starting Server...");
        });

        clientButton.onClick.AddListener(() =>
        {
            // Запускаем режим Клиента, который будет искать и подключаться к серверу.
            NetworkManager.Singleton.StartClient();
            Debug.Log("Starting Client...");
        });
    }
}