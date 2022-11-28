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

// Servico teste chamado Laputa
public class Laputa : WebSocketBehavior {
    protected override void OnMessage(MessageEventArgs e) {
        Debug.Log("Msg received!");
    }

    protected override void OnOpen() {
        Debug.Log("Server started!");
    }
}

public class GetMeshVertexWS : MonoBehaviour {
    // Struct que contem os dados de cada submesh
    
    [Serializable]
    public struct subMeshes {
        public Vector3[] verticesSubMesh;
        public int[] trianglesSubMesh;
        public bool IsAlive;
        public bool hasJob;
        public int task;
    }

    [Serializable]
    public struct receivedSubMeshes {
        public string nameSubMesh;
        public SerializableVector3[] verticesSubMesh;
        public SerializableVector2[] uvSubMesh;
        public int[] trianglesSubMesh;
        public int task;
    }

    IDictionary<string, subMeshes> HashMap = new Dictionary<string, subMeshes>();

    List<receivedSubMeshes> receivedSubmeshesList;

    private int initialMeshesToSend;
    
    private int initialMeshesSent;

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
        // ip ML1 = ws://192.168.43.34:8080
        //ip pc = ws://192.168.43.150:8080
        String serverAddress = "ws://192.168.43.150:8080";

        // SERVER
    #if UNITY_LUMIN

        InvokeRepeating("MeshSend", 1.0f, 0.5f);
        Invoke("sendActualMeshes", 10.0f);
        // InvokeRepeating("sendActualMeshes", 10.0f, 60.0f);

        try {
            wssv = new WebSocketServer(serverAddress);

            wssv.AddWebSocketService<Laputa>("/Laputa");
            wssv.Start();

            SessionManager = wssv.WebSocketServices["/Laputa"].Sessions;
        }
        catch (Exception e) {
            print(e.Message);
        }

        // Evento do Magic Leap que cria as malhas
        Mapper.meshAdded += delegate(MeshId meshId) {
            string meshName = "Mesh " + meshId.ToString();

            // getting mesh
            Mesh actualMesh = GameObject.Find(meshName).GetComponent<MeshFilter>().mesh;

            // getting vertices from mesh
            Vector3[] tempverticesArray = actualMesh.vertices;

            // getting triangles
            int[] TrianglesArray = actualMesh.GetTriangles(0);

            if(!HashMap.ContainsKey(meshName) && initialMeshesSent >= initialMeshesToSend) {

                subMeshes newSubmesh;

                newSubmesh.verticesSubMesh = tempverticesArray;
                newSubmesh.trianglesSubMesh = TrianglesArray;
                newSubmesh.IsAlive = false;
                newSubmesh.hasJob = true;
                newSubmesh.task = 0;

                HashMap.Add(meshName, newSubmesh);
            }   
        };

        // Evento do Magic Leap que edita as malhas
        Mapper.meshUpdated += delegate(MeshId meshId) {
            Mesh actualMesh;
            Vector3[] tempverticesArray;
            int[] TrianglesArray;
            SerializableVector3[] verticesArray;

            string meshName = "Mesh " + meshId.ToString();

            // getting mesh
            actualMesh = GameObject.Find(meshName).GetComponent<MeshFilter>().mesh;

            // getting vertices from mesh
            tempverticesArray = actualMesh.vertices;

            // getting triangles
            TrianglesArray = actualMesh.GetTriangles(0);

            subMeshes newSubmesh;

            newSubmesh.verticesSubMesh = tempverticesArray;
            newSubmesh.trianglesSubMesh = TrianglesArray;
            newSubmesh.hasJob = true;
            newSubmesh.task = 1;

            if (initialMeshesSent >= initialMeshesToSend) {
                if(HashMap.ContainsKey(meshName)) {
                    newSubmesh.IsAlive = HashMap[meshName].IsAlive;
                    HashMap[meshName] = newSubmesh;
                }

                else {
                    newSubmesh.IsAlive = false;
                    HashMap.Add(meshName, newSubmesh);
                    print("Tentou editar malha antes de criar!");
                }
            }

            
        };

        // Evento do Magic Leap que deleta as malhas
        Mapper.meshRemoved += delegate(MeshId meshId) {
            string meshName = "Mesh " + meshId.ToString();
            Vector3[] verticesArray = new Vector3[0];
            int[] TrianglesArray = new int[0];
            int task = 2;

            subMeshes newSubmesh;

            newSubmesh.verticesSubMesh = verticesArray;
            newSubmesh.trianglesSubMesh = TrianglesArray;
            newSubmesh.IsAlive = false;
            newSubmesh.hasJob = true;
            newSubmesh.task = 2;

            if(HashMap.ContainsKey(meshName) && initialMeshesSent >= initialMeshesToSend) {
                HashMap[meshName] = newSubmesh;
            }

        };

        


    #endif

        // CLIENT
    #if UNITY_STANDALONE_WIN

        // Funcao que checa se há malhas a serem criadas
        // InvokeRepeating("MeshCreation", 5.0f, 0.01f);

        receivedSubmeshesList = new List<receivedSubMeshes>();

