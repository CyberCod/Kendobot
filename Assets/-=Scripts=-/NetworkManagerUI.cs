using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{

    [SerializeField] private Button ServerButton;
    [SerializeField] private Button HostButton;
    [SerializeField] private Button ClientButton;
    [SerializeField] NetworkManager nm;
    GameObject tempNM;

    private void Awake()
    {
        
        nm = NetworkManager.Singleton;
        tempNM = GameObject.FindGameObjectWithTag("NetworkManager");

        if (nm == null) { nm = tempNM.GetComponent<NetworkManager>(); }

        if (nm != null)
        {

            ServerButton.onClick.AddListener(() =>
            {
                Debug.Log("Server Button Clicked");
                Serverstart();
            });

            HostButton.onClick.AddListener(() =>
            {
                Debug.Log("Host Button Clicked");
                Hoststart();
            });

            ClientButton.onClick.AddListener(() =>
            {
                Debug.Log("Client Button Clicked");
                Clientstart();
            });
        }
        else
        {
            Debug.Log("NetworkManager not found");
        }
    }

    
    public void Disconnect()
    {
        //nm.Stop();
        DisableButtons();
    }

    public void Serverstart()
    {
        
        nm.StartServer();
        DisableButtons();
    }

    public void Hoststart()
    {
        nm.StartHost();
        DisableButtons();
    }

    public void Clientstart()
    {
        nm.StartClient();
        DisableButtons();
    }

    public void DisableButtons()
    {

        ClientButton.enabled = false;
        ServerButton.enabled = false;
        HostButton.enabled = false;

        ClientButton.interactable = false;
        ServerButton.interactable = false;
        HostButton.interactable = false;

        ClientButton.gameObject.SetActive(false);
        ServerButton.gameObject.SetActive(false);
        HostButton.gameObject.SetActive(false);


    }


}
