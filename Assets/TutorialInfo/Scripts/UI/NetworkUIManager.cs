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
        // 호스트(방장) 버튼 클릭 시
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            HideButtons(); // 버튼 숨기기
        });

        // 클라이언트(참가자) 버튼 클릭 시
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            HideButtons(); // 버튼 숨기기
        });
    }

    // 접속하면 버튼 거슬리니까 숨겨버리는 함수
    void HideButtons()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
    }
}
