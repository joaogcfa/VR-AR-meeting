using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.XR.MagicLeap;

using System;
using UnityEngine.Lumin;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.MagicLeap.Meshing;

using WebSocketSharp;
using System.Threading;
using WebSocketSharp.Server;

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

// Servico de comunicação com os clients chamado Laputa
public class Laputa : WebSocketBehavior {
    protected override void OnMessage(MessageEventArgs e) {
        // Assim que o server recebe a mensagem pedindo as malhas salvas na memoria ele para
        // a atualização de malhas e envia as malhas salvas
        if (e.Data == "SendAll") {
            GetMeshVertexWS.stopAllAtt = true;
            GetMeshVertexWS.startGetMeshes = true;
            Debug.Log("Sending memory meshes!");
        }
        // Assim que o client recebe todas as malhas planejadas o server retorna a enviar atualizações
        else {
            GetMeshVertexWS.stopAllAtt = false;
            Debug.Log("All memory meshes sent! Sending real time now!");
        }

    }

    protected override void OnOpen() {
        Debug.Log("Server started!");
    }
}

public class GetMeshVertexWS : MonoBehaviour {
    
    // Struct que contem os dados de cada submesh a ser enviada
    [Serializable]
    public struct subMeshes {
        public Vector3[] verticesSubMesh;
        public int[] trianglesSubMesh;
        public bool IsRunning;
        public Thread threadJob;
        public int task;
    }

    // Struct que contem os dados de cada submesh a ser recebida
    [Serializable]
    public struct receivedSubMeshes {
        public string nameSubMesh;
        public SerializableVector3[] verticesSubMesh;
        public SerializableVector2[] uvSubMesh;
        public int[] trianglesSubMesh;
        public int task;
    }

    public static bool stopAllAtt = true;

    public static bool startGetMeshes = false;

    IDictionary<string, subMeshes> HashMap = new Dictionary<string, subMeshes>();

    List<receivedSubMeshes> receivedSubmeshesList;

    private int initialMeshesToReceive;
    private int initialMeshesToSend;
    private int initialMeshesReceived;

    private int task;

    public Material white_material;

    public Material red_material;

    public Material green_material;

    public MLSpatialMapper Mapper;

    private string deleteMeshName;

    private subMeshes subMesh;

    public WebSocket ws;

    public Thread thread;

    public Thread threadMeshJob;

    public Thread threadMeshJobReserve;

    public WebSocketServer wssv;

    public WebSocketSessionManager SessionManager;

    public GameObject meshingNodesObject;

