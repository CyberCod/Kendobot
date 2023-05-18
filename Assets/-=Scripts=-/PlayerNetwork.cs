using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{


    [SerializeField] Transform spawnedObjectPrefab;
    private Transform spawnedObjectTransform;

    private NetworkVariable<int> RandomNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<CustomDataBlock> MyCustomDataBlock = new NetworkVariable<CustomDataBlock>(new CustomDataBlock { intA = 1, boolB = true },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);



    public struct CustomDataBlock : INetworkSerializable {

        public int intA;
        public bool boolB;
        public FixedString128Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref intA);
            serializer.SerializeValue(ref boolB);
            serializer.SerializeValue(ref message);
        }
    }


    int SenderId;
    public override void OnNetworkSpawn()
    {
        SenderId = (int)OwnerClientId;
        RandomNumber.OnValueChanged += (int previousValue, int newValue) =>
        {
            Debug.Log("Owner = " + OwnerClientId + " : Randomnumber = " + RandomNumber.Value);
        };

        MyCustomDataBlock.OnValueChanged += (CustomDataBlock previousValue, CustomDataBlock newValue) =>
        {
            Debug.Log("Owner = " + OwnerClientId + "   intA=" + newValue.intA + "    boolB = " + newValue.boolB + "    message =" + newValue.message);
        };



    }

    public void SpawnObject()
    {
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }

    public void DespawnObject()
    {
        spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);
    }


    // Update is called once per frame
    void Update()
    {


        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.T)) { RandomNumber.Value = Random.Range(0, 100); } //send network value

        if (Input.GetKeyDown(KeyCode.Q))  //send RPC to server to instantiate a network object from prefab
        {
            SpawnNetworkObjectServerRpc();
        }

        if (Input.GetKeyDown(KeyCode.Z)) //Send RPC to server to delete network object from prefab
        {

            DespawnNetworkObjectServerRpc();
        }

        if (Input.GetKeyDown(KeyCode.U)) //send message from client to server
        {
            TestServerRpc();
        }

        if (Input.GetKeyDown(KeyCode.Y)) //send message from server to client
        {
            TestClientRpc();
        }


        if (Input.GetKeyDown(KeyCode.E)) //send message from server to client
        {
            ulong TargetID = 1;  //declare intended client
            Test2ClientRpc((int)TargetID, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { TargetID } } });
        }


        if (Input.GetKeyDown(KeyCode.R))   //Send messages from client to server while identifying which client it came from
        {
            Test2ServerRpc(new ServerRpcParams());
        }


        if (Input.GetKeyDown(KeyCode.I)) {  //send a struct  --no reference values, must use fixed strings
            MyCustomDataBlock.Value = new CustomDataBlock
            { intA = Random.Range(0, 100), boolB = false, message = "Well craptastic!" };
        }




        Vector3 moveDirection = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.W)) moveDirection.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDirection.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDirection.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDirection.x = +1f;


        float moveSpeed = 3.5f;
        transform.position += moveSpeed * moveDirection * Time.deltaTime;

    }




    [ServerRpc]  //run by client on server
    private void TestServerRpc()
    {
        Debug.Log("TestServerRpc Initiated by client" + OwnerClientId);
    }


    [ServerRpc]  //run by client on server
    private void Test2ServerRpc(ServerRpcParams serverRpcParams)
    {
        Debug.Log("Test2ServerRpc initiated on server by client " + serverRpcParams.Receive.SenderClientId);
    }


    [ClientRpc] //run by server on clients
    private void TestClientRpc()
    {
        Debug.Log("TestClientRpc started by Server " + SenderNetworkID());
    }


    [ClientRpc]  //run by server on specific client
    private void Test2ClientRpc(int target, ClientRpcParams clientRpcParams)
    {
        Debug.Log("Test2ClientRpc from Server " + SenderNetworkID() + " to target Client " + target);

    }



    [ServerRpc]
    private void SpawnNetworkObjectServerRpc()
    {
        spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }

    [ServerRpc]
    private void DespawnNetworkObjectServerRpc()
    {
        spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);

        Destroy(spawnedObjectTransform.gameObject);
    }
    private ulong SenderNetworkID()  //Allows Test2ClientRpc to use data from outside its context
    {
        return OwnerClientId;
    }
}
