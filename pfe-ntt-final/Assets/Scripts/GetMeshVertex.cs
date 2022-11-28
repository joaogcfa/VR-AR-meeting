using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
// using Photon.Realtime;
// using Photon.Pun;
// using UnityEngine.XR.MagicLeap;

// using System;
// using UnityEngine.Lumin;
// using UnityEngine.Serialization;
// using UnityEngine.XR;
// using UnityEngine.XR.ARSubsystems;
// using UnityEngine.XR.Management;
// using UnityEngine.XR.MagicLeap.Meshing; 

public class GetMeshVertex : MonoBehaviour
{

    // public struct subMeshes {
    //     public string nameSubMesh;
    //     public Vector3[] verticesSubMesh;
    //     public Vector2[] uvSubMesh;
    //     public int[] trianglesSubMesh;
    // }

    // public List<subMeshes> subMeshesArray;
    // public List<subMeshes> subMeshesArrayUpdate;

    // public Mesh actualMesh;
    
    // public Vector3[] verticesArray;
    // public Vector2[] UvArray;   
    // public int[] TrianglesArray;
    // public string meshName;

    // public GameObject MeshingNodes;

    // public int meshQuantity;

    // public GameObject meshObj;

    // public Material new_material;

    // public Material red_material;

    // public PhotonView view;

    // public bool canSend;

    // public int currentMeshIndex;

    // public MLSpatialMapper Mapper;

    // public string deleteMeshName;

    // public subMeshes subMesh; 

    // public int subMeshesLength;

    // public int subMeshesLengthUpdate;

    // void Start()
    // {
    //     view = this.GetComponent<PhotonView>();

    //     subMeshesArray = new List<subMeshes>();

    //     subMeshesArrayUpdate = new List<subMeshes>();

    //     #if UNITY_LUMIN 
    //         InvokeRepeating("callRPC", 5.0f, 0.3f);

    //         InvokeRepeating("callRPCUpdate", 5.0f, 0.3f);
    //     #endif

    //     Mapper.meshAdded += delegate(MeshId meshId)
    //     {
    //         meshName = "Mesh " + meshId.ToString();

    //         //lista de vertex
    //         MeshingNodes = GameObject.Find(meshName);

    //         // getting mesh
    //         actualMesh = MeshingNodes.GetComponent<MeshFilter>().mesh;

    //         // getting vertices from mesh
    //         verticesArray = actualMesh.vertices;

    //         // getting uv from mesh
    //         UvArray = new  Vector2 [actualMesh.vertices.Length];
    //         for (int i = 0; i < UvArray.Length; i++)
    //         {
    //             UvArray[i] = new Vector2(actualMesh.vertices[i].x, actualMesh.vertices[i].z);
    //         }

    //         // getting triangles
    //         TrianglesArray = actualMesh.GetTriangles(0);

    //         subMesh.nameSubMesh = meshName;
    //         subMesh.verticesSubMesh = verticesArray;
    //         subMesh.uvSubMesh = UvArray;
    //         subMesh.trianglesSubMesh = TrianglesArray;

    //         subMeshesArray.Add(subMesh);

    //         #if UNITY_LUMIN 
    //             PhotonNetwork.CleanRpcBufferIfMine(view);
    //         #endif
    //     };

    //     Mapper.meshUpdated += delegate(MeshId meshId)
    //     {
    //         meshName = "Mesh " + meshId.ToString();

    //         //lista de vertex
    //         MeshingNodes = GameObject.Find(meshName);

    //         // getting mesh
    //         actualMesh = MeshingNodes.GetComponent<MeshFilter>().mesh;

    //         // getting vertices from mesh
    //         verticesArray = actualMesh.vertices;

    //         // getting uv from mesh
    //         UvArray = new  Vector2 [actualMesh.vertices.Length];
    //         for (int i = 0; i < UvArray.Length; i++)
    //         {
    //             UvArray[i] = new Vector2(actualMesh.vertices[i].x, actualMesh.vertices[i].z);
    //         }

    //         // getting triangles
    //         TrianglesArray = actualMesh.GetTriangles(0);

    //         subMesh.nameSubMesh = meshName;
    //         subMesh.verticesSubMesh = verticesArray;
    //         subMesh.uvSubMesh = UvArray;
    //         subMesh.trianglesSubMesh = TrianglesArray;

    //         subMeshesArrayUpdate.Add(subMesh);

    //         #if UNITY_LUMIN 
    //             PhotonNetwork.CleanRpcBufferIfMine(view);
    //         #endif
    //     };

