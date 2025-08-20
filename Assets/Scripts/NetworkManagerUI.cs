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
        // ��������� ���������� �� ������� ������� ������.
        // ����� ������ ����� ������, ��������� ��������������� ����� (������-�������).

        hostButton.onClick.AddListener(() =>
        {
            // ��������� ����� �����. Unity ������� ������ � ����� ����������� � ���� ��� ������.
            NetworkManager.Singleton.StartHost();
            Debug.Log("Starting Host...");
        });

        serverButton.onClick.AddListener(() =>
        {
            // ��������� ����� ����������� �������.
            NetworkManager.Singleton.StartServer();
            Debug.Log("Starting Server...");
        });

        clientButton.onClick.AddListener(() =>
        {
            // ��������� ����� �������, ������� ����� ������ � ������������ � �������.
            NetworkManager.Singleton.StartClient();
            Debug.Log("Starting Client...");
        });
    }
}