    // Transforma objetos da Unity em Bytes para envio
    byte[] ObjectToByteArray(object obj) {
        if (obj == null)
            return null;
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    // Transforma os Bytes recebidos em objetos da Unity
    public object ByteArrayToObject(byte[] arrBytes) {
        MemoryStream memStream = new MemoryStream();
        BinaryFormatter binForm = new BinaryFormatter();
        memStream.Write(arrBytes, 0, arrBytes.Length);
        memStream.Seek(0, SeekOrigin.Begin);
        object obj = (object)binForm.Deserialize(memStream);
        return obj;
    }

    void Start() {
        // IP se buildar pro ML1: ws://192.168.43.34:8080
        // ip se rodar no zero do pc: ws://192.168.43.150:8080
        String serverAddress = "ws://192.168.43.150:8080";

        // SERVER
    #if UNITY_LUMIN

        try {
            // Abre o server
            wssv = new WebSocketServer(serverAddress);

            // Inicia o serviço de comunicação
            wssv.AddWebSocketService<Laputa>("/Laputa");
            wssv.Start();

            SessionManager = wssv.WebSocketServices["/Laputa"].Sessions;
            print("Service created!");
        }
        catch (Exception e) {
            print(e.Message);
        }

        // Evento do Magic Leap que cria as malhas
        Mapper.meshAdded += delegate(MeshId meshId) {
            string meshName = "Mesh " + meshId.ToString();

            // Pega a mesh com base no ID
            Mesh actualMesh = GameObject.Find(meshName).GetComponent<MeshFilter>().mesh;

            // Pega os vertices com base no ID
            Vector3[] tempverticesArray = actualMesh.vertices;

            // Pega os triangulos com base no ID
            int[] TrianglesArray = actualMesh.GetTriangles(0);

            // Cria uma Thread que cria uma malha igual ao ID e envia
            if(!HashMap.ContainsKey(meshName) && !stopAllAtt) {

                threadMeshJob = new Thread(threadJobFunction);

                subMeshes newSubmesh;

                newSubmesh.verticesSubMesh = tempverticesArray;
                newSubmesh.trianglesSubMesh = TrianglesArray;
                newSubmesh.IsRunning = true;
                newSubmesh.threadJob = threadMeshJob;
                newSubmesh.task = 0;

                // Adiciona a entrada no Hashmap
                HashMap.Add(meshName, newSubmesh);

                threadMeshJob.Start(meshName);
            }   
        };

        // Evento do Magic Leap que edita as malhas
        Mapper.meshUpdated += delegate(MeshId meshId) {
            Mesh actualMesh;
            Vector3[] tempverticesArray;
            int[] TrianglesArray;
            SerializableVector3[] verticesArray;

            string meshName = "Mesh " + meshId.ToString();

            // Pega a mesh com base no ID
            actualMesh = GameObject.Find(meshName).GetComponent<MeshFilter>().mesh;

            // Pega os vertices com base no ID
            tempverticesArray = actualMesh.vertices;

            // Pega os trinagulos com base no ID
            TrianglesArray = actualMesh.GetTriangles(0);

            if (!stopAllAtt) {
                // Cria uma Thread que edita a malha que tem o ID e envia
                if(HashMap.ContainsKey(meshName)) {
                    if(HashMap[meshName].threadJob.ThreadState == ThreadState.Stopped) {

                        try {
                            subMeshes newSubmesh;

                            newSubmesh.verticesSubMesh = tempverticesArray;
                            newSubmesh.trianglesSubMesh = TrianglesArray;
                            newSubmesh.IsRunning = true;
                            newSubmesh.threadJob = HashMap[meshName].threadJob;
                            newSubmesh.task = 1;

                            // Edita a entrada no Hashmap
                            HashMap[meshName] = newSubmesh;
                            
                            HashMap[meshName].threadJob.Start(meshName);
                        }
                        // Caso a Thread anterior não tenha sido encerrada cria uma nova
                        catch (Exception msg) {
                            print("Thread ja iniciada: " + msg);

                            threadMeshJobReserve = new Thread(threadJobFunction);

                            subMeshes newSubmesh;
                            newSubmesh.verticesSubMesh = tempverticesArray;
                            newSubmesh.trianglesSubMesh = TrianglesArray;
                            newSubmesh.IsRunning = true;
                            newSubmesh.threadJob = threadMeshJobReserve;
                            newSubmesh.task = 1;

                            HashMap[meshName] = newSubmesh;

                            threadMeshJobReserve.Start(meshName);
                        }
                    }
                }
                // Cria uma Thread que cria uma malha igual ao ID e envia
                else {

                    threadMeshJob = new Thread(threadJobFunction);

                    subMeshes newSubmesh;

                    newSubmesh.verticesSubMesh = tempverticesArray;
                    newSubmesh.trianglesSubMesh = TrianglesArray;
                    newSubmesh.IsRunning = true;
                    newSubmesh.threadJob = threadMeshJob;
                    newSubmesh.task = 1;

                    HashMap.Add(meshName, newSubmesh);
                    //threadMeshJob.Abort();
                    threadMeshJob.Start(meshName);
                }
            }

            
        };

        // Evento do Magic Leap que deleta as malhas
        Mapper.meshRemoved += delegate(MeshId meshId) {
            string meshName = "Mesh " + meshId.ToString();
            Vector3[] verticesArray = new Vector3[0];
            int[] TrianglesArray = new int[0];
            int task = 2;
            
            // Cria uma Thread que deleta uma malha igual ao ID e envia pro client
            if(HashMap.ContainsKey(meshName) && !HashMap[meshName].IsRunning && !stopAllAtt) {
                try {
                    subMeshes newSubmesh;

                    newSubmesh.verticesSubMesh = verticesArray;
                    newSubmesh.trianglesSubMesh = TrianglesArray;
                    newSubmesh.IsRunning = true;
                    newSubmesh.threadJob = HashMap[meshName].threadJob;
                    newSubmesh.task = task;

                    HashMap[meshName] = newSubmesh;
                    
                    HashMap[meshName].threadJob.Start(meshName);
                }
                // Caso a Thread anterior não tenha sido encerrada cria uma nova
                catch (Exception msg) {

                    threadMeshJobReserve = new Thread(threadJobFunction);

                    subMeshes newSubmesh;
                    newSubmesh.verticesSubMesh = verticesArray;
                    newSubmesh.trianglesSubMesh = TrianglesArray;
                    newSubmesh.IsRunning = true;
                    newSubmesh.threadJob = threadMeshJobReserve;
                    newSubmesh.task = task;

                    HashMap[meshName] = newSubmesh;

                    threadMeshJobReserve.Start(meshName);
                }
            }
        };

    // Bloqueia temporariamente a atualização de malhas e envia as malhas
    // salvas na memoria 
    if (startGetMeshes) {
        startGetMeshes = false;
        Invoke("sendActualMeshes", 1.0f);
    }


    #endif

        // CLIENT
    #if UNITY_STANDALONE_WIN

        initialMeshesToReceive = 0;
        initialMeshesReceived = 0;

        receivedSubmeshesList = new List<receivedSubMeshes>();

        // Thread do client que cuida do recebimento de malhas
        thread = new Thread(() => {
            print("Thread Entered");

            // Cria o client
            ws = new WebSocket(serverAddress + "/Laputa");

            while (ws.IsAlive == false) {
                ws.Connect();
                print("Trying Connection...");
                Thread.Sleep(2000);
            }
            print("Connected");

            // Pede pro server todas as malhas salvas na memoria
            ws.Send("SendAll");

            // Assim que o client recebe uma mensagem executa:
            ws.OnMessage += (sender, e) => {
                try {
                    byte[][] receivedArray = new byte[4][];
                    receivedSubMeshes receivedMesh;

                    object obj = ByteArrayToObject(e.RawData);

                    // Se o recebido for só um inteiro é o numero de malhas salvas na memoria
                    if (obj.GetType().Equals(typeof(System.Int32))) {
                        initialMeshesToReceive = (System.Int32)obj;
                    }

                    receivedArray = obj as byte[][];

                    string meshName = ByteArrayToObject(receivedArray[0]) as string;
                    SerializableVector3[] verticesArray =
                        ByteArrayToObject(receivedArray[1]) as SerializableVector3[];
                    SerializableVector2[] UvArray = new SerializableVector2[verticesArray.Length];
                    int vCount = 0;
                    while (vCount < verticesArray.Length) {
                        UvArray[vCount] = new SerializableVector2(
                            verticesArray[vCount].x,
                            verticesArray[vCount].z
                        );
                        vCount++;
                    }
                    int[] TrianglesArray = ByteArrayToObject(receivedArray[2]) as System.Int32[];
                    int task = (System.Int32)ByteArrayToObject(receivedArray[3]);

                    receivedMesh.nameSubMesh = meshName;
                    receivedMesh.verticesSubMesh = verticesArray;
                    receivedMesh.uvSubMesh = UvArray;
                    receivedMesh.trianglesSubMesh = TrianglesArray;
                    receivedMesh.task = task;

                    print(
                        meshName
                        + " Tamanho vertices: "
                        + verticesArray.Count()
                        + " Tamanho triangulos: "
                        + TrianglesArray.Count()
                        + " Task: "
                        + task
                    );

                    initialMeshesReceived++;

                    // Depois de receber as malhas da memoria pede as atualizacoes em tempo real
                    if (initialMeshesReceived  == initialMeshesToReceive) {
                        ws.Send("ResumeSending");
                    }

                    receivedSubmeshesList.Add(receivedMesh);
                }
                catch (Exception error) {
                    Debug.Log(error);
                }
            };
        });

        thread.Start();

        void OnDestroy() {
            thread.Abort();
        }
    #endif
    }

    void Update() { 
        #if UNITY_LUMIN
        // Bloqueia temporariamente a atualização de malhas e envia as malhas
        // salvas na memoria 
        if (startGetMeshes) {
            startGetMeshes = false;
            Invoke("sendActualMeshes", 1.0f);
        }
        #endif
        #if UNITY_STANDALONE_WIN
            // Chama a criação de malhas pra ver se tem alguma malha na lista pra criar
            MeshCreation();
        #endif
    }

#if UNITY_LUMIN

    // Thread que faz um pacote contendo as infos da malha e envia
    public void threadJobFunction(object thisMeshName) {

        string meshName = thisMeshName as String;
        // Array de bytes a ser enviada com os dados das malhas
        byte[][] requestArray = new byte[4][];

        SerializableVector3[] verticesArray = new SerializableVector3[HashMap[meshName].verticesSubMesh.Length];

        // Converte os vertices de vector3 para serializableVector3
        for (int i = 0; i < verticesArray.Length; i++) {
            verticesArray[i] = new SerializableVector3 (
                HashMap[meshName].verticesSubMesh[i].x,
                HashMap[meshName].verticesSubMesh[i].y,
                HashMap[meshName].verticesSubMesh[i].z
            );
        }


        requestArray[0] = ObjectToByteArray(meshName);
        requestArray[1] = ObjectToByteArray(verticesArray);
        requestArray[2] = ObjectToByteArray(HashMap[meshName].trianglesSubMesh);
        // Task é a tarefa a ser feita no client
        requestArray[3] = ObjectToByteArray(HashMap[meshName].task);

        print(  meshName
                + " Tamanho vertices: "
                + verticesArray.Count()
                + " Tamanho triangulos: "
                + HashMap[meshName].trianglesSubMesh.Count()
                + " Task: "
                + HashMap[meshName].task
        );

        switch (HashMap[meshName].task) {
            case 0:
                print("Mesh creation");
                break;
            case 1:
                print("Mesh edit");
                break;
            case 2:
                print("Mesh deletion");
                break;
            case 3:
                print("Mesh memory");
                break;
        }

        // Envia o pacote
        SessionManager.Broadcast(ObjectToByteArray(requestArray));

        subMeshes newSubmesh;

        newSubmesh.verticesSubMesh = HashMap[meshName].verticesSubMesh;
        newSubmesh.trianglesSubMesh = HashMap[meshName].trianglesSubMesh;
        newSubmesh.IsRunning = false;
        newSubmesh.threadJob = HashMap[meshName].threadJob;
        newSubmesh.task = HashMap[meshName].task;

        HashMap[meshName] = newSubmesh;
    }

    // Pega as malhas salvas na memoria e cria as threads pra enviar essas malhas
    void sendActualMeshes() {

        initialMeshesToSend = meshingNodesObject.transform.childCount;

        SessionManager.Broadcast(ObjectToByteArray(initialMeshesToSend));

        foreach (Transform child in meshingNodesObject.transform) {

            Mesh actualMesh;
            Vector3[] tempverticesArray;
            int[] TrianglesArray;
            SerializableVector3[] verticesArray;

            string meshName = child.gameObject.name;

            // Pega a mesh com base no nome
            actualMesh = child.GetComponent<MeshFilter>().mesh;

            // Pega os vertices com base no nome
            tempverticesArray = actualMesh.vertices;

            // Pega os trinagulos com base no nome
            TrianglesArray = actualMesh.GetTriangles(0);

            initialMeshesToSend++;
            

            if(HashMap.ContainsKey(meshName)) {
                
                try {
                    if (!HashMap[meshName].IsRunning) {

                        subMeshes newSubmesh;
                        newSubmesh.verticesSubMesh = tempverticesArray;
                        newSubmesh.trianglesSubMesh = TrianglesArray;
                        newSubmesh.IsRunning = true;
                        newSubmesh.threadJob = HashMap[meshName].threadJob;
                        newSubmesh.task = 3;

                        HashMap[meshName] = newSubmesh;
                        //HashMap[meshName].threadJob.Abort();
                        HashMap[meshName].threadJob.Start(meshName);
                    }
                }
                // Caso a Thread anterior não tenha sido encerrada cria uma nova
                catch (Exception msg) {

                    threadMeshJobReserve = new Thread(threadJobFunction);

                    subMeshes newSubmesh;
                    newSubmesh.verticesSubMesh = tempverticesArray;
                    newSubmesh.trianglesSubMesh = TrianglesArray;
                    newSubmesh.IsRunning = true;
                    newSubmesh.threadJob = threadMeshJobReserve;
                    newSubmesh.task = 3;

                    HashMap[meshName] = newSubmesh;

                    threadMeshJobReserve.Start(meshName);
                }
                
            }
            // Caso a malha não exista ainda cria ela no Hashmap
            else {

                threadMeshJob = new Thread(threadJobFunction);

                subMeshes newSubmesh;

                newSubmesh.verticesSubMesh = tempverticesArray;
                newSubmesh.trianglesSubMesh = TrianglesArray;
                newSubmesh.IsRunning = true;
                newSubmesh.threadJob = threadMeshJob;
                newSubmesh.task = 3;

                HashMap.Add(meshName, newSubmesh);
                threadMeshJob.Start(meshName);
            }
        }
    }
#endif

#if UNITY_STANDALONE_WIN

    // Se tiver malhas pra criar chama a CreateMesh
    void MeshCreation() {
        if (receivedSubmeshesList.Count() > 0) {
            CreateMesh();
        }
    }

    // Cria a malha no client
    public void CreateMesh() {

        GameObject MeshingNodes;
        GameObject meshObj;

        // Se a malha ja existir apaga a antiga
        if (receivedSubmeshesList[0].task == 1) {
            DeleteMesh(receivedSubmeshesList[0].nameSubMesh);
        }
        else if (receivedSubmeshesList[0].task == 2) {
            DeleteMesh(receivedSubmeshesList[0].nameSubMesh);
            receivedSubmeshesList.RemoveAt(0);
            return;
        }

        // Criando obj vazio com mesh filter
        meshObj = new GameObject(receivedSubmeshesList[0].nameSubMesh);
        MeshFilter meshfilter = meshObj.AddComponent<MeshFilter>();
        meshfilter.gameObject.AddComponent<MeshRenderer>();
        Mesh newMesh = new Mesh();
        meshObj.GetComponent<MeshFilter>().mesh = newMesh;

        Vector3[] TempVert = new Vector3[receivedSubmeshesList[0].verticesSubMesh.Length];

        for (int i = 0; i < TempVert.Length; i++) {
            TempVert[i] = new Vector3(
                receivedSubmeshesList[0].verticesSubMesh[i].x,
                receivedSubmeshesList[0].verticesSubMesh[i].y,
                receivedSubmeshesList[0].verticesSubMesh[i].z
            );
        }
        newMesh.vertices = TempVert;

        Vector2[] TempUV = new Vector2[receivedSubmeshesList[0].uvSubMesh.Length];
        for (int i = 0; i < TempUV.Length; i++) {
            TempUV[i] = new Vector2(
                receivedSubmeshesList[0].uvSubMesh[i].x,
                receivedSubmeshesList[0].uvSubMesh[i].y
            );
        }
        newMesh.uv = TempUV;

        newMesh.triangles = receivedSubmeshesList[0].trianglesSubMesh;

        // Materiais diferentes pra criação, edição e deleção de malhas
        if (receivedSubmeshesList[0].task == 1) {
            meshObj.GetComponent<Renderer>().material = red_material;
        }
        else if (receivedSubmeshesList[0].task == 0) {
            meshObj.GetComponent<Renderer>().material = white_material;
        }
        else if (receivedSubmeshesList[0].task == 3) {
            meshObj.GetComponent<Renderer>().material = green_material;
        }
        else {
            DeleteMesh(receivedSubmeshesList[0].nameSubMesh);
        }

        MeshingNodes = GameObject.Find("MeshingNodes");
        meshObj.transform.parent = MeshingNodes.transform;
        meshObj.GetComponent<MeshFilter>().mesh.RecalculateNormals();

        receivedSubmeshesList.RemoveAt(0);
    }

    // Deleta o mesh no client com base no id
    public void DeleteMesh(String deleteMeshName) {
        Destroy(GameObject.Find(deleteMeshName));
    }
#endif
}