    //     Mapper.meshRemoved += delegate(MeshId meshId)
    //     {
    //         meshName = "Mesh " + meshId.ToString();

    //         // subMeshesArrayDelete.Add(meshName);

    //         //rpc
    //         view.RPC("send_delete_mesh", RpcTarget.AllBuffered, meshName);

    //         #if UNITY_LUMIN 
    //             PhotonNetwork.CleanRpcBufferIfMine(view);
    //         #endif
    //     };
    // }

    // void Update()
    // {
        
    // }   

    // public void callRPC() {
    //     subMeshesLength = subMeshesArray.Count;
    //     if(subMeshesLength > 0) {
    //         //rpc
    //         view.RPC(
    //             "send_all_arrays", 
    //             RpcTarget.AllBuffered,
    //             subMeshesArray[subMeshesLength-1].verticesSubMesh,
    //             subMeshesArray[subMeshesLength-1].uvSubMesh,
    //             subMeshesArray[subMeshesLength-1].trianglesSubMesh,
    //             subMeshesArray[subMeshesLength-1].nameSubMesh
    //         );

    //         subMeshesArray.RemoveAt(subMeshesLength-1);
    //     }
    // }

    // public void callRPCUpdate() {
    //     subMeshesLengthUpdate = subMeshesArrayUpdate.Count;
    //     if(subMeshesLengthUpdate > 0) {
    //         //rpc
    //         view.RPC("send_delete_mesh", RpcTarget.AllBuffered, subMeshesArrayUpdate[subMeshesLengthUpdate-1].nameSubMesh);

    //         view.RPC(
    //             "update_arrays", 
    //             RpcTarget.AllBuffered,
    //             subMeshesArrayUpdate[subMeshesLengthUpdate-1].verticesSubMesh,
    //             subMeshesArrayUpdate[subMeshesLengthUpdate-1].uvSubMesh,
    //             subMeshesArrayUpdate[subMeshesLengthUpdate-1].trianglesSubMesh,
    //             subMeshesArrayUpdate[subMeshesLengthUpdate-1].nameSubMesh
    //         );

    //         subMeshesArrayUpdate.RemoveAt(subMeshesLengthUpdate-1);
    //     }
    // }

    // [PunRPC]
    // void send_all_arrays(Vector3[] verticesUpdated, Vector2[] uvUpdated, int[] trianglesUpdated,string name)
    // {
    //     print("send_all_arrays");
    //     #if UNITY_STANDALONE_WIN
    //         verticesArray = verticesUpdated;
    //         UvArray = uvUpdated;
    //         TrianglesArray = trianglesUpdated;
    //         meshName = name;
    //         CreateMesh(false);
    //     #endif
    // }

    // [PunRPC]
    // void update_arrays(Vector3[] verticesUpdated, Vector2[] uvUpdated, int[] trianglesUpdated,string name)
    // {
    //     print("update_arrays");
    //     #if UNITY_STANDALONE_WIN
    //         verticesArray = verticesUpdated;
    //         UvArray = uvUpdated;
    //         TrianglesArray = trianglesUpdated;
    //         meshName = name;
    //         CreateMesh(true);
    //     #endif
    // }

    // [PunRPC]
    // void send_delete_mesh(string name)
    // {
    //     print("send_delete_mesh");
    //     #if UNITY_STANDALONE_WIN
    //         deleteMeshName = name;
    //         DeleteMesh();
    //     #endif
    // }

    // public void CreateMesh(bool red){
    //     //criando obj vazio com mesh filter

    //     string name = meshName;
    //     meshObj = new GameObject(name);
    //     MeshFilter meshfilter = meshObj.AddComponent<MeshFilter>();
    //     meshfilter.gameObject.AddComponent<MeshRenderer>();
    //     Mesh newMesh = new Mesh();
    //     meshObj.GetComponent<MeshFilter>().mesh = newMesh;
    //     newMesh.vertices = verticesArray;
    //     newMesh.uv = UvArray;
    //     newMesh.triangles = TrianglesArray;
    //     if(red){
    //         meshObj.GetComponent<Renderer>().material = red_material;
    //     }
    //     else{
    //         meshObj.GetComponent<Renderer>().material = new_material;
    //     }
        
    //     MeshingNodes = GameObject.Find("MeshingNodes");
    //     meshObj.transform.parent = MeshingNodes.transform;
    //     meshObj.GetComponent<MeshFilter>().mesh.RecalculateNormals();
    // }

    // public void DeleteMesh() {
    //     Destroy(GameObject.Find(deleteMeshName)); 
    // }
}
