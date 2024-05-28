using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Photon.Pun;
using UnityEngine.SceneManagement;

//[ExecuteInEditMode]
public class PlayerNickname : MonoBehaviourPun
{
    //public string RoundText = "";
    public TMP_InputField inputNickName; // LHE
    public Canvas nickNameCanvas;
    public float Radius = 10;
    public GameObject TextMeshPrefab;
    public bool join, destroy;
    List<GameObject> prefabs = new List<GameObject>();

    string nickName;

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex != 2)
        {
            this.gameObject.GetComponent<PlayerNickname>().enabled = false;
        }
    }

    void Update()
    {
        if(SceneManager.GetActiveScene().buildIndex == 2)
        {
            this.gameObject.GetComponent<PlayerNickname>().enabled = true;
            nickName = photonView.Owner.NickName;
        }

        if (join)
        {
            Vector3 center = transform.position;
            float ang = (nickName.Length * 20) / 2;

            for (int i = 0; i < nickName.Length; i++)
            {
                // ù ������ position �� ���� �
                Vector3 pos = RandomCircle(center, Radius, ang);

                Quaternion rot = Quaternion.FromToRotation(Vector3.forward, center - pos);

                GameObject go = Instantiate(TextMeshPrefab, this.gameObject.transform, false);
                go.transform.position = pos;

                if (!((nickName.Length % 2 == 1) && (i == (nickName.Length - 1) / 2)))
                {
                    go.transform.rotation = rot;
                }
                else
                {
                    go.transform.rotation = new Quaternion(rot.x, rot.y + 180, rot.z, 0);
                }
                
                prefabs.Add(go);
                
                char c = nickName[i];
                prefabs[i].GetComponentInChildren<TextMeshPro>().text = c.ToString();
                
                ang -= (nickName.Length * 20) / (nickName.Length - 1);

                prefabs[0].transform.rotation = Quaternion.Euler(0, prefabs[0].transform.rotation.y-120, prefabs[0].transform.rotation.z); 
            }
            join = false;
        }
        if (destroy)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                DestroyImmediate(prefabs[i]);
            }
            prefabs = new List<GameObject>();
            destroy = false;
        }
    }
    
    Vector3 RandomCircle(Vector3 center, float radius, float ang)
    {
        Vector3 pos;
        pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
        pos.y = center.y;
        pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
        return pos;
    }
}
