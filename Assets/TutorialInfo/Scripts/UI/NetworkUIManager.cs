using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUIManager : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;

    void Start()
    {
        // 방장 버튼 
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            HideButtons(); // 버튼 숨기기
        });

        // 참가자 버튼
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            HideButtons(); // 버튼 숨기기
        });
    }
    //버튼 비활성화
    void HideButtons()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
    }
}