        thread = new Thread(() =>
        {
            print("Thread Entered");

            // Cria o client
            ws = new WebSocket(serverAddress + "/Laputa");

            while (ws.IsAlive == false)
            {
                ws.Connect();
                print("Trying Connection...");
                Thread.Sleep(2000);
            }
            print("Connected");

            // Assim que o client recebe uma mensagem executa:
            ws.OnMessage += (sender, e) => {
                try {
                    print("recebendo");
                    byte[][] receivedArray = new byte[4][];
                    receivedSubMeshes receivedMesh;
                    object obj = ByteArrayToObject(e.RawData);
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
        #if UNITY_STANDALONE_WIN
                MeshCreation();
        #endif
    }

#if UNITY_LUMIN
    void MeshSend() {

        if (initialMeshesSent < initialMeshesToSend) {
            print("Enviado: " + initialMeshesSent + " de " + initialMeshesToSend);
        }
        else {
            if (initialMeshesToSend != 0) {
                print("Malha normal!");
            }
        }
        

        IDictionary<string, subMeshes> HashMapCopy = new Dictionary<string, subMeshes>();
        HashMapCopy = HashMap;

        foreach (KeyValuePair<String, subMeshes> submeshItem in HashMapCopy) {
            // Console.WriteLine("chave: {0}, valor: {1}", submeshItem.Key, submeshItem.Value);
            if (submeshItem.Value.hasJob && !submeshItem.Value.IsAlive) {

                threadMeshJob = new Thread(() => {
                    // Array de bytes a ser enviada com os dados das malhas
                    byte[][] requestArray = new byte[4][];

                    SerializableVector3[] verticesArray = new SerializableVector3[submeshItem.Value.verticesSubMesh.Length];

                    // Converte os vertices de vector3 para serializableVector3
                    for (int i = 0; i < verticesArray.Length; i++) {
                        verticesArray[i] = new SerializableVector3 (
                            submeshItem.Value.verticesSubMesh[i].x,
                            submeshItem.Value.verticesSubMesh[i].y,
                            submeshItem.Value.verticesSubMesh[i].z
                        );
                    }

                    // Impede que varias malhas de mesmo nome sejam criadas

                    requestArray[0] = ObjectToByteArray(submeshItem.Key);
                    requestArray[1] = ObjectToByteArray(verticesArray);
                    requestArray[2] = ObjectToByteArray(submeshItem.Value.trianglesSubMesh);
                    // O numero 0 indica que eh pra acontecer criacao de malhas
                    requestArray[3] = ObjectToByteArray(submeshItem.Value.task);

                    // print(
                    //     submeshItem.Key.ToString()
                    //         + " /tamanho vertices "
                    //         + verticesArray.Count()
                    //         + " /tamanho triangulos "
                    //         + submeshItem.Value.trianglesSubMesh.Count()
                    //         + "/criacao"
                    // );

                    SessionManager.Broadcast(ObjectToByteArray(requestArray));
                    initialMeshesSent++;
                    // }
                });

                subMeshes newSubmesh;
                newSubmesh.verticesSubMesh = submeshItem.Value.verticesSubMesh;
                newSubmesh.trianglesSubMesh = submeshItem.Value.trianglesSubMesh;
                newSubmesh.IsAlive = threadMeshJob.IsAlive;
                newSubmesh.hasJob = false;
                newSubmesh.task = 0;

                HashMap[submeshItem.Key] = newSubmesh;
            
                threadMeshJob.Start();

            }
        }
    }

    void sendActualMeshes() {

        initialMeshesToSend = 0;
        initialMeshesSent = 0;

        foreach (Transform child in meshingNodesObject.transform) {

            Mesh actualMesh;
            Vector3[] tempverticesArray;
            int[] TrianglesArray;
            SerializableVector3[] verticesArray;

            string meshName = child.gameObject.name;

            // getting mesh
            actualMesh = child.GetComponent<MeshFilter>().mesh;

            // getting vertices from mesh
            tempverticesArray = actualMesh.vertices;

            // getting triangles
            TrianglesArray = actualMesh.GetTriangles(0);

            subMeshes newSubmesh;

            newSubmesh.verticesSubMesh = tempverticesArray;
            newSubmesh.trianglesSubMesh = TrianglesArray;
            newSubmesh.hasJob = true;
            newSubmesh.task = 3;

            initialMeshesToSend++;
            

            if(HashMap.ContainsKey(meshName)) {
                newSubmesh.IsAlive = HashMap[meshName].IsAlive;
                HashMap[meshName] = newSubmesh;
            }

            else {
                newSubmesh.IsAlive = false;
                HashMap.Add(meshName, newSubmesh);
                // print("Tentou editar malha antes de criar no sendActualMeshes!");
            }
        }
    }
#endif

    void MeshCreation() {
        if (receivedSubmeshesList.Count() > 0) {
            CreateMesh();
        }
    }

    public void CreateMesh() {
        //criando obj vazio com mesh filter

        GameObject MeshingNodes;
        GameObject meshObj;

        if (receivedSubmeshesList[0].task == 1) {
            DeleteMesh(receivedSubmeshesList[0].nameSubMesh);
        }
        else if (receivedSubmeshesList[0].task == 2) {
            DeleteMesh(receivedSubmeshesList[0].nameSubMesh);
            return;
        }

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

    public void DeleteMesh(String deleteMeshName) {
        Destroy(GameObject.Find(deleteMeshName));
    }
}